
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
        private readonly IApiTagService _apiTagService;
        private readonly IAttachmentService _attachmentService;

        public APIEndpointService(IDbConnection dbConnection, IApiTagService apiTagService, IAttachmentService attachmentService)
        {
            _dbConnection = dbConnection;
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

            // Step 3: Fetch ApiTags
            var apiTags = new List<ApiTag>();
            if (request.ApiTags != null)
            {
                foreach (var tagName in request.ApiTags)
                {
                    var apiTagRequestDto = new ApiTagRequestDto { Name = tagName };
                    var apiTag = await _apiTagService.GetApiTag(apiTagRequestDto);
                    if (apiTag != null)
                    {
                        apiTags.Add(apiTag);
                    }
                }
            }

            // Step 4: Create new ApiTags if they don't exist
            foreach (var tagName in request.ApiTags.Where(tagName => !apiTags.Any(at => at.Name == tagName)))
            {
                var createApiTagDto = new CreateApiTagDto { Name = tagName };
                await _apiTagService.CreateApiTag(createApiTagDto);
            }

            // Step 5: Handle Tags Removal
            var existingTags = await _dbConnection.QueryAsync<ApiTag>("SELECT * FROM ApiTags WHERE Id IN (SELECT ApiTagId FROM APIEndpointTags WHERE APIEndpointId = @Id)", new { request.Id });
            var tagsToRemove = existingTags.Where(et => !request.ApiTags.Contains(et.Name)).ToList();

            // Step 6: Handle Tags Addition
            var newTags = request.ApiTags.Where(tagName => !existingTags.Any(et => et.Name == tagName)).ToList();

            // Step 7: Handle Attachments
            if (request.Documentation != null)
            {
                await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingEndpoint.Documentation });
                existingEndpoint.Documentation = Guid.Parse(await _attachmentService.CreateAttachment(request.Documentation));
            }
            if (request.Swagger != null)
            {
                await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingEndpoint.Swagger });
                existingEndpoint.Swagger = Guid.Parse(await _attachmentService.CreateAttachment(request.Swagger));
            }
            if (request.Tour != null)
            {
                await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = existingEndpoint.Tour });
                existingEndpoint.Tour = Guid.Parse(await _attachmentService.CreateAttachment(request.Tour));
            }

            // Step 9: Update the APIEndpoint object
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

            // Step 10: Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Step 11: Remove Old Tags
                    foreach (var tag in tagsToRemove)
                    {
                        await _dbConnection.ExecuteAsync("DELETE FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId AND ApiTagId = @ApiTagId", new { APIEndpointId = request.Id, ApiTagId = tag.Id }, transaction);
                    }

                    // Step 12: Add New Tags
                    foreach (var tagName in newTags)
                    {
                        var newApiTag = await _apiTagService.GetApiTag(new ApiTagRequestDto { Name = tagName });
                        await _dbConnection.ExecuteAsync("INSERT INTO APIEndpointTags (APIEndpointId, ApiTagId) VALUES (@APIEndpointId, @ApiTagId)", new { APIEndpointId = request.Id, ApiTagId = newApiTag.Id }, transaction);
                    }

                    // Step 13: Update APIEndpoint object in the database table
                    await _dbConnection.ExecuteAsync("UPDATE ApiEndpoints SET ApiName = @ApiName, ApiScope = @ApiScope, ApiScopeProduction = @ApiScopeProduction, Deprecated = @Deprecated, Description = @Description, Documentation = @Documentation, EndpointUrls = @EndpointUrls, AppEnvironment = @AppEnvironment, Swagger = @Swagger, Tour = @Tour, ApiVersion = @ApiVersion, Langcode = @Langcode, Sticky = @Sticky, Promote = @Promote, UrlAlias = @UrlAlias, Published = @Published WHERE Id = @Id", existingEndpoint, transaction);

                    // Step 14: Commit the transaction
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
                }
            }

            // Step 15: Return Result
            return existingEndpoint.Id.ToString();
        }
    }
}
