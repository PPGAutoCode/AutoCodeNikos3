
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProjectName.ControllersExceptions;
using ProjectName.Interfaces;
using ProjectName.Types;

namespace ProjectName.Services
{
    public class APIEndpointService : IAPIEndpointService
    {
        private readonly IDbConnection _dbConnection;
        private readonly IAppEnvironmentService _appEnvironmentService;
        private readonly IApiTagService _apiTagService;
        private readonly IAttachmentService _attachmentService;

        public APIEndpointService(IDbConnection dbConnection, IAppEnvironmentService appEnvironmentService, IApiTagService apiTagService, IAttachmentService attachmentService)
        {
            _dbConnection = dbConnection;
            _appEnvironmentService = appEnvironmentService;
            _apiTagService = apiTagService;
            _attachmentService = attachmentService;
        }

        public async Task<string> UpdateAPIEndpoint(UpdateAPIEndpointDto request)
        {
            // Step 1: Validate UpdateAPIEndpointDto
            if (request.Id == Guid.Empty || string.IsNullOrEmpty(request.ApiName) || string.IsNullOrEmpty(request.Langcode) || string.IsNullOrEmpty(request.UrlAlias))
            {
                throw new BusinessException("DP-422", "Missing required parameters in UpdateAPIEndpointDto.");
            }

            // Step 2: Fetch Existing API Endpoint
            var existingAPIEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>(
                "SELECT * FROM ApiEndpoints WHERE Id = @Id",
                new { request.Id });

            if (existingAPIEndpoint == null)
            {
                throw new BusinessException("DP-404", "APIEndpoint not found.");
            }

            // Step 3: Fetch and validate related entities
            var appEnvironmentRequest = new AppEnvironmentRequestDto { Id = request.AppEnvironment };
            var appEnvironment = await _appEnvironmentService.GetAppEnvironment(appEnvironmentRequest);
            if (appEnvironment == null)
            {
                throw new BusinessException("DP-404", "AppEnvironment not found.");
            }

            // Step 4: Handle Tags
            List<Guid> newTagIds = new List<Guid>();
            if (request.ApiTags != null)
            {
                foreach (var tagName in request.ApiTags)
                {
                    var apiTagRequest = new ApiTagRequestDto { Name = tagName };
                    var apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                    if (apiTag == null)
                    {
                        var createApiTagDto = new CreateApiTagDto { Name = tagName };
                        await _apiTagService.CreateApiTag(createApiTagDto);
                        apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                    }
                    newTagIds.Add(apiTag.Id);
                }
            }

            // Step 5: Handle Attachments
            async Task HandleAttachment(CreateAttachmentDto newAttachment, Guid? existingAttachmentId, Action<Guid> updateAttachmentField)
            {
                if (newAttachment != null)
                {
                    if (existingAttachmentId.HasValue)
                    {
                        var existingAttachment = await _attachmentService.GetAttachment(new AttachmentRequestDto { Id = existingAttachmentId.Value });
                        if (!existingAttachment.FileUrl.SequenceEqual(newAttachment.FileUrl))
                        {
                            await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingAttachmentId.Value });
                            var newAttachmentId = Guid.Parse(await _attachmentService.CreateAttachment(newAttachment));
                            updateAttachmentField(newAttachmentId);
                        }
                    }
                    else
                    {
                        var newAttachmentId = Guid.Parse(await _attachmentService.CreateAttachment(newAttachment));
                        updateAttachmentField(newAttachmentId);
                    }
                }
                else if (existingAttachmentId.HasValue)
                {
                    await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingAttachmentId.Value });
                    updateAttachmentField(Guid.Empty);
                }
            }

            await HandleAttachment(request.Documentation, existingAPIEndpoint.Documentation, id => existingAPIEndpoint.Documentation = id);
            await HandleAttachment(request.Swagger, existingAPIEndpoint.Swagger, id => existingAPIEndpoint.Swagger = id);
            await HandleAttachment(request.Tour, existingAPIEndpoint.Tour, id => existingAPIEndpoint.Tour = id);

            // Step 6: Update the APIEndpoint object
            existingAPIEndpoint.ApiName = request.ApiName;
            existingAPIEndpoint.ApiScope = request.ApiScope;
            existingAPIEndpoint.ApiScopeProduction = request.ApiScopeProduction;
            existingAPIEndpoint.Deprecated = request.Deprecated;
            existingAPIEndpoint.Description = request.Description;
            existingAPIEndpoint.EndpointUrls = request.EndpointUrls;
            existingAPIEndpoint.AppEnvironment = request.AppEnvironment;
            existingAPIEndpoint.ApiVersion = request.ApiVersion;
            existingAPIEndpoint.Langcode = request.Langcode;
            existingAPIEndpoint.Sticky = request.Sticky;
            existingAPIEndpoint.Promote = request.Promote;
            existingAPIEndpoint.UrlAlias = request.UrlAlias;
            existingAPIEndpoint.Published = request.Published;
            existingAPIEndpoint.ApiTags = newTagIds;

            // Step 7: Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    await _dbConnection.ExecuteAsync(
                        "UPDATE ApiEndpoints SET ApiName = @ApiName, ApiScope = @ApiScope, ApiScopeProduction = @ApiScopeProduction, Deprecated = @Deprecated, Description = @Description, EndpointUrls = @EndpointUrls, AppEnvironment = @AppEnvironment, ApiVersion = @ApiVersion, Langcode = @Langcode, Sticky = @Sticky, Promote = @Promote, UrlAlias = @UrlAlias, Published = @Published WHERE Id = @Id",
                        existingAPIEndpoint, transaction);

                    // Remove old tags
                    await _dbConnection.ExecuteAsync(
                        "DELETE FROM APIEndpointTags WHERE APIEndpointId = @Id",
                        new { existingAPIEndpoint.Id }, transaction);

                    // Add new tags
                    foreach (var tagId in newTagIds)
                    {
                        await _dbConnection.ExecuteAsync(
                            "INSERT INTO APIEndpointTags (Id, APIEndpointId, ApiTagId) VALUES (@Id, @APIEndpointId, @ApiTagId)",
                            new { Id = Guid.NewGuid(), APIEndpointId = existingAPIEndpoint.Id, ApiTagId = tagId }, transaction);
                    }

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return "APIEndpoint updated successfully.";
        }
    }
}
