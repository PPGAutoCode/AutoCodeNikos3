
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

        public async Task<string> CreateAPIEndpoint(CreateAPIEndpointDto request)
        {
            // Validate required parameters
            if (string.IsNullOrEmpty(request.ApiName) || string.IsNullOrEmpty(request.Langcode))
            {
                throw new BusinessException("DP-422", "Required parameters are missing.");
            }

            // Fetch and validate AppEnvironment
            var appEnvironmentRequest = new AppEnvironmentRequestDto { Id = request.AppEnvironment };
            var appEnvironment = await _appEnvironmentService.GetAppEnvironment(appEnvironmentRequest);
            if (appEnvironment == null)
            {
                throw new TechnicalException("DP-404", "AppEnvironment not found.");
            }

            // Fetch and validate ApiTags
            var apiTagIds = new List<Guid>();
            foreach (var tagName in request.ApiTags)
            {
                var apiTagRequest = new ApiTagRequestDto { Name = tagName };
                var apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                if (apiTag != null)
                {
                    apiTagIds.Add(apiTag.Id);
                }
                else
                {
                    var newTagId = await _apiTagService.CreateApiTag(new CreateApiTagDto { Name = tagName });
                    apiTagIds.Add(new Guid(newTagId));
                }
            }

            // Handle file uploads
            Guid? documentationId = null, swaggerId = null, tourId = null;
            if (request.Documentation != null)
            {
                documentationId = await _attachmentService.CreateAttachment(request.Documentation);
            }
            if (request.Swagger != null)
            {
                swaggerId = await _attachmentService.CreateAttachment(request.Swagger);
            }
            if (request.Tour != null)
            {
                tourId = await _attachmentService.CreateAttachment(request.Tour);
            }

            // Create APIEndpoint object
            var apiEndpoint = new APIEndpoint
            {
                Id = Guid.NewGuid(),
                ApiName = request.ApiName,
                ApiScope = request.ApiScope,
                ApiScopeProduction = request.ApiScopeProduction,
                Deprecated = request.Deprecated,
                Description = request.Description,
                Documentation = documentationId,
                EndpointUrls = request.EndpointUrls,
                AppEnvironment = appEnvironment.Id,
                Swagger = swaggerId,
                Tour = tourId,
                ApiVersion = request.ApiVersion,
                Langcode = request.Langcode,
                Sticky = request.Sticky,
                Promote = request.Promote,
                UrlAlias = request.UrlAlias,
                Published = request.Published
            };

            // Create APIEndpointTags list
            var apiEndpointTags = new List<APIEndpointTag>();
            foreach (var apiTagId in apiTagIds)
            {
                apiEndpointTags.Add(new APIEndpointTag
                {
                    Id = Guid.NewGuid(),
                    APIEndpointId = apiEndpoint.Id,
                    APITagId = apiTagId
                });
            }

            // Insert into database in a single transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    var insertApiEndpointQuery = "INSERT INTO APIEndpoints (Id, ApiName, ApiScope, ApiScopeProduction, Deprecated, Description, Documentation, EndpointUrls, AppEnvironment, Swagger, Tour, ApiVersion, Langcode, Sticky, Promote, UrlAlias, Published) " +
                                                  "VALUES (@Id, @ApiName, @ApiScope, @ApiScopeProduction, @Deprecated, @Description, @Documentation, @EndpointUrls, @AppEnvironment, @Swagger, @Tour, @ApiVersion, @Langcode, @Sticky, @Promote, @UrlAlias, @Published)";
                    await _dbConnection.ExecuteAsync(insertApiEndpointQuery, apiEndpoint, transaction);

                    var insertApiEndpointTagsQuery = "INSERT INTO APIEndpointTags (Id, APIEndpointId, APITagId) VALUES (@Id, @APIEndpointId, @APITagId)";
                    foreach (var tag in apiEndpointTags)
                    {
                        await _dbConnection.ExecuteAsync(insertApiEndpointTagsQuery, tag, transaction);
                    }

                    transaction.Commit();
                    return apiEndpoint.Id.ToString();
                }
                catch
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "An error occurred while creating the API endpoint.");
                }
            }
        }

        public async Task<APIEndpoint> GetAPIEndpoint(APIEndpointRequestDto request)
        {
            // Validate request payload
            if (request.Id == Guid.Empty && string.IsNullOrEmpty(request.ApiName))
            {
                throw new BusinessException("DP-422", "Required parameters are missing.");
            }

            // Fetch API Endpoint
            APIEndpoint apiEndpoint = null;
            if (request.Id != Guid.Empty)
            {
                var query = "SELECT * FROM APIEndpoints WHERE Id = @Id";
                apiEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>(query, new { Id = request.Id });
            }
            else if (!string.IsNullOrEmpty(request.ApiName))
            {
                var query = "SELECT * FROM APIEndpoints WHERE ApiName = @ApiName";
                apiEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>(query, new { ApiName = request.ApiName });
            }

            if (apiEndpoint == null)
            {
                throw new TechnicalException("DP-404", "API Endpoint not found.");
            }

            // Fetch associated tags
            var tagIdsQuery = "SELECT APITagId FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId";
            var tagIds = await _dbConnection.QueryAsync<Guid>(tagIdsQuery, new { APIEndpointId = apiEndpoint.Id });

            foreach (var tagId in tagIds)
            {
                var apiTagRequest = new ApiTagRequestDto { Id = tagId };
                var apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                if (apiTag == null)
                {
                    throw new TechnicalException("DP-404", "Associated API Tag not found.");
                }
                apiEndpoint.ApiTags.Add(apiTag);
            }

            return apiEndpoint;
        }

        public async Task<string> UpdateAPIEndpoint(UpdateAPIEndpointDto request)
        {
            // Validate UpdateAPIEndpointDto
            if (request.Id == Guid.Empty || string.IsNullOrEmpty(request.ApiName) || string.IsNullOrEmpty(request.Langcode) || string.IsNullOrEmpty(request.UrlAlias))
            {
                throw new BusinessException("DP-422", "Required parameters are missing.");
            }

            // Fetch existing API Endpoint
            var existingApiEndpointQuery = "SELECT * FROM APIEndpoints WHERE Id = @Id";
            var existingApiEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>(existingApiEndpointQuery, new { Id = request.Id });
            if (existingApiEndpoint == null)
            {
                throw new TechnicalException("DP-404", "API Endpoint not found.");
            }

            // Fetch and validate AppEnvironment
            var appEnvironmentRequest = new AppEnvironmentRequestDto { Id = request.AppEnvironment };
            var appEnvironment = await _appEnvironmentService.GetAppEnvironment(appEnvironmentRequest);
            if (appEnvironment == null)
            {
                throw new TechnicalException("DP-404", "AppEnvironment not found.");
            }

            // Fetch and validate ApiTags
            var apiTagIds = new List<Guid>();
            foreach (var tagName in request.ApiTags)
            {
                var apiTagRequest = new ApiTagRequestDto { Name = tagName };
                var apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                if (apiTag != null)
                {
                    apiTagIds.Add(apiTag.Id);
                }
                else
                {
                    var newTagId = await _apiTagService.CreateApiTag(new CreateApiTagDto { Name = tagName });
                    apiTagIds.Add(new Guid(newTagId));
                }
            }

            // Update APIEndpoint object
            existingApiEndpoint.ApiName = request.ApiName;
            existingApiEndpoint.ApiScope = request.ApiScope;
            existingApiEndpoint.ApiScopeProduction = request.ApiScopeProduction;
            existingApiEndpoint.Deprecated = request.Deprecated;
            existingApiEndpoint.Description = request.Description;
            existingApiEndpoint.EndpointUrls = request.EndpointUrls;
            existingApiEndpoint.AppEnvironment = appEnvironment.Id;
            existingApiEndpoint.ApiVersion = request.ApiVersion;
            existingApiEndpoint.Langcode = request.Langcode;
            existingApiEndpoint.Sticky = request.Sticky;
            existingApiEndpoint.Promote = request.Promote;
            existingApiEndpoint.UrlAlias = request.UrlAlias;
            existingApiEndpoint.Published = request.Published;

            // Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Update APIEndpoint
                    var updateApiEndpointQuery = "UPDATE APIEndpoints SET ApiName = @ApiName, ApiScope = @ApiScope, ApiScopeProduction = @ApiScopeProduction, Deprecated = @Deprecated, Description = @Description, EndpointUrls = @EndpointUrls, AppEnvironment = @AppEnvironment, ApiVersion = @ApiVersion, Langcode = @Langcode, Sticky = @Sticky, Promote = @Promote, UrlAlias = @UrlAlias, Published = @Published WHERE Id = @Id";
                    await _dbConnection.ExecuteAsync(updateApiEndpointQuery, existingApiEndpoint, transaction);

                    // Handle ApiTags Removal and Addition
                    // (Implementation for handling tags would go here)

                    transaction.Commit();
                    return existingApiEndpoint.Id.ToString();
                }
                catch
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "An error occurred while updating the API endpoint.");
                }
            }
        }

        public async Task<bool> DeleteAPIEndpoint(DeleteAPIEndpointDto request)
        {
            // Validate Request Payload
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Required parameter is missing.");
            }

            // Fetch Existing API Endpoint
            var existingApiEndpointQuery = "SELECT * FROM APIEndpoints WHERE Id = @Id";
            var existingApiEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>(existingApiEndpointQuery, new { Id = request.Id });
            if (existingApiEndpoint == null)
            {
                throw new TechnicalException("DP-404", "API Endpoint not found.");
            }

            // Delete Related Attachments
            if (existingApiEndpoint.Documentation.HasValue)
            {
                await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingApiEndpoint.Documentation.Value });
            }
            if (existingApiEndpoint.Swagger.HasValue)
            {
                await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingApiEndpoint.Swagger.Value });
            }
            if (existingApiEndpoint.Tour.HasValue)
            {
                await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingApiEndpoint.Tour.Value });
            }

            // Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Delete APIEndpointTags
                    var deleteTagsQuery = "DELETE FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId";
                    await _dbConnection.ExecuteAsync(deleteTagsQuery, new { APIEndpointId = request.Id }, transaction);

                    // Delete APIEndpoint
                    var deleteApiEndpointQuery = "DELETE FROM APIEndpoints WHERE Id = @Id";
                    await _dbConnection.ExecuteAsync(deleteApiEndpointQuery, new { Id = request.Id }, transaction);

                    transaction.Commit();
                    return True;
                }
                catch
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "An error occurred while deleting the API endpoint.");
                }
            }
        }

        public async Task<List<APIEndpoint>> GetListAPIEndpoint(ListAPIEndpointRequestDto request)
        {
            // Validate pagination parameters
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Invalid pagination parameters.");
            }

            // Fetch API Endpoints
            var query = "SELECT * FROM APIEndpoints ORDER BY Id OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";
            var apiEndpoints = await _dbConnection.QueryAsync<APIEndpoint>(query, new { PageOffset = request.PageOffset, PageLimit = request.PageLimit });

            // Fetch and Map Related Tags
            foreach (var apiEndpoint in apiEndpoints)
            {
                var tagIdsQuery = "SELECT APITagId FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId";
                var tagIds = await _dbConnection.QueryAsync<Guid>(tagIdsQuery, new { APIEndpointId = apiEndpoint.Id });

                foreach (var tagId in tagIds)
                {
                    var apiTagRequest = new ApiTagRequestDto { Id = tagId };
                    var apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                    if (apiTag == null)
                    {
                        throw new TechnicalException("DP-404", "Associated API Tag not found.");
                    }
                    apiEndpoint.ApiTags.Add(apiTag);
                }
            }

            return apiEndpoints.AsList();
        }
    }
}
