
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
                    var createApiTagDto = new CreateApiTagDto { Name = tagName };
                    var newApiTagId = await _apiTagService.CreateApiTag(createApiTagDto);
                    apiTagIds.Add(new Guid(newApiTagId));
                }
            }

            // 5. Upload Attachment Files
            string documentationId = null, swaggerId = null, tourId = null;
            if (request.Documentation != null)
            {
                documentationId = await _attachmentService.CreateAttachment(new CreateAttachmentDto { FileName = request.Documentation.FileName, File = request.Documentation.File });
            }
            if (request.Swagger != null)
            {
                swaggerId = await _attachmentService.CreateAttachment(new CreateAttachmentDto { FileName = request.Swagger.FileName, File = request.Swagger.File });
            }
            if (request.Tour != null)
            {
                tourId = await _attachmentService.CreateAttachment(new CreateAttachmentDto { FileName = request.Tour.FileName, File = request.Tour.File });
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
                Documentation = documentationId != null ? Guid.Parse(documentationId) : (Guid?)null,
                EndpointUrls = request.EndpointUrls,
                AppEnvironment = appEnvironment.Id,
                Swagger = swaggerId != null ? Guid.Parse(swaggerId) : (Guid?)null,
                Tour = tourId != null ? Guid.Parse(tourId) : (Guid?)null,
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

                    transaction.Commit();
                    return apiEndpoint.Id.ToString();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "An error occurred while creating the API endpoint.");
                }
            }
        }
    }
}
