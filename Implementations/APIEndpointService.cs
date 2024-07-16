
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProjectName.Interfaces;
using ProjectName.Types;
using ProjectName.ControllersExceptions;

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
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch Existing API Endpoint
            var existingAPIEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>("SELECT * FROM ApiEndpoints WHERE Id = @Id", new { request.Id });
            if (existingAPIEndpoint == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Fetch and validate related entities
            var appEnvironmentRequest = new AppEnvironmentRequestDto { Id = request.AppEnvironment };
            var appEnvironment = await _appEnvironmentService.GetAppEnvironment(appEnvironmentRequest);
            if (appEnvironment == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 4: Handle Tags
            List<Guid> newTagIds = new List<Guid>();
            if (request.ApiTags != null)
            {
                foreach (var tagName in request.ApiTags)
                {
                    var tagRequest = new ApiTagRequestDto { Name = tagName };
                    var tag = await _apiTagService.GetApiTag(tagRequest);
                    if (tag == null)
                    {
                        var createTagDto = new CreateApiTagDto { Name = tagName };
                        await _apiTagService.CreateApiTag(createTagDto);
                        tag = await _apiTagService.GetApiTag(tagRequest);
                    }
                    newTagIds.Add(tag.Id);
                }
            }

            // Step 5: Handle Attachments
            async Task HandleAttachment(CreateAttachmentDto attachmentDto, Guid existingAttachmentId, Action<Guid> setAttachmentId)
            {
                if (attachmentDto != null)
                {
                    var attachmentRequest = new AttachmentRequestDto { Id = existingAttachmentId };
                    var existingAttachment = await _attachmentService.GetAttachment(attachmentRequest);
                    if (existingAttachment != null && existingAttachment.FileName != attachmentDto.FileName)
                    {
                        var deleteAttachmentDto = new DeleteAttachmentDto { Id = existingAttachmentId };
                        await _attachmentService.DeleteAttachment(deleteAttachmentDto);
                    }
                    var newAttachmentId = Guid.Parse(await _attachmentService.CreateAttachment(attachmentDto));
                    setAttachmentId(newAttachmentId);
                }
                else if (existingAttachmentId != Guid.Empty)
                {
                    var deleteAttachmentDto = new DeleteAttachmentDto { Id = existingAttachmentId };
                    await _attachmentService.DeleteAttachment(deleteAttachmentDto);
                    setAttachmentId(Guid.Empty);
                }
            }

            await HandleAttachment(request.Documentation, existingAPIEndpoint.Documentation, id => existingAPIEndpoint.Documentation = id);
            await HandleAttachment(request.Swagger, existingAPIEndpoint.Swagger, id => existingAPIEndpoint.Swagger = id);
            await HandleAttachment(request.Tour, existingAPIEndpoint.Tour, id => existingAPIEndpoint.Tour = id);

            // Step 6: Update APIEndpoint object
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

            // Step 7: Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Remove Old Tags
                    await _dbConnection.ExecuteAsync("DELETE FROM APIEndpointTags WHERE APIEndpointId = @Id", new { existingAPIEndpoint.Id }, transaction);

                    // Add New Tags
                    foreach (var tagId in newTagIds)
                    {
                        await _dbConnection.ExecuteAsync("INSERT INTO APIEndpointTags (Id, APIEndpointId, ApiTagId) VALUES (@Id, @APIEndpointId, @ApiTagId)",
                            new { Id = Guid.NewGuid(), APIEndpointId = existingAPIEndpoint.Id, ApiTagId = tagId }, transaction);
                    }

                    // Update APIEndpoint object in the database table
                    await _dbConnection.ExecuteAsync("UPDATE ApiEndpoints SET ApiName = @ApiName, ApiScope = @ApiScope, ApiScopeProduction = @ApiScopeProduction, Deprecated = @Deprecated, Description = @Description, Documentation = @Documentation, EndpointUrls = @EndpointUrls, AppEnvironment = @AppEnvironment, Swagger = @Swagger, Tour = @Tour, ApiVersion = @ApiVersion, Langcode = @Langcode, Sticky = @Sticky, Promote = @Promote, UrlAlias = @UrlAlias, Published = @Published WHERE Id = @Id",
                        existingAPIEndpoint, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
                }
            }

            return existingAPIEndpoint.Id.ToString();
        }
    }
}
