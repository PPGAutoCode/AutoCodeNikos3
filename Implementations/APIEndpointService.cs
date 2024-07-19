
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
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch and Validate Related Entities
            var appEnvironmentRequestDto = new AppEnvironmentRequestDto { Id = request.AppEnvironment };
            var appEnvironment = await _appEnvironmentService.GetAppEnvironment(appEnvironmentRequestDto);
            if (appEnvironment == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Get the apiTags names from request.apiTags and fetch the whole entity of ApiTag
            var apiTagIds = new List<Guid>();
            if (request.ApiTags != null)
            {
                foreach (var tagName in request.ApiTags)
                {
                    var apiTagRequestDto = new ApiTagRequestDto { Name = tagName };
                    var apiTag = await _apiTagService.GetApiTag(apiTagRequestDto);
                    if (apiTag == null)
                    {
                        var createApiTagDto = new CreateApiTagDto { Name = tagName };
                        var newApiTagId = await _apiTagService.CreateApiTag(createApiTagDto);
                        apiTagIds.Add(Guid.Parse(newApiTagId));
                    }
                    else
                    {
                        apiTagIds.Add(apiTag.Id);
                    }
                }
            }

            // Step 4: Upload Attachment Files
            Guid? documentationId = null;
            Guid? swaggerId = null;
            Guid? tourId = null;

            if (request.Documentation != null)
            {
                documentationId = Guid.Parse(await _attachmentService.CreateAttachment(request.Documentation));
            }
            if (request.Swagger != null)
            {
                swaggerId = Guid.Parse(await _attachmentService.CreateAttachment(request.Swagger));
            }
            if (request.Tour != null)
            {
                tourId = Guid.Parse(await _attachmentService.CreateAttachment(request.Tour));
            }

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

            // Step 7: In a single SQL transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    var sqlInsertAPIEndpoint = @"
                        INSERT INTO APIEndpoints (Id, ApiName, ApiScope, ApiScopeProduction, Deprecated, Description, Documentation, EndpointUrls, AppEnvironment, Swagger, Tour, ApiVersion, Langcode, Sticky, Promote, UrlAlias, Published)
                        VALUES (@Id, @ApiName, @ApiScope, @ApiScopeProduction, @Deprecated, @Description, @Documentation, @EndpointUrls, @AppEnvironment, @Swagger, @Tour, @ApiVersion, @Langcode, @Sticky, @Promote, @UrlAlias, @Published)";
                    await _dbConnection.ExecuteAsync(sqlInsertAPIEndpoint, apiEndpoint, transaction);

                    var sqlInsertAPIEndpointTags = @"
                        INSERT INTO APIEndpointTags (Id, APIEndpointId, APITagId)
                        VALUES (@Id, @APIEndpointId, @APITagId)";
                    await _dbConnection.ExecuteAsync(sqlInsertAPIEndpointTags, apiEndpointTags, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
                }
            }

            // Step 8: Return the APIEndpoint.Id from the database
            return apiEndpoint.Id.ToString();
        }

        public async Task<APIEndpoint> GetAPIEndpoint(APIEndpointRequestDto request)
        {
            // Step 1: Validate Request Payload
            if (request.Id == null && string.IsNullOrEmpty(request.ApiName))
            {
                throw new BusinessException("DP-422", "Client Error");
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
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Fetch Associated Tags
            var apiTagIds = await _dbConnection.QueryAsync<Guid>("SELECT APITagId FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId", new { APIEndpointId = apiEndpoint.Id });

            var apiTags = new List<ApiTag>();
            foreach (var tagId in apiTagIds)
            {
                var apiTagRequestDto = new ApiTagRequestDto { Id = tagId };
                var apiTag = await _apiTagService.GetApiTag(apiTagRequestDto);
                if (apiTag == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
                apiTags.Add(apiTag);
            }

            // Step 4: Map and Return APIEndpoint
            apiEndpoint.ApiTags = apiTags;
            return apiEndpoint;
        }
    }
}
