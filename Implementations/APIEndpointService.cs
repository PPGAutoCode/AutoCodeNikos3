
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
            // Validate required fields
            if (string.IsNullOrEmpty(request.ApiName) || string.IsNullOrEmpty(request.Langcode))
            {
                throw new BusinessException("DP-422", "Required fields are missing.");
            }

            // Fetch and validate AppEnvironment
            var appEnvironmentRequest = new AppEnvironmentRequestDto { Id = request.AppEnvironment };
            var appEnvironment = await _appEnvironmentService.GetAppEnvironment(appEnvironmentRequest);
            if (appEnvironment == null)
            {
                throw new TechnicalException("DP-404", "AppEnvironment not found.");
            }

            // Fetch or create ApiTags
            var apiTagIds = new List<Guid>();
            foreach (var tagName in request.ApiTags)
            {
                var apiTagRequest = new ApiTagRequestDto { Name = tagName };
                var apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                if (apiTag == null)
                {
                    var createApiTagDto = new CreateApiTagDto { Name = tagName };
                    var newTagId = await _apiTagService.CreateApiTag(createApiTagDto);
                    apiTagIds.Add(new Guid(newTagId));
                }
                else
                {
                    apiTagIds.Add(apiTag.Id);
                }
            }

            // Handle file uploads
            Guid? documentationId = null;
            Guid? swaggerId = null;
            Guid? tourId = null;

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
            foreach (var tagId in apiTagIds)
            {
                apiEndpointTags.Add(new APIEndpointTag
                {
                    Id = Guid.NewGuid(),
                    APIEndpointId = apiEndpoint.Id,
                    APITagId = tagId
                });
            }

            // Insert into database in a single transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    var insertApiEndpointQuery = "INSERT INTO APIEndpoints (Id, ApiName, ApiScope, ApiScopeProduction, Deprecated, Description, Documentation, EndpointUrls, AppEnvironment, Swagger, Tour, ApiVersion, Langcode, Sticky, Promote, UrlAlias, Published) VALUES (@Id, @ApiName, @ApiScope, @ApiScopeProduction, @Deprecated, @Description, @Documentation, @EndpointUrls, @AppEnvironment, @Swagger, @Tour, @ApiVersion, @Langcode, @Sticky, @Promote, @UrlAlias, @Published)";
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
                throw new BusinessException("DP-422", "Id or ApiName must be provided.");
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
                throw new TechnicalException("DP-404", "APIEndpoint not found.");
            }

            // Fetch associated tags
            var tagIdsQuery = "SELECT APITagId FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId";
            var tagIds = await _dbConnection.QueryAsync<Guid>(tagIdsQuery, new { APIEndpointId = apiEndpoint.Id });

            var apiTags = new List<ApiTag>();
            foreach (var tagId in tagIds)
            {
                var apiTagRequest = new ApiTagRequestDto { Id = tagId };
                var apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                if (apiTag != null)
                {
                    apiTags.Add(apiTag);
                }
            }

            apiEndpoint.ApiTags = apiTags;
            return apiEndpoint;
        }

        public async Task<string> UpdateAPIEndpoint(UpdateAPIEndpointDto request)
        {
            // Validate required fields
            if (request.Id == Guid.Empty || string.IsNullOrEmpty(request.ApiName) || string.IsNullOrEmpty(request.Langcode) || string.IsNullOrEmpty(request.UrlAlias))
            {
                throw new BusinessException("DP-422", "Required fields are missing.");
            }

            // Fetch existing API Endpoint
            var existingApiEndpointQuery = "SELECT * FROM APIEndpoints WHERE Id = @Id";
            var existingApiEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>(existingApiEndpointQuery, new { Id = request.Id });
            if (existingApiEndpoint == null)
            {
                throw new TechnicalException("DP-404", "APIEndpoint not found.");
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
                if (apiTag == null)
                {
                    var createApiTagDto = new CreateApiTagDto { Name = tagName };
                    var newTagId = await _apiTagService.CreateApiTag(createApiTagDto);
                    apiTagIds.Add(new Guid(newTagId));
                }
                else
                {
                    apiTagIds.Add(apiTag.Id);
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

            // Perform database updates in a single transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    var updateApiEndpointQuery = "UPDATE APIEndpoints SET ApiName = @ApiName, ApiScope = @ApiScope, ApiScopeProduction = @ApiScopeProduction, Deprecated = @Deprecated, Description = @Description, EndpointUrls = @EndpointUrls, AppEnvironment = @AppEnvironment, ApiVersion = @ApiVersion, Langcode = @Langcode, Sticky = @Sticky, Promote = @Promote, UrlAlias = @UrlAlias, Published = @Published WHERE Id = @Id";
                    await _dbConnection.ExecuteAsync(updateApiEndpointQuery, existingApiEndpoint, transaction);

                    // Handle ApiTags addition and removal
                    // (Implementation for adding/removing tags goes here)

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
            // Validate request payload
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Id must be provided.");
            }

            // Fetch existing API Endpoint
            var existingApiEndpointQuery = "SELECT * FROM APIEndpoints WHERE Id = @Id";
            var existingApiEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>(existingApiEndpointQuery, new { Id = request.Id });
            if (existingApiEndpoint == null)
            {
                throw new TechnicalException("DP-404", "APIEndpoint not found.");
            }

            // Delete related attachments
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

            // Perform database updates in a single transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Delete APIEndpointTags
                    var deleteApiEndpointTagsQuery = "DELETE FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId";
                    await _dbConnection.ExecuteAsync(deleteApiEndpointTagsQuery, new { APIEndpointId = existingApiEndpoint.Id }, transaction);

                    // Delete APIEndpoint
                    var deleteApiEndpointQuery = "DELETE FROM APIEndpoints WHERE Id = @Id";
                    await _dbConnection.ExecuteAsync(deleteApiEndpointQuery, new { Id = request.Id }, transaction);

                    transaction.Commit();
                    return true;
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
            var fetchApiEndpointsQuery = "SELECT * FROM APIEndpoints ORDER BY Id OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";
            var apiEndpoints = await _dbConnection.QueryAsync<APIEndpoint>(fetchApiEndpointsQuery, new { PageOffset = request.PageOffset, PageLimit = request.PageLimit });

            // Fetch and map related tags
            foreach (var apiEndpoint in apiEndpoints)
            {
                var tagIdsQuery = "SELECT APITagId FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId";
                var tagIds = await _dbConnection.QueryAsync<Guid>(tagIdsQuery, new { APIEndpointId = apiEndpoint.Id });

                var apiTags = new List<ApiTag>();
                foreach (var tagId in tagIds)
                {
                    var apiTagRequest = new ApiTagRequestDto { Id = tagId };
                    var apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                    if (apiTag != null)
                    {
                        apiTags.Add(apiTag);
                    }
                }

                apiEndpoint.ApiTags = apiTags;
            }

            return apiEndpoints.AsList();
        }
    }
}
