
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProjectName.Types;
using ProjectName.Interfaces;
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
                throw new BusinessException("DP-422", "Required fields are missing.");
            }

            // Step 2: Fetch Existing API Endpoint
            var existingAPIEndpoint = await _dbConnection.QueryFirstOrDefaultAsync<APIEndpoint>(
                "SELECT * FROM ApiEndpoints WHERE Id = @Id", new { Id = request.Id });

            if (existingAPIEndpoint == null)
            {
                throw new BusinessException("DP-404", "API Endpoint not found.");
            }

            // Step 3: Fetch and validate related entities
            var appEnvironment = await _appEnvironmentService.GetAppEnvironment(new AppEnvironmentRequestDto { Id = request.AppEnvironment });
            if (appEnvironment == null)
            {
                throw new BusinessException("DP-404", "App Environment not found.");
            }

            // Step 4: Handle API Tags
            var apiTags = new List<Guid>();
            if (request.ApiTags != null)
            {
                foreach (var tagName in request.ApiTags)
                {
                    var apiTag = await _apiTagService.GetApiTag(new ApiTagRequestDto { Name = tagName });
                    if (apiTag == null)
                    {
                        var newTagId = await _apiTagService.CreateApiTag(new CreateApiTagDto { Name = tagName });
                        apiTags.Add(new Guid(newTagId));
                    }
                    else
                    {
                        apiTags.Add(apiTag.Id);
                    }
                }
            }

            // Step 5: Handle Attachments
            async Task HandleAttachment(CreateAttachmentDto newAttachment, Guid? existingAttachmentId, Action<Guid?> updateAttachmentField)
            {
                if (newAttachment != null)
                {
                    if (existingAttachmentId.HasValue)
                    {
                        var existingAttachment = await _attachmentService.GetAttachment(new AttachmentRequestDto { Id = existingAttachmentId.Value });
                        if (existingAttachment != null && !existingAttachment.FileUrl.SequenceEqual(newAttachment.FileUrl))
                        {
                            await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingAttachmentId.Value });
                            var newAttachmentId = await _attachmentService.CreateAttachment(newAttachment);
                            updateAttachmentField(new Guid(newAttachmentId));
                        }
                    }
                    else
                    {
                        var newAttachmentId = await _attachmentService.CreateAttachment(newAttachment);
                        updateAttachmentField(new Guid(newAttachmentId));
                    }
                }
                else if (existingAttachmentId.HasValue)
                {
                    await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingAttachmentId.Value });
                    updateAttachmentField(null);
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

            // Step 7: Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Remove Old Tags
                    var existingTags = await _dbConnection.QueryAsync<Guid>(
                        "SELECT TagId FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId", new { APIEndpointId = request.Id }, transaction);

                    var tagsToRemove = existingTags.Except(apiTags).ToList();
                    foreach (var tagId in tagsToRemove)
                    {
                        await _dbConnection.ExecuteAsync(
                            "DELETE FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId AND TagId = @TagId",
                            new { APIEndpointId = request.Id, TagId = tagId }, transaction);
                    }

                    // Add New Tags
                    var tagsToAdd = apiTags.Except(existingTags).ToList();
                    foreach (var tagId in tagsToAdd)
                    {
                        await _dbConnection.ExecuteAsync(
                            "INSERT INTO APIEndpointTags (APIEndpointId, TagId) VALUES (@APIEndpointId, @TagId)",
                            new { APIEndpointId = request.Id, TagId = tagId }, transaction);
                    }

                    // Update APIEndpoint
                    await _dbConnection.ExecuteAsync(
                        @"UPDATE ApiEndpoints SET 
                            ApiName = @ApiName, 
                            ApiScope = @ApiScope, 
                            ApiScopeProduction = @ApiScopeProduction, 
                            Deprecated = @Deprecated, 
                            Description = @Description, 
                            EndpointUrls = @EndpointUrls, 
                            AppEnvironment = @AppEnvironment, 
                            ApiVersion = @ApiVersion, 
                            Langcode = @Langcode, 
                            Sticky = @Sticky, 
                            Promote = @Promote, 
                            UrlAlias = @UrlAlias, 
                            Published = @Published, 
                            Documentation = @Documentation, 
                            Swagger = @Swagger, 
                            Tour = @Tour 
                          WHERE Id = @Id",
                        existingAPIEndpoint, transaction);

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new TechnicalException("1001", "A technical exception has occurred, please contact your system administrator.");
                }
            }

            return "API Endpoint updated successfully.";
        }
    }
}
