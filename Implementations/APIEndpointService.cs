
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
        private readonly IAttachmentService _attachmentService;

        public APIEndpointService(IDbConnection dbConnection, IAttachmentService attachmentService)
        {
            _dbConnection = dbConnection;
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
                new { request.Id });

            if (existingEndpoint == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Fetch and Validate Related Entities
            List<Guid> attachmentIds = new List<Guid>();
            if (existingEndpoint.Documentation != null) attachmentIds.Add(existingEndpoint.Documentation);
            if (existingEndpoint.Swagger != null) attachmentIds.Add(existingEndpoint.Swagger);
            if (existingEndpoint.Tour != null) attachmentIds.Add(existingEndpoint.Tour);

            // Step 4: Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Delete Attachments
                    foreach (var attachmentId in attachmentIds)
                    {
                        await _attachmentService.DeleteAttachment(new DeleteAttachmentDto { Id = attachmentId });
                    }

                    // Delete APIEndpointTags
                    await _dbConnection.ExecuteAsync(
                        "DELETE FROM APIEndpointTags WHERE APIEndpointId = @Id",
                        new { request.Id },
                        transaction);

                    // Delete APIEndpoint
                    await _dbConnection.ExecuteAsync(
                        "DELETE FROM APIEndpoints WHERE Id = @Id",
                        new { request.Id },
                        transaction);

                    // Commit the transaction
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
