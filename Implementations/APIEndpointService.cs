
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
            var existingEndpoint = await _dbConnection.QueryFirstOrDefaultAsync<APIEndpoint>(
                "SELECT * FROM ApiEndpoints WHERE Id = @Id",
                new { request.Id });

            if (existingEndpoint == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Fetch API tags by using the service that implements the IApiTagService interface
            var apiTags = new List<ApiTag>();
            foreach (var tagName in request.ApiTags ?? new List<string>())
            {
                var apiTagRequestDto = new ApiTagRequestDto { Name = tagName };
                var apiTag = await _apiTagService.GetApiTag(apiTagRequestDto);
                if (apiTag != null)
                {
                    apiTags.Add(apiTag);
                }
                else
                {
                    var createApiTagDto = new CreateApiTagDto { Name = tagName };
                    await _apiTagService.CreateApiTag(createApiTagDto);
                }
            }

            // Step 5: Define API Tags for Removal and Addition
            var tagsToRemove = existingEndpoint.ApiTags.Where(existingTag => !request.ApiTags.Contains(existingTag.Name)).ToList();
            var tagsToAdd = request.ApiTags.Where(tagName => !existingEndpoint.ApiTags.Any(existingTag => existingTag.Name == tagName)).ToList();

            // Step 7: Handle Attachments
            if (request.Documentation != null)
            {
                var deleteAttachmentDto = new DeleteAttachmentDto { Id = existingEndpoint.Documentation };
                await _attachmentService.DeleteAttachment(deleteAttachmentDto);
                var createAttachmentDto = new CreateAttachmentDto { FileName = request.Documentation.FileName, FileUrl = request.Documentation.FileUrl };
                existingEndpoint.Documentation = Guid.Parse(await _attachmentService.CreateAttachment(createAttachmentDto));
            }

            if (request.Swagger != null)
            {
                var deleteAttachmentDto = new DeleteAttachmentDto { Id = existingEndpoint.Swagger };
                await _attachmentService.DeleteAttachment(deleteAttachmentDto);
                var createAttachmentDto = new CreateAttachmentDto { FileName = request.Swagger.FileName, FileUrl = request.Swagger.FileUrl };
                existingEndpoint.Swagger = Guid.Parse(await _attachmentService.CreateAttachment(createAttachmentDto));
            }

            if (request.Tour != null)
            {
                var deleteAttachmentDto = new DeleteAttachmentDto { Id = existingEndpoint.Tour };
                await _attachmentService.DeleteAttachment(deleteAttachmentDto);
                var createAttachmentDto = new CreateAttachmentDto { FileName = request.Tour.FileName, FileUrl = request.Tour.FileUrl };
                existingEndpoint.Tour = Guid.Parse(await _attachmentService.CreateAttachment(createAttachmentDto));
            }

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
            existingEndpoint.Changed = DateTime.UtcNow;

            // Step 9: Database Transactions
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Remove Old Tags
                    foreach (var tag in tagsToRemove)
                    {
                        await _dbConnection.ExecuteAsync(
                            "DELETE FROM APIEndpointTags WHERE ApiEndpointId = @ApiEndpointId AND ApiTagId = @ApiTagId",
                            new { ApiEndpointId = existingEndpoint.Id, ApiTagId = tag.Id },
                            transaction);
                    }

                    // Add New Tags
                    foreach (var tagName in tagsToAdd)
                    {
                        var apiTagRequestDto = new ApiTagRequestDto { Name = tagName };
                        var apiTag = await _apiTagService.GetApiTag(apiTagRequestDto);
                        if (apiTag != null)
                        {
                            await _dbConnection.ExecuteAsync(
                                "INSERT INTO APIEndpointTags (ApiEndpointId, ApiTagId) VALUES (@ApiEndpointId, @ApiTagId)",
                                new { ApiEndpointId = existingEndpoint.Id, ApiTagId = apiTag.Id },
                                transaction);
                        }
                    }

                    // Update APIEndpoint
                    await _dbConnection.ExecuteAsync(
                        "UPDATE ApiEndpoints SET ApiName = @ApiName, ApiScope = @ApiScope, ApiScopeProduction = @ApiScopeProduction, Deprecated = @Deprecated, Description = @Description, Documentation = @Documentation, EndpointUrls = @EndpointUrls, AppEnvironment = @AppEnvironment, Swagger = @Swagger, Tour = @Tour, ApiVersion = @ApiVersion, Langcode = @Langcode, Sticky = @Sticky, Promote = @Promote, UrlAlias = @UrlAlias, Published = @Published, Changed = @Changed WHERE Id = @Id",
                        existingEndpoint,
                        transaction);

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
