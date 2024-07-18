
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

            // Step 3: Fetch and validate related entities
            var appEnvironmentRequest = new AppEnvironmentRequestDto { Id = request.AppEnvironment };
            var appEnvironment = await _appEnvironmentService.GetAppEnvironment(appEnvironmentRequest);

            if (appEnvironment == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 4: Handle Tags
            List<ApiTag> apiTags = new List<ApiTag>();
            if (request.ApiTags != null)
            {
                foreach (var tagName in request.ApiTags)
                {
                    var apiTagRequest = new ApiTagRequestDto { Name = tagName };
                    var apiTag = await _apiTagService.GetApiTag(apiTagRequest);

                    if (apiTag == null)
                    {
                        var createApiTagDto = new CreateApiTagDto { Name = tagName };
                        await _apiTagService.CreateApiTag(createApiTagDto);
                        apiTag = await _apiTagService.GetApiTag(apiTagRequest);
                    }

                    apiTags.Add(apiTag);
                }
            }

            // Step 5: Handle Tags Removal and Addition
            var existingTags = await _dbConnection.QueryAsync<ApiTag>(
                "SELECT t.* FROM ApiTags t INNER JOIN ApiEndpointTags et ON t.Id = et.ApiTagId WHERE et.ApiEndpointId = @Id",
                new { request.Id });

            var tagsToRemove = existingTags.Where(et => !apiTags.Any(t => t.Id == et.Id)).ToList();
            var tagsToAdd = apiTags.Where(t => !existingTags.Any(et => et.Id == t.Id)).ToList();

            // Step 6: Handle Attachments
            if (request.Documentation != null)
            {
                await _attachmentService.HandleAttachment(request.Documentation, existingAPIEndpoint.Documentation, (newDocId) => existingAPIEndpoint.Documentation = newDocId);
            }

            if (request.Swagger != null)
            {
                await _attachmentService.HandleAttachment(request.Swagger, existingAPIEndpoint.Swagger, (newSwaggerId) => existingAPIEndpoint.Swagger = newSwaggerId);
            }

            if (request.Tour != null)
            {
                await _attachmentService.HandleAttachment(request.Tour, existingAPIEndpoint.Tour, (newTourId) => existingAPIEndpoint.Tour = newTourId);
            }

            // Step 7: Update APIEndpoint object
            existingAPIEndpoint.ApiName = request.ApiName;
            existingAPIEndpoint.ApiScope = request.ApiScope;
            existingAPIEndpoint.ApiScopeProduction = request.ApiScopeProduction;
            existingAPIEndpoint.Deprecated = request.Deprecated;
            existingAPIEndpoint.Description = request.Description;
            existingAPIEndpoint.EndpointUrls = request.EndpointUrls;
            existingAPIEndpoint.AppEnvironment = request.AppEnvironment;
            existingAPIEndpoint.ApiVersion = request.ApiVersion;
            existingAPIEndpoint.Langcode = request.Langcode;
            existingAPIEndpoint.Sticky = request.Sticky;
            existingAPIEndpoint.Promote = request.Promote;
            existingAPIEndpoint.UrlAlias = request.UrlAlias;
            existingAPIEndpoint.Published = request.Published;
            existingAPIEndpoint.ApiTags = apiTags;

            // Step 8: Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Remove Old Tags
                    foreach (var tag in tagsToRemove)
                    {
                        await _dbConnection.ExecuteAsync(
                            "DELETE FROM ApiEndpointTags WHERE ApiEndpointId = @ApiEndpointId AND ApiTagId = @ApiTagId",
                            new { ApiEndpointId = request.Id, ApiTagId = tag.Id },
                            transaction);
                    }

                    // Add New Tags
                    foreach (var tag in tagsToAdd)
                    {
                        await _dbConnection.ExecuteAsync(
                            "INSERT INTO ApiEndpointTags (Id, ApiEndpointId, ApiTagId) VALUES (@Id, @ApiEndpointId, @ApiTagId)",
                            new { Id = Guid.NewGuid(), ApiEndpointId = request.Id, ApiTagId = tag.Id },
                            transaction);
                    }

                    // Update APIEndpoint
                    await _dbConnection.ExecuteAsync(
                        "UPDATE ApiEndpoints SET ApiName = @ApiName, ApiScope = @ApiScope, ApiScopeProduction = @ApiScopeProduction, Deprecated = @Deprecated, Description = @Description, EndpointUrls = @EndpointUrls, AppEnvironment = @AppEnvironment, ApiVersion = @ApiVersion, Langcode = @Langcode, Sticky = @Sticky, Promote = @Promote, UrlAlias = @UrlAlias, Published = @Published WHERE Id = @Id",
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

            return existingAPIEndpoint.Id.ToString();
        }
    }
}
