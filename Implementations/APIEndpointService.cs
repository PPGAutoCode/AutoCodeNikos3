
using System;
using System.Collections.Generic;
using System.Data;
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
            // 1. Validate UpdateAPIEndpointDto
            if (request.Id == Guid.Empty || string.IsNullOrWhiteSpace(request.ApiName) || string.IsNullOrWhiteSpace(request.Langcode) || string.IsNullOrWhiteSpace(request.UrlAlias))
            {
                throw new BusinessException("DP-422", "Required fields are missing.");
            }

            // 2. Fetch Existing API Endpoint
            var existingApiEndpoint = await _dbConnection.QueryFirstOrDefaultAsync<APIEndpoint>("SELECT * FROM APIEndpoints WHERE Id = @Id", new { Id = request.Id });
            if (existingApiEndpoint == null)
            {
                throw new TechnicalException("DP-404", "API Endpoint not found.");
            }

            // 3. Fetch and validate related entities
            // AppEnvironment
            var appEnvironmentRequest = new AppEnvironmentRequestDto { Id = request.AppEnvironment };
            var appEnvironment = _appEnvironmentService.GetAppEnvironment(appEnvironmentRequest);
            if (appEnvironment == null)
            {
                throw new TechnicalException("DP-404", "App Environment not found.");
            }

            // ApiTags
            List<ApiTag> apiTagsList = new List<ApiTag>();
            if (request.ApiTags != null)
            {
                foreach (var tagName in request.ApiTags)
                {
                    var apiTagRequest = new ApiTagRequestDto { Name = tagName };
                    var apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                    if (apiTag == null)
                    {
                        var createApiTagDto = new CreateApiTagDto { Name = tagName };
                        var newTagId = await _apiTagService.CreateApiTag(createApiTagDto);
                        apiTagsList.Add(new ApiTag { Id = Guid.Parse(newTagId), Name = tagName });
                    }
                    else
                    {
                        apiTagsList.Add(apiTag);
                    }
                }
            }

            // 9. Handle Attachments
            await _attachmentService.UpsertAttachment(request.Documentation);
            await _attachmentService.UpsertAttachment(request.Swagger);
            await _attachmentService.UpsertAttachment(request.Tour);

            // 10. Update the APIEndpoint object
            existingApiEndpoint.ApiName = request.ApiName;
            existingApiEndpoint.ApiScope = request.ApiScope;
            existingApiEndpoint.ApiScopeProduction = request.ApiScopeProduction;
            existingApiEndpoint.Deprecated = request.Deprecated;
            existingApiEndpoint.Description = request.Description;
            existingApiEndpoint.EndpointUrls = request.EndpointUrls;
            existingApiEndpoint.AppEnvironment = request.AppEnvironment;
            existingApiEndpoint.ApiVersion = request.ApiVersion;
            existingApiEndpoint.Langcode = request.Langcode;
            existingApiEndpoint.Sticky = request.Sticky;
            existingApiEndpoint.Promote = request.Promote;
            existingApiEndpoint.UrlAlias = request.UrlAlias;
            existingApiEndpoint.Published = request.Published;
            existingApiEndpoint.ApiTags = apiTagsList;

            // 11. Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Remove Old Tags
                    await _dbConnection.ExecuteAsync("DELETE FROM APIEndpointTags WHERE APIEndpointId = @Id", new { Id = existingApiEndpoint.Id }, transaction);

                    // Add New Tags
                    foreach (var tag in apiTagsList)
                    {
                        var newTagId = Guid.NewGuid();
                        await _dbConnection.ExecuteAsync("INSERT INTO APIEndpointTags (Id, APITagId, APIEndpointId) VALUES (@Id, @ApiTagId, @APIEndpointId)", 
                            new { Id = newTagId, ApiTagId = tag.Id, APIEndpointId = existingApiEndpoint.Id }, transaction);
                    }

                    // Update APIEndpoint
                    await _dbConnection.ExecuteAsync("UPDATE APIEndpoints SET ApiName = @ApiName, ApiScope = @ApiScope, ApiScopeProduction = @ApiScopeProduction, Deprecated = @Deprecated, Description = @Description, EndpointUrls = @EndpointUrls, AppEnvironment = @AppEnvironment, ApiVersion = @ApiVersion, Langcode = @Langcode, Sticky = @Sticky, Promote = @Promote, UrlAlias = @UrlAlias, Published = @Published WHERE Id = @Id",
                        existingApiEndpoint, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "An error occurred while updating the API Endpoint.");
                }
            }

            return existingApiEndpoint.Id.ToString();
        }
    }
}
