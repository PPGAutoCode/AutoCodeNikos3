
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
                throw new BusinessException("DP-422", "Validation failed: Id, ApiName, Langcode, and UrlAlias are required.");
            }

            // Step 2: Fetch Existing API Endpoint
            var existingEndpoint = await _dbConnection.QueryFirstOrDefaultAsync<APIEndpoint>(
                "SELECT * FROM ApiEndpoints WHERE Id = @Id", new { request.Id });
            if (existingEndpoint == null)
            {
                throw new BusinessException("DP-404", "APIEndpoint not found.");
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

            // Step 4: Handle Attachments
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

            // Step 6: Database Transactions
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Remove Old Tags
                    await _dbConnection.ExecuteAsync(
                        "DELETE FROM APIEndpointTags WHERE APIEndpointId = @Id", new { existingEndpoint.Id }, transaction);

                    // Add New Tags
                    foreach (var apiTag in apiTags)
                    {
                        await _dbConnection.ExecuteAsync(
                            "INSERT INTO APIEndpointTags (APIEndpointId, ApiTagId) VALUES (@APIEndpointId, @ApiTagId)",
                            new { APIEndpointId = existingEndpoint.Id, ApiTagId = apiTag.Id }, transaction);
                    }

                    // Update APIEndpoint
                    await _dbConnection.ExecuteAsync(
                        "UPDATE ApiEndpoints SET ApiName = @ApiName, ApiScope = @ApiScope, ApiScopeProduction = @ApiScopeProduction, Deprecated = @Deprecated, Description = @Description, Documentation = @Documentation, EndpointUrls = @EndpointUrls, AppEnvironment = @AppEnvironment, Swagger = @Swagger, Tour = @Tour, ApiVersion = @ApiVersion, Langcode = @Langcode, Sticky = @Sticky, Promote = @Promote, UrlAlias = @UrlAlias, Published = @Published WHERE Id = @Id",
                        existingEndpoint, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "A technical exception has occurred, please contact your system administrator.");
                }
            }

            return existingEndpoint.Id.ToString();
        }

        public async Task<bool> DeleteAPIEndpoint(DeleteAPIEndpointDto request)
        {
            // Step 1: Validate Request Payload
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Validation failed: Id is required.");
            }

            // Step 2: Fetch Existing API Endpoint
            var existingEndpoint = await _dbConnection.QueryFirstOrDefaultAsync<APIEndpoint>(
                "SELECT * FROM ApiEndpoints WHERE Id = @Id", new { request.Id });
            if (existingEndpoint == null)
            {
                throw new BusinessException("DP-404", "APIEndpoint not found.");
            }

            // Step 3: Fetch and Validate Related Entities
            var relatedTags = await _dbConnection.QueryAsync<Guid>(
                "SELECT ApiTagId FROM APIEndpointTags WHERE APIEndpointId = @Id", new { request.Id });

            foreach (var tagId in relatedTags)
            {
                var apiTagRequestDto = new ApiTagRequestDto { Id = tagId };
                var apiTag = await _apiTagService.GetApiTag(apiTagRequestDto);
                if (apiTag == null)
                {
                    throw new BusinessException("DP-404", $"ApiTag with ID {tagId} not found.");
                }
            }

            // Step 4: Fetch and Validate AppEnvironment
            var appEnvironmentRequestDto = new AppEnvironmentRequestDto { Id = existingEndpoint.AppEnvironment };
            var appEnvironment = await _apiTagService.GetAppEnvironment(appEnvironmentRequestDto);
            if (appEnvironment == null)
            {
                throw new BusinessException("DP-404", "AppEnvironment not found.");
            }

            // Step 5: Database Transactions
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Remove APIEndpointTags associations
                    await _dbConnection.ExecuteAsync(
                        "DELETE FROM APIEndpointTags WHERE APIEndpointId = @Id", new { request.Id }, transaction);

                    // Delete APIEndpoint
                    await _dbConnection.ExecuteAsync(
                        "DELETE FROM ApiEndpoints WHERE Id = @Id", new { request.Id }, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "A technical exception has occurred, please contact your system administrator.");
                }
            }

            return true;
        }
    }
}
