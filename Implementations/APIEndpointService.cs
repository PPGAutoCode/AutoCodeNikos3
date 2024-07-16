
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
            var existingAPIEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>("SELECT * FROM ApiEndpoints WHERE Id = @Id", new { request.Id });
            if (existingAPIEndpoint == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Fetch and validate related entities
            // AppEnvironment
            var appEnvironmentRequest = new AppEnvironmentRequestDto { Id = request.AppEnvironment };
            var appEnvironment = await _appEnvironmentService.GetAppEnvironment(appEnvironmentRequest);
            if (appEnvironment == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // ApiTags
            List<Guid> tagIds = new List<Guid>();
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
                    tagIds.Add(apiTag.Id);
                }
            }

            // Handle Tags Removal and Addition
            var existingTags = await _dbConnection.QueryAsync<Guid>("SELECT TagId FROM APIEndpointTags WHERE APIEndpointId = @Id", new { request.Id });
            var tagsToRemove = existingTags.Except(tagIds).ToList();
            var tagsToAdd = tagIds.Except(existingTags).ToList();

            // Handle Attachments
            Guid? documentationId = null;
            Guid? swaggerId = null;
            Guid? tourId = null;

            async Task HandleAttachment(CreateAttachmentDto attachmentDto, Guid? existingAttachmentId, ref Guid? newAttachmentId)
            {
                if (attachmentDto != null)
                {
                    var attachmentRequest = new AttachmentRequestDto { Id = existingAttachmentId ?? Guid.Empty };
                    var existingAttachment = await _attachmentService.GetAttachment(attachmentRequest);
                    if (existingAttachment == null || existingAttachment.FileName != attachmentDto.FileName)
                    {
                        if (existingAttachmentId != null)
                        {
                            var deleteAttachmentDto = new DeleteAttachmentDto { Id = existingAttachmentId.Value };
                            await _attachmentService.DeleteAttachment(deleteAttachmentDto);
                        }
                        var createAttachmentDto = new CreateAttachmentDto { FileName = attachmentDto.FileName, FileUrl = attachmentDto.FileUrl };
                        newAttachmentId = Guid.Parse(await _attachmentService.CreateAttachment(createAttachmentDto));
                    }
                }
                else if (existingAttachmentId != null)
                {
                    var deleteAttachmentDto = new DeleteAttachmentDto { Id = existingAttachmentId.Value };
                    await _attachmentService.DeleteAttachment(deleteAttachmentDto);
                    newAttachmentId = null;
                }
            }

            await HandleAttachment(request.Documentation, existingAPIEndpoint.Documentation, ref documentationId);
            await HandleAttachment(request.Swagger, existingAPIEndpoint.Swagger, ref swaggerId);
            await HandleAttachment(request.Tour, existingAPIEndpoint.Tour, ref tourId);

            // Update the APIEndpoint object
            existingAPIEndpoint.ApiName = request.ApiName;
            existingAPIEndpoint.ApiScope = request.ApiScope;
            existingAPIEndpoint.ApiScopeProduction = request.ApiScopeProduction;
            existingAPIEndpoint.Deprecated = request.Deprecated;
            existingAPIEndpoint.Description = request.Description;
            existingAPIEndpoint.Documentation = documentationId;
            existingAPIEndpoint.EndpointUrls = request.EndpointUrls;
            existingAPIEndpoint.AppEnvironment = request.AppEnvironment;
            existingAPIEndpoint.Swagger = swaggerId;
            existingAPIEndpoint.Tour = tourId;
            existingAPIEndpoint.ApiVersion = request.ApiVersion;
            existingAPIEndpoint.Langcode = request.Langcode;
            existingAPIEndpoint.Sticky = request.Sticky;
            existingAPIEndpoint.Promote = request.Promote;
            existingAPIEndpoint.UrlAlias = request.UrlAlias;
            existingAPIEndpoint.Published = request.Published;

            // Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Remove Old Tags
                    foreach (var tagId in tagsToRemove)
                    {
                        await _dbConnection.ExecuteAsync("DELETE FROM APIEndpointTags WHERE APIEndpointId = @APIEndpointId AND TagId = @TagId", new { request.Id, tagId }, transaction);
                    }

                    // Add New Tags
                    foreach (var tagId in tagsToAdd)
                    {
                        await _dbConnection.ExecuteAsync("INSERT INTO APIEndpointTags (Id, APIEndpointId, TagId) VALUES (@Id, @APIEndpointId, @TagId)", new { Id = Guid.NewGuid(), request.Id, tagId }, transaction);
                    }

                    // Update APIEndpoint object in the database table
                    await _dbConnection.ExecuteAsync("UPDATE ApiEndpoints SET ApiName = @ApiName, ApiScope = @ApiScope, ApiScopeProduction = @ApiScopeProduction, Deprecated = @Deprecated, Description = @Description, Documentation = @Documentation, EndpointUrls = @EndpointUrls, AppEnvironment = @AppEnvironment, Swagger = @Swagger, Tour = @Tour, ApiVersion = @ApiVersion, Langcode = @Langcode, Sticky = @Sticky, Promote = @Promote, UrlAlias = @UrlAlias, Published = @Published WHERE Id = @Id", existingAPIEndpoint, transaction);

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
