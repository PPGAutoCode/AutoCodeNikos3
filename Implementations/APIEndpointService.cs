
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

        public async Task<bool> DeleteAPIEndpoint(DeleteAPIEndpointDto request)
        {
            // Step 1: Validate Request Payload
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch Existing API Endpoint
            var existingEndpoint = await _dbConnection.QuerySingleOrDefaultAsync<APIEndpoint>(
                "SELECT * FROM APIEndpoints WHERE Id = @Id",
                new { Id = request.Id });

            if (existingEndpoint == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Fetch and Validate Related Entities
            var apiTagIds = await _dbConnection.QueryAsync<Guid>(
                "SELECT ApiTagId FROM APIEndpointTags WHERE APIEndpointId = @Id",
                new { Id = request.Id });

            foreach (var tagId in apiTagIds)
            {
                var apiTagRequestDto = new ApiTagRequestDto { Id = tagId };
                var apiTag = await _apiTagService.GetApiTag(apiTagRequestDto);
                if (apiTag == null)
                {
                    throw new TechnicalException("DP-400", "Technical Error");
                }
            }

            var attachmentIds = new List<Guid>();
            if (existingEndpoint.Documentation != Guid.Empty) attachmentIds.Add(existingEndpoint.Documentation);
            if (existingEndpoint.Swagger != Guid.Empty) attachmentIds.Add(existingEndpoint.Swagger);
            if (existingEndpoint.Tour != Guid.Empty) attachmentIds.Add(existingEndpoint.Tour);

            foreach (var attachmentId in attachmentIds)
            {
                var attachmentRequestDto = new AttachmentRequestDto { Id = attachmentId };
                var attachment = await _attachmentService.GetAttachment(attachmentRequestDto);
                if (attachment == null)
                {
                    throw new TechnicalException("DP-400", "Technical Error");
                }
            }

            // Step 4: Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Delete APIEndpointTags
                    await _dbConnection.ExecuteAsync(
                        "DELETE FROM APIEndpointTags WHERE APIEndpointId = @Id",
                        new { Id = request.Id },
                        transaction);

                    // Delete Attachments
                    foreach (var attachmentId in attachmentIds)
                    {
                        var deleteAttachmentDto = new DeleteAttachmentDto { Id = attachmentId };
                        await _attachmentService.DeleteAttachment(deleteAttachmentDto);
                    }

                    // Delete APIEndpoint
                    await _dbConnection.ExecuteAsync(
                        "DELETE FROM APIEndpoints WHERE Id = @Id",
                        new { Id = request.Id },
                        transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
                }
            }

            return true;
        }
    }
}
