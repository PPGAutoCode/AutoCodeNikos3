
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
            var existingAPIEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>(
                "SELECT * FROM ApiEndpoints WHERE Id = @Id",
                new { request.Id });

            if (existingAPIEndpoint == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Get the apiTags names from request.apiTags and fetch the whole entity of ApiTag
            var apiTags = new List<ApiTag>();
            foreach (var tagName in request.ApiTags)
            {
                var apiTagRequestDto = new ApiTagRequestDto { Name = tagName };
                var apiTag = await _apiTagService.GetApiTag(apiTagRequestDto);
                if (apiTag != null)
                {
                    apiTags.Add(apiTag);
                }
            }

            // Step 4: If apiTag does not exist, create API tags
            foreach (var tagName in request.ApiTags.Where(tagName => !apiTags.Any(t => t.Name == tagName)))
            {
                var createApiTagDto = new CreateApiTagDto { Name = tagName };
                await _apiTagService.CreateApiTag(createApiTagDto);
            }

            // Step 5: Define API Tags for Removal
            var tagsToRemove = existingAPIEndpoint.ApiTags.Where(existingTag => !request.ApiTags.Contains(existingTag.Name)).ToList();

            // Step 6: Define API Tags for Addition
            var tagsToAdd = request.ApiTags.Where(tagName => !existingAPIEndpoint.ApiTags.Any(t => t.Name == tagName)).ToList();

            // Step 7: Handle Attachments
            if (request.Documentation != null)
            {
                var deleteAttachmentDto = new DeleteAttachmentDto { Id = existingAPIEndpoint.Documentation };
                await _attachmentService.DeleteAttachment(deleteAttachmentDto);
            }

            if (request.Swagger != null)
            {
                var deleteAttachmentDto = new DeleteAttachmentDto { Id = existingAPIEndpoint.Swagger };
                await _attachmentService.DeleteAttachment(deleteAttachmentDto);
            }

            if (request.Tour != null)
            {
                var deleteAttachmentDto = new DeleteAttachmentDto { Id = existingAPIEndpoint.Tour };
                await _attachmentService.DeleteAttachment(deleteAttachmentDto);
            }

            var documentationId = request.Documentation?.Id ?? existingAPIEndpoint.Documentation;
            var swaggerId = request.Swagger?.Id ?? existingAPIEndpoint.Swagger;
            var tourId = request.Tour?.Id ?? existingAPIEndpoint.Tour;

            // Step 9: Update the APIEndpoint object
            existingAPIEndpoint.ApiName = request.ApiName;
            existingAPIEndpoint.ApiScope = request.ApiScope;
            existingAPIEndpoint.ApiScopeProduction = request.ApiScopeProduction;
            existingAPIEndpoint.Deprecated = request.Deprecated;
            existingAPIEndpoint.Description = request.Description;
            existingAPIEndpoint.Documentation = documentationId;
            existingAPIEndpoint.EndpointUrls = request.EndpointUrls;
            existingAPIEndpoint.AppEnvironment = request.AppEnvironment;
            existingAPIEndpoint.Swagger = swaggerId;
            existingAPIEndpoint.Tour = tourId;
            existingAPIEndpoint.ApiVersion = request.ApiVersion;
            existingAPIEndpoint.Langcode = request.Langcode;
            existingAPIEndpoint.Sticky = request.Sticky;
            existingAPIEndpoint.Promote = request.Promote;
            existingAPIEndpoint.UrlAlias = request.UrlAlias;
            existingAPIEndpoint.Published = request.Published;

            // Step 10: Database Transactions
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Step 12: Remove Old Tags
                    foreach (var tag in tagsToRemove)
                    {
                        await _dbConnection.ExecuteAsync(
                            "DELETE FROM APIEndpointTags WHERE ApiEndpointId = @ApiEndpointId AND ApiTagId = @ApiTagId",
                            new { ApiEndpointId = existingAPIEndpoint.Id, ApiTagId = tag.Id },
                            transaction);
                    }

                    // Step 14: Add New Tags
                    foreach (var tagName in tagsToAdd)
                    {
                        var apiTag = await _apiTagService.GetApiTag(new ApiTagRequestDto { Name = tagName });
                        await _dbConnection.ExecuteAsync(
                            "INSERT INTO APIEndpointTags (ApiEndpointId, ApiTagId) VALUES (@ApiEndpointId, @ApiTagId)",
                            new { ApiEndpointId = existingAPIEndpoint.Id, ApiTagId = apiTag.Id },
                            transaction);
                    }

                    // Step 16: Insert the updated APIEndpoint object
                    await _dbConnection.ExecuteAsync(
                        "UPDATE ApiEndpoints SET ApiName = @ApiName, ApiScope = @ApiScope, ApiScopeProduction = @ApiScopeProduction, Deprecated = @Deprecated, Description = @Description, Documentation = @Documentation, EndpointUrls = @EndpointUrls, AppEnvironment = @AppEnvironment, Swagger = @Swagger, Tour = @Tour, ApiVersion = @ApiVersion, Langcode = @Langcode, Sticky = @Sticky, Promote = @Promote, UrlAlias = @UrlAlias, Published = @Published WHERE Id = @Id",
                        existingAPIEndpoint,
                        transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
                }
            }

            // Step 18: Return Result
            return existingAPIEndpoint.Id.ToString();
        }
    }
}
