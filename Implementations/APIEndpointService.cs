
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

        public async Task<string> CreateAPIEndpoint(CreateAPIEndpointDto request)
        {
            // Step 1: Validate request payload
            if (string.IsNullOrEmpty(request.ApiName) || string.IsNullOrEmpty(request.Langcode))
            {
                throw new BusinessException("DP-422", "Validation error: ApiName or Langcode is null or empty.");
            }

            // Step 2: Fetch and Validate Related Entities
            var appEnvironment = await _appEnvironmentService.GetAppEnvironment(new AppEnvironmentRequestDto { Id = request.AppEnvironment });
            if (appEnvironment == null)
            {
                throw new TechnicalException("DP-404", "AppEnvironment not found.");
            }

            var apiTags = new List<ApiTag>();
            if (request.ApiTags != null)
            {
                foreach (var tagName in request.ApiTags)
                {
                    var apiTag = await _apiTagService.GetApiTag(new ApiTagRequestDto { Name = tagName });
                    if (apiTag == null)
                    {
                        apiTag = await _apiTagService.CreateApiTag(new CreateApiTagDto { Name = tagName });
                    }
                    apiTags.Add(apiTag);
                }
            }

            // Step 3: Upload Attachment Files
            var documentationId = request.Documentation != null ? await _attachmentService.CreateAttachment(request.Documentation) : (Guid?)null;
            var swaggerId = request.Swagger != null ? await _attachmentService.CreateAttachment(request.Swagger) : (Guid?)null;
            var tourId = request.Tour != null ? await _attachmentService.CreateAttachment(request.Tour) : (Guid?)null;

            // Step 4: Create APIEndpoint object
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

            var apiEndpointTags = apiTags.Select(tag => new APIEndpointTag
            {
                Id = Guid.NewGuid(),
                APIEndpointId = apiEndpoint.Id,
                APITagId = tag.Id
            }).ToList();

            // Step 5: Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    const string insertAPIEndpointQuery = @"
                        INSERT INTO APIEndpoints (Id, ApiName, ApiScope, ApiScopeProduction, Deprecated, Description, Documentation, EndpointUrls, AppEnvironment, Swagger, Tour, ApiVersion, Langcode, Sticky, Promote, UrlAlias, Published)
                        VALUES (@Id, @ApiName, @ApiScope, @ApiScopeProduction, @Deprecated, @Description, @Documentation, @EndpointUrls, @AppEnvironment, @Swagger, @Tour, @ApiVersion, @Langcode, @Sticky, @Promote, @UrlAlias, @Published)";
                    await _dbConnection.ExecuteAsync(insertAPIEndpointQuery, apiEndpoint, transaction);

                    const string insertAPIEndpointTagsQuery = @"
                        INSERT INTO APIEndpointTags (Id, APIEndpointId, APITagId)
                        VALUES (@Id, @APIEndpointId, @APITagId)";
                    await _dbConnection.ExecuteAsync(insertAPIEndpointTagsQuery, apiEndpointTags, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "A technical exception has occurred, please contact your system administrator.");
                }
            }

            return apiEndpoint.Id.ToString();
        }

        public async Task<APIEndpoint> GetAPIEndpoint(APIEndpointRequestDto request)
        {
            // Step 1: Validate Request Payload
            if (request.Id == null && string.IsNullOrEmpty(request.ApiName))
            {
                throw new BusinessException("DP-422", "Validation error: Id or ApiName is null or empty.");
            }

            APIEndpoint apiEndpoint;
            if (request.Id != null)
            {
                const string selectAPIEndpointByIdQuery = @"
                    SELECT * FROM APIEndpoints WHERE Id = @Id";
                apiEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>(selectAPIEndpointByIdQuery, new { Id = request.Id });
            }
            else
            {
                const string selectAPIEndpointByNameQuery = @"
                    SELECT * FROM APIEndpoints WHERE ApiName = @ApiName";
                apiEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>(selectAPIEndpointByNameQuery, new { ApiName = request.ApiName });
            }

            if (apiEndpoint == null)
            {
                throw new TechnicalException("DP-404", "APIEndpoint not found.");
            }

            // Step 2: Fetch Associated Tags
            const string selectAPIEndpointTagsQuery = @"
                SELECT * FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId";
            var apiEndpointTags = await _dbConnection.QueryAsync<APIEndpointTag>(selectAPIEndpointTagsQuery, new { APIEndpointId = apiEndpoint.Id });

            var apiTagIds = apiEndpointTags.Select(tag => tag.APITagId).ToList();

            const string selectApiTagsQuery = @"
                SELECT * FROM ApiTags WHERE Id IN @Ids";
            var apiTags = await _dbConnection.QueryAsync<ApiTag>(selectApiTagsQuery, new { Ids = apiTagIds });

            if (apiTags.Any(tag => tag == null))
            {
                throw new TechnicalException("DP-404", "One or more ApiTags not found.");
            }

            apiEndpoint.ApiTags = apiTags.ToList();

            return apiEndpoint;
        }

        public async Task<string> UpdateAPIEndpoint(UpdateAPIEndpointDto request)
        {
            // Step 1: Validate UpdateAPIEndpointDto
            if (request.Id == null || string.IsNullOrEmpty(request.ApiName) || string.IsNullOrEmpty(request.Langcode) || string.IsNullOrEmpty(request.UrlAlias))
            {
                throw new BusinessException("DP-422", "Validation error: Id, ApiName, Langcode, or UrlAlias is null or empty.");
            }

            // Step 2: Fetch Existing API Endpoint
            const string selectAPIEndpointQuery = @"
                SELECT * FROM APIEndpoints WHERE Id = @Id";
            var existingEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>(selectAPIEndpointQuery, new { Id = request.Id });

            if (existingEndpoint == null)
            {
                throw new TechnicalException("DP-404", "APIEndpoint not found.");
            }

            // Step 3: Fetch and validate related entities
            var appEnvironment = await _appEnvironmentService.GetAppEnvironment(new AppEnvironmentRequestDto { Id = request.AppEnvironment });
            if (appEnvironment == null)
            {
                throw new TechnicalException("DP-404", "AppEnvironment not found.");
            }

            var apiTags = new List<ApiTag>();
            if (request.ApiTags != null)
            {
                foreach (var tagName in request.ApiTags)
                {
                    var apiTag = await _apiTagService.GetApiTag(new ApiTagRequestDto { Name = tagName });
                    if (apiTag == null)
                    {
                        apiTag = await _apiTagService.CreateApiTag(new CreateApiTagDto { Name = tagName });
                    }
                    apiTags.Add(apiTag);
                }
            }

            // Step 4: Handle Attachments
            if (request.Documentation != null)
            {
                await _attachmentService.HandleAttachment(request.Documentation, existingEndpoint.Documentation, id => existingEndpoint.Documentation = id);
            }
            if (request.Swagger != null)
            {
                await _attachmentService.HandleAttachment(request.Swagger, existingEndpoint.Swagger, id => existingEndpoint.Swagger = id);
            }
            if (request.Tour != null)
            {
                await _attachmentService.HandleAttachment(request.Tour, existingEndpoint.Tour, id => existingEndpoint.Tour = id);
            }

            // Step 5: Update APIEndpoint object
            existingEndpoint.ApiName = request.ApiName;
            existingEndpoint.ApiScope = request.ApiScope;
            existingEndpoint.ApiScopeProduction = request.ApiScopeProduction;
            existingEndpoint.Deprecated = request.Deprecated;
            existingEndpoint.Description = request.Description;
            existingEndpoint.EndpointUrls = request.EndpointUrls;
            existingEndpoint.AppEnvironment = request.AppEnvironment;
            existingEndpoint.ApiVersion = request.ApiVersion;
            existingEndpoint.Langcode = request.Langcode;
            existingEndpoint.Sticky = request.Sticky;
            existingEndpoint.Promote = request.Promote;
            existingEndpoint.UrlAlias = request.UrlAlias;
            existingEndpoint.Published = request.Published;

            // Step 6: Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try:
                    // Remove Old Tags
                    const string deleteOldTagsQuery = @"
                        DELETE FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId";
                    await _dbConnection.ExecuteAsync(deleteOldTagsQuery, new { APIEndpointId = existingEndpoint.Id }, transaction);

                    // Add New Tags
                    var newTags = apiTags.Select(tag => new APIEndpointTag
                    {
                        Id = Guid.NewGuid(),
                        APIEndpointId = existingEndpoint.Id,
                        APITagId = tag.Id
                    }).ToList();

                    const string insertNewTagsQuery = @"
                        INSERT INTO APIEndpointTags (Id, APIEndpointId, APITagId)
                        VALUES (@Id, @APIEndpointId, @APITagId)";
                    await _dbConnection.ExecuteAsync(insertNewTagsQuery, newTags, transaction);

                    // Update APIEndpoint
                    const string updateAPIEndpointQuery = @"
                        UPDATE APIEndpoints SET
                            ApiName = @ApiName,
                            ApiScope = @ApiScope,
                            ApiScopeProduction = @ApiScopeProduction,
                            Deprecated = @Deprecated,
                            Description = @Description,
                            Documentation = @Documentation,
                            EndpointUrls = @EndpointUrls,
                            AppEnvironment = @AppEnvironment,
                            Swagger = @Swagger,
                            Tour = @Tour,
                            ApiVersion = @ApiVersion,
                            Langcode = @Langcode,
                            Sticky = @Sticky,
                            Promote = @Promote,
                            UrlAlias = @UrlAlias,
                            Published = @Published
                        WHERE Id = @Id";
                    await _dbConnection.ExecuteAsync(updateAPIEndpointQuery, existingEndpoint, transaction);

                    transaction.Commit();
                except Exception:
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "A technical exception has occurred, please contact your system administrator.");
            }

            return existingEndpoint.Id.ToString();
        }

        public async Task<bool> DeleteAPIEndpoint(DeleteAPIEndpointDto request)
        {
            // Step 1: Validate Request Payload
            if (request.Id == null)
            {
                throw new BusinessException("DP-422", "Validation error: Id is null.");
            }

            // Step 2: Fetch Existing API Endpoint
            const string selectAPIEndpointQuery = @"
                SELECT * FROM APIEndpoints WHERE Id = @Id";
            var existingEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>(selectAPIEndpointQuery, new { Id = request.Id });

            if (existingEndpoint == null)
            {
                throw new TechnicalException("DP-404", "APIEndpoint not found.");
            }

            // Step 3: Delete Related Attachments
            if (existingEndpoint.Documentation != null)
            {
                await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingEndpoint.Documentation.Value });
            }
            if (existingEndpoint.Swagger != null)
            {
                await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingEndpoint.Swagger.Value });
            }
            if (existingEndpoint.Tour != null)
            {
                await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingEndpoint.Tour.Value });
            }

            // Step 4: Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try:
                    // Delete APIEndpointTags
                    const string deleteAPIEndpointTagsQuery = @"
                        DELETE FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId";
                    await _dbConnection.ExecuteAsync(deleteAPIEndpointTagsQuery, new { APIEndpointId = existingEndpoint.Id }, transaction);

                    // Delete APIEndpoint
                    const string deleteAPIEndpointQuery = @"
                        DELETE FROM APIEndpoints WHERE Id = @Id";
                    await _dbConnection.ExecuteAsync(deleteAPIEndpointQuery, new { Id = existingEndpoint.Id }, transaction);

                    transaction.Commit();
                except Exception:
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "A technical exception has occurred, please contact your system administrator.");
            }

            return True;
        }

        public async Task<List<APIEndpoint>> GetListAPIEndpoint(ListAPIEndpointRequestDto request)
        {
            // Step 1: Validate ListApiTagRequestDto
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Validation error: PageLimit or PageOffset is invalid.");
            }

            // Step 2: Fetch API Endpoints
            var sortField = string.IsNullOrEmpty(request.SortField) ? "Id" : request.SortField;
            var sortOrder = string.IsNullOrEmpty(request.SortOrder) ? "asc" : request.SortOrder;

            const string selectAPIEndpointsQuery = @"
                SELECT * FROM APIEndpoints
                ORDER BY @SortField @SortOrder
                OFFSET @PageOffset ROWS
                FETCH NEXT @PageLimit ROWS ONLY";
            var apiEndpoints = await _dbConnection.QueryAsync<APIEndpoint>(selectAPIEndpointsQuery, new { SortField = sortField, SortOrder = sortOrder, PageOffset = request.PageOffset, PageLimit = request.PageLimit });

            // Step 3: Fetch Related Tags
            var apiEndpointIds = apiEndpoints.Select(endpoint => endpoint.Id).ToList();

            const string selectAPIEndpointTagsQuery = @"
                SELECT * FROM APIEndpointTags WHERE APIEndpointId IN @APIEndpointIds";
            var apiEndpointTags = await _dbConnection.QueryAsync<APIEndpointTag>(selectAPIEndpointTagsQuery, new { APIEndpointIds = apiEndpointIds });

            var apiTagIds = apiEndpointTags.Select(tag => tag.APITagId).Distinct().ToList();

            const string selectApiTagsQuery = @"
                SELECT * FROM ApiTags WHERE Id IN @Ids";
            var apiTags = await _dbConnection.QueryAsync<ApiTag>(selectApiTagsQuery, new { Ids = apiTagIds });

            if (apiTags.Any(tag => tag == null))
            {
                throw new TechnicalException("DP-404", "One or more ApiTags not found.");
            }

            // Step 4: Map Tags to Endpoints
            foreach (var endpoint in apiEndpoints)
            {
                var tagsForEndpoint = apiEndpointTags
                    .Where(tag => tag.APIEndpointId == endpoint.Id)
                    .Select(tag => apiTags.First(t => t.Id == tag.APITagId))
                    .ToList();
                endpoint.ApiTags = tagsForEndpoint;
            }

            return apiEndpoints.ToList();
        }
    }
}
