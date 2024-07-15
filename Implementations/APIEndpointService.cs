
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
        private readonly IApiTagService _apiTagService;
        private readonly IAttachmentService _attachmentService;

        public APIEndpointService(IDbConnection dbConnection, IApiTagService apiTagService, IAttachmentService attachmentService)
        {
            _dbConnection = dbConnection;
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
            var existingEndpoint = await _dbConnection.QueryFirstOrDefaultAsync<APIEndpoint>("SELECT * FROM ApiEndpoints WHERE Id = @Id", new { request.Id });
            if (existingEndpoint == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Get the apiTags names from request.apiTags and fetch the whole entity of ApiTag
            var apiTags = new List<ApiTag>();
            if (request.ApiTags != null)
            {
                foreach (var tagName in request.ApiTags)
                {
                    var apiTagRequestDto = new ApiTagRequestDto { Name = tagName };
                    var apiTag = await _apiTagService.GetApiTag(apiTagRequestDto);
                    if (apiTag != null)
                    {
                        apiTags.Add(apiTag);
                    }
                }
            }

            // Step 4: If apiTag does not exist, create API tags
            var newTags = request.ApiTags.Except(apiTags.Select(t => t.Name)).ToList();
            foreach (var newTag in newTags)
            {
                var createApiTagDto = new CreateApiTagDto { Name = newTag };
                await _apiTagService.CreateApiTag(createApiTagDto);
            }

            // Step 5: Define API Tags for Removal
            var tagsToRemove = existingEndpoint.ApiTags.Select(t => t.Id).Except(apiTags.Select(t => t.Id)).ToList();

            // Step 6: Define API Tags for Addition
            var tagsToAdd = apiTags.Select(t => t.Id).Except(existingEndpoint.ApiTags.Select(t => t.Id)).ToList();

            // Step 7: Handle Attachments
            if (request.Documentation != null)
            {
                var deleteAttachmentDto = new DeleteAttachmentDto { Id = existingEndpoint.Documentation };
                await _attachmentService.DeleteAttachment(deleteAttachmentDto);
            }
            if (request.Swagger != null)
            {
                var deleteAttachmentDto = new DeleteAttachmentDto { Id = existingEndpoint.Swagger };
                await _attachmentService.DeleteAttachment(deleteAttachmentDto);
            }
            if (request.Tour != null)
            {
                var deleteAttachmentDto = new DeleteAttachmentDto { Id = existingEndpoint.Tour };
                await _attachmentService.DeleteAttachment(deleteAttachmentDto);
            }

            var documentationId = request.Documentation?.Id ?? existingEndpoint.Documentation;
            var swaggerId = request.Swagger?.Id ?? existingEndpoint.Swagger;
            var tourId = request.Tour?.Id ?? existingEndpoint.Tour;

            // Step 9: Update the APIEndpoint object
            var updatedEndpoint = new APIEndpoint
            {
                Id = existingEndpoint.Id,
                ApiName = request.ApiName,
                ApiScope = request.ApiScope,
                ApiScopeProduction = request.ApiScopeProduction,
                Deprecated = request.Deprecated,
                Description = request.Description,
                Documentation = documentationId,
                EndpointUrls = request.EndpointUrls,
                AppEnvironment = request.AppEnvironment,
                Swagger = swaggerId,
                Tour = tourId,
                ApiVersion = request.ApiVersion,
                Langcode = request.Langcode,
                Sticky = request.Sticky,
                Promote = request.Promote,
                UrlAlias = request.UrlAlias,
                Published = request.Published
            };

            // Step 10: Database Transactions
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Step 12: Remove Old Tags
                    foreach (var tagId in tagsToRemove)
                    {
                        await _dbConnection.ExecuteAsync("DELETE FROM APIEndpointTags WHERE ApiEndpointId = @ApiEndpointId AND ApiTagId = @ApiTagId", new { ApiEndpointId = existingEndpoint.Id, ApiTagId = tagId }, transaction);
                    }

                    // Step 14: Add New Tags
                    foreach (var tagId in tagsToAdd)
                    {
                        await _dbConnection.ExecuteAsync("INSERT INTO APIEndpointTags (ApiEndpointId, ApiTagId) VALUES (@ApiEndpointId, @ApiTagId)", new { ApiEndpointId = existingEndpoint.Id, ApiTagId = tagId }, transaction);
                    }

                    // Step 16: Insert the updated APIEndpoint object
                    await _dbConnection.ExecuteAsync("UPDATE ApiEndpoints SET ApiName = @ApiName, ApiScope = @ApiScope, ApiScopeProduction = @ApiScopeProduction, Deprecated = @Deprecated, Description = @Description, Documentation = @Documentation, EndpointUrls = @EndpointUrls, AppEnvironment = @AppEnvironment, Swagger = @Swagger, Tour = @Tour, ApiVersion = @ApiVersion, Langcode = @Langcode, Sticky = @Sticky, Promote = @Promote, UrlAlias = @UrlAlias, Published = @Published WHERE Id = @Id", updatedEndpoint, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
                }
            }

            // Step 18: Return Result
            return existingEndpoint.Id.ToString();
        }
    }
}
