
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
            // Step 1: Validate the request payload
            if (string.IsNullOrEmpty(request.ApiName) || string.IsNullOrEmpty(request.Langcode))
            {
                throw new BusinessException("DP-422", "Missing required parameters");
            }

            // Step 2: Fetch and Validate Related Entities
            var appEnvironmentRequest = new AppEnvironmentRequestDto { Id = request.AppEnvironment };
            var appEnvironment = await _appEnvironmentService.GetAppEnvironment(appEnvironmentRequest);
            if (appEnvironment == null)
            {
                throw new BusinessException("DP-404", "AppEnvironment not found");
            }

            // Step 3: Fetch or Create API Tags
            var apiTags = new List<ApiTag>();
            foreach (var tagId in request.ApiTags)
            {
                var apiTagRequest = new ApiTagRequestDto { Id = tagId };
                var apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                if (apiTag == null)
                {
                    var createApiTagDto = new CreateApiTagDto { Name = tagId.ToString() };
                    var newApiTagId = await _apiTagService.CreateApiTag(createApiTagDto);
                    apiTag = new ApiTag { Id = Guid.Parse(newApiTagId), Name = tagId.ToString() };
                }
                apiTags.Add(apiTag);
            }

            // Step 4: Upload Attachment Files
            var documentationId = await UploadAttachment(request.Documentation, "documentation");
            var swaggerId = await UploadAttachment(request.Swagger, "swagger");
            var tourId = await UploadAttachment(request.Tour, "tour");

            // Step 5: Create a new APIEndpoint object
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

            // Step 6: Create a new list of APIEndpointTags type objects
            var apiEndpointTags = apiTags.Select(apiTag => new APIEndpointTag
            {
                Id = Guid.NewGuid(),
                APIEndpointId = apiEndpoint.Id,
                APITagId = apiTag.Id
            }).ToList();

            // Step 7: In a single SQL transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    var sql = @"
                        INSERT INTO APIEndpoints (Id, ApiName, ApiScope, ApiScopeProduction, Deprecated, Description, Documentation, EndpointUrls, AppEnvironment, Swagger, Tour, ApiVersion, Langcode, Sticky, Promote, UrlAlias, Published)
                        VALUES (@Id, @ApiName, @ApiScope, @ApiScopeProduction, @Deprecated, @Description, @Documentation, @EndpointUrls, @AppEnvironment, @Swagger, @Tour, @ApiVersion, @Langcode, @Sticky, @Promote, @UrlAlias, @Published);
                    ";
                    await _dbConnection.ExecuteAsync(sql, apiEndpoint, transaction);

                    var sqlTags = @"
                        INSERT INTO APIEndpointTags (Id, APIEndpointId, APITagId)
                        VALUES (@Id, @APIEndpointId, @APITagId);
                    ";
                    await _dbConnection.ExecuteAsync(sqlTags, apiEndpointTags, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "A technical exception has occurred, please contact your system administrator");
                }
            }

            return apiEndpoint.Id.ToString();
        }

        private async Task<Guid> UploadAttachment(Guid fileId, string fileName)
        {
            var createAttachmentDto = new CreateAttachmentDto { FileName = fileName, FileUrl = fileId.ToByteArray() };
            var attachmentId = await _attachmentService.CreateAttachment(createAttachmentDto);
            return Guid.Parse(attachmentId);
        }

        public async Task<APIEndpoint> GetAPIEndpoint(APIEndpointRequestDto request)
        {
            // Step 1: Validate Request Payload
            if (request.Id == null && string.IsNullOrEmpty(request.ApiName))
            {
                throw new BusinessException("DP-422", "Missing required parameters");
            }

            // Step 2: Fetch API Endpoint
            APIEndpoint apiEndpoint;
            if (request.Id != null)
            {
                apiEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>("SELECT * FROM APIEndpoints WHERE Id = @Id", new { Id = request.Id });
            }
            else
            {
                apiEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>("SELECT * FROM APIEndpoints WHERE ApiName = @ApiName", new { ApiName = request.ApiName });
            }

            if (apiEndpoint == null)
            {
                throw new BusinessException("DP-404", "APIEndpoint not found");
            }

            // Step 3: Fetch Associated Tags
            var apiTagIds = await _dbConnection.QueryAsync<Guid>("SELECT APITagId FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId", new { APIEndpointId = apiEndpoint.Id });
            var apiTags = new List<ApiTag>();
            foreach (var tagId in apiTagIds)
            {
                var apiTagRequest = new ApiTagRequestDto { Id = tagId };
                var apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                if (apiTag == null)
                {
                    throw new BusinessException("DP-404", "ApiTag not found");
                }
                apiTags.Add(apiTag);
            }

            apiEndpoint.ApiTags = apiTags;
            return apiEndpoint;
        }

        public async Task<string> UpdateAPIEndpoint(UpdateAPIEndpointDto request)
        {
            // Step 1: Validate Request Payload
            if (request.Id == null || string.IsNullOrEmpty(request.ApiName) || string.IsNullOrEmpty(request.Langcode) || string.IsNullOrEmpty(request.UrlAlias))
            {
                throw new BusinessException("DP-422", "Missing required parameters");
            }

            // Step 2: Fetch Existing API Endpoint
            var existingAPIEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>("SELECT * FROM APIEndpoints WHERE Id = @Id", new { Id = request.Id });
            if (existingAPIEndpoint == null)
            {
                throw new BusinessException("DP-404", "APIEndpoint not found");
            }

            // Step 3: Fetch or Create API Tags
            var apiTags = new List<ApiTag>();
            foreach (var tagId in request.ApiTags)
            {
                var apiTagRequest = new ApiTagRequestDto { Id = tagId };
                var apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                if (apiTag == null)
                {
                    var createApiTagDto = new CreateApiTagDto { Name = tagId.ToString() };
                    var newApiTagId = await _apiTagService.CreateApiTag(createApiTagDto);
                    apiTag = new ApiTag { Id = Guid.Parse(newApiTagId), Name = tagId.ToString() };
                }
                apiTags.Add(apiTag);
            }

            // Step 4: Update API Endpoint
            existingAPIEndpoint.ApiName = request.ApiName;
            existingAPIEndpoint.ApiScope = request.ApiScope;
            existingAPIEndpoint.ApiScopeProduction = request.ApiScopeProduction;
            existingAPIEndpoint.Deprecated = request.Deprecated;
            existingAPIEndpoint.Description = request.Description;
            existingAPIEndpoint.Documentation = request.Documentation;
            existingAPIEndpoint.EndpointUrls = request.EndpointUrls;
            existingAPIEndpoint.AppEnvironment = request.AppEnvironment;
            existingAPIEndpoint.Swagger = request.Swagger;
            existingAPIEndpoint.Tour = request.Tour;
            existingAPIEndpoint.ApiVersion = request.ApiVersion;
            existingAPIEndpoint.Langcode = request.Langcode;
            existingAPIEndpoint.Sticky = request.Sticky;
            existingAPIEndpoint.Promote = request.Promote;
            existingAPIEndpoint.UrlAlias = request.UrlAlias;
            existingAPIEndpoint.Published = request.Published;

            // Step 5: Update API Endpoint in the database
            var sql = @"
                UPDATE APIEndpoints 
                SET ApiName = @ApiName, ApiScope = @ApiScope, ApiScopeProduction = @ApiScopeProduction, Deprecated = @Deprecated, Description = @Description, Documentation = @Documentation, EndpointUrls = @EndpointUrls, AppEnvironment = @AppEnvironment, Swagger = @Swagger, Tour = @Tour, ApiVersion = @ApiVersion, Langcode = @Langcode, Sticky = @Sticky, Promote = @Promote, UrlAlias = @UrlAlias, Published = @Published 
                WHERE Id = @Id;
            ";
            await _dbConnection.ExecuteAsync(sql, existingAPIEndpoint);

            // Step 6: Update API Endpoint Tags
            await _dbConnection.ExecuteAsync("DELETE FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId", new { APIEndpointId = existingAPIEndpoint.Id });
            var apiEndpointTags = apiTags.Select(apiTag => new APIEndpointTag
            {
                Id = Guid.NewGuid(),
                APIEndpointId = existingAPIEndpoint.Id,
                APITagId = apiTag.Id
            }).ToList();
            var sqlTags = @"
                INSERT INTO APIEndpointTags (Id, APIEndpointId, APITagId)
                VALUES (@Id, @APIEndpointId, @APITagId);
            ";
            await _dbConnection.ExecuteAsync(sqlTags, apiEndpointTags);

            return existingAPIEndpoint.Id.ToString();
        }
    }
}
