
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
            // 1. Validate required parameters
            if (string.IsNullOrEmpty(request.ApiName) || string.IsNullOrEmpty(request.Langcode))
            {
                throw new BusinessException("DP-422", "Required parameters are missing.");
            }

            // 2. Fetch and Validate Related Entities
            var appEnvironmentRequest = new AppEnvironmentRequestDto { Id = request.AppEnvironment };
            var appEnvironment = await _appEnvironmentService.GetAppEnvironment(appEnvironmentRequest);
            if (appEnvironment == null)
            {
                throw new TechnicalException("DP-404", "App environment not found.");
            }

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
                    var newApiTagId = await _apiTagService.CreateApiTag(new CreateApiTagDto { Name = tagName });
                    apiTagIds.Add(new Guid(newApiTagId));
                }
            }

            // 5. Upload Attachment Files
            Guid? documentationId = null;
            Guid? swaggerId = null;
            Guid? tourId = null;

            if (request.Documentation != null)
            {
                documentationId = Guid.Parse(await _attachmentService.CreateAttachment(new CreateAttachmentDto { FileName = request.Documentation.FileName, File = request.Documentation.File }));
            }
            if (request.Swagger != null)
            {
                swaggerId = Guid.Parse(await _attachmentService.CreateAttachment(new CreateAttachmentDto { FileName = request.Swagger.FileName, File = request.Swagger.File }));
            }
            if (request.Tour != null)
            {
                tourId = Guid.Parse(await _attachmentService.CreateAttachment(new CreateAttachmentDto { FileName = request.Tour.FileName, File = request.Tour.File }));
            }

            // 6. Create APIEndpoint object
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

            // 7. Create APIEndpointTags objects
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

            // 8. In a single SQL transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Insert apiEndpoint
                    var insertApiEndpointQuery = @"INSERT INTO APIEndpoints (Id, ApiName, ApiScope, ApiScopeProduction, Deprecated, Description, Documentation, EndpointUrls, AppEnvironment, Swagger, Tour, ApiVersion, Langcode, Sticky, Promote, UrlAlias, Published) 
                                                    VALUES (@Id, @ApiName, @ApiScope, @ApiScopeProduction, @Deprecated, @Description, @Documentation, @EndpointUrls, @AppEnvironment, @Swagger, @Tour, @ApiVersion, @Langcode, @Sticky, @Promote, @UrlAlias, @Published)";
                    await _dbConnection.ExecuteAsync(insertApiEndpointQuery, apiEndpoint, transaction);

                    // Insert apiEndpointTags
                    var insertApiEndpointTagsQuery = @"INSERT INTO APIEndpointTags (Id, APIEndpointId, APITagId) 
                                                        VALUES (@Id, @APIEndpointId, @APITagId)";
                    foreach (var tag in apiEndpointTags)
                    {
                        await _dbConnection.ExecuteAsync(insertApiEndpointTagsQuery, tag, transaction);
                    }

                    // Commit transaction
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "An error occurred while creating the API endpoint.");
                }
            }

            // 9. Return the APIEndpoint Id
            return apiEndpoint.Id.ToString();
        }
        
        
        public async Task<APIEndpoint> GetAPIEndpoint(APIEndpointRequestDto request)
        {
            // 1. Validate Request Payload
            if (request.Id == Guid.Empty && string.IsNullOrEmpty(request.ApiName))
            {
                throw new BusinessException("DP-422", "Either Id or ApiName must be provided.");
            }

            // 2. Fetch API Endpoint
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

            // 3. Fetch Associated Tags
            var tagQuery = "SELECT APITagId FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId";
            var tagIds = await _dbConnection.QueryAsync<Guid>(tagQuery, new { APIEndpointId = apiEndpoint.Id });

            foreach (var tagId in tagIds)
            {
                var apiTagRequest = new ApiTagRequestDto { Id = tagId };
                var apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                if (apiTag != null)
                {
                    apiEndpoint.ApiTags.Add(apiTag);
                }
            }

            return apiEndpoint;
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
        
         public async Task<bool> DeleteAPIEndpoint(DeleteAPIEndpointDto request)
        {
            // 1. Validate Request Payload
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Id is required.");
            }

            // 2. Fetch Existing API Endpoint
            var existingApiEndpoint = await GetAPIEndpoint(new APIEndpointRequestDto { Id = request.Id });
            if (existingApiEndpoint == null)
            {
                throw new TechnicalException("DP-404", "API Endpoint not found.");
            }

            // 3. Delete Related Attachments
            if (existingApiEndpoint.Documentation != null)
            {
                await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingApiEndpoint.Documentation.Value });
            }
            if (existingApiEndpoint.Swagger != null)
            {
                await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingApiEndpoint.Swagger.Value });
            }
            if (existingApiEndpoint.Tour != null)
            {
                await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingApiEndpoint.Tour.Value });
            }

            // 4. Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Delete APIEndpointTags
                    var deleteTagsQuery = "DELETE FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId";
                    await _dbConnection.ExecuteAsync(deleteTagsQuery, new { APIEndpointId = existingApiEndpoint.Id }, transaction);

                    // Delete APIEndpoint
                    var deleteEndpointQuery = "DELETE FROM APIEndpoints WHERE Id = @Id";
                    await _dbConnection.ExecuteAsync(deleteEndpointQuery, new { Id = existingApiEndpoint.Id }, transaction);

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
            // 1. Validate pagination parameters
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "PageLimit must be greater than 0 and PageOffset must be non-negative.");
            }

            // 2. Fetch API Endpoints
            var query = "SELECT * FROM APIEndpoints ORDER BY Id OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";
            var apiEndpoints = await _dbConnection.QueryAsync<APIEndpoint>(query, new { PageOffset = request.PageOffset, PageLimit = request.PageLimit });

            // 3. Fetch and Map Related Tags
            foreach (var apiEndpoint in apiEndpoints)
            {
                var tagQuery = "SELECT APITagId FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId";
                var tagIds = await _dbConnection.QueryAsync<Guid>(tagQuery, new { APIEndpointId = apiEndpoint.Id });

                foreach (var tagId in tagIds)
                {
                    var apiTagRequest = new ApiTagRequestDto { Id = tagId };
                    var apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                    if (apiTag != null)
                    {
                        apiEndpoint.ApiTags.Add(apiTag);
                    }
                }
            }

            return apiEndpoints.AsList();
        }
    }
}
