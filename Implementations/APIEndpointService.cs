
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
            var existingEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>("SELECT * FROM ApiEndpoints WHERE Id = @Id", new { request.Id });
            if (existingEndpoint == null)
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

            // Step 4: Handle ApiTags
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

            // Step 5: Handle ApiTags Removal
            var existingTags = await _dbConnection.QueryAsync<ApiTag>("SELECT * FROM ApiTags WHERE Id IN (SELECT ApiTagId FROM APIEndpointTags WHERE APIEndpointId = @Id)", new { request.Id });
            var tagsToRemove = existingTags.Where(existingTag => !apiTags.Any(tag => tag.Id == existingTag.Id)).ToList();

            // Step 6: Handle ApiTags Addition
            var newTags = apiTags.Where(tag => !existingTags.Any(existingTag => existingTag.Id == tag.Id)).ToList();

            // Step 7: Handle Attachments
            await _attachmentService.HandleAttachment(request.Documentation, existingEndpoint.Documentation, (newDocId) => existingEndpoint.Documentation = newDocId);
            await _attachmentService.HandleAttachment(request.Swagger, existingEndpoint.Swagger, (newSwaggerId) => existingEndpoint.Swagger = newSwaggerId);
            await _attachmentService.HandleAttachment(request.Tour, existingEndpoint.Tour, (newTourId) => existingEndpoint.Tour = newTourId);

            // Step 8: Update the APIEndpoint object
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
            existingEndpoint.ApiTags = apiTags;

            // Step 9: Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Remove Old Tags
                    foreach (var tag in tagsToRemove)
                    {
                        await _dbConnection.ExecuteAsync("DELETE FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId AND ApiTagId = @ApiTagId", new { request.Id, tag.Id }, transaction);
                    }

                    // Add New Tags
                    foreach (var tag in newTags)
                    {
                        await _dbConnection.ExecuteAsync("INSERT INTO APIEndpointTags (Id, APIEndpointId, ApiTagId) VALUES (@Id, @APIEndpointId, @ApiTagId)", new { Id = Guid.NewGuid(), request.Id, tag.Id }, transaction);
                    }

                    // Update APIEndpoint
                    await _dbConnection.ExecuteAsync("UPDATE ApiEndpoints SET ApiName = @ApiName, ApiScope = @ApiScope, ApiScopeProduction = @ApiScopeProduction, Deprecated = @Deprecated, Description = @Description, EndpointUrls = @EndpointUrls, AppEnvironment = @AppEnvironment, ApiVersion = @ApiVersion, Langcode = @Langcode, Sticky = @Sticky, Promote = @Promote, UrlAlias = @UrlAlias, Published = @Published WHERE Id = @Id", existingEndpoint, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
                }
            }

            return existingEndpoint.Id.ToString();
        }
    }
}
