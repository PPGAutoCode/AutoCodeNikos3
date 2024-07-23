
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
    public class AttachmentService : IAttachmentService
    {
        private readonly IDbConnection _dbConnection;

        public AttachmentService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<string> CreateAttachment(CreateAttachmentDto request)
        {
            // Step 1: Validate the request payload
            if (string.IsNullOrEmpty(request.FileName) || request.FileUrl == null || string.IsNullOrEmpty(request.FilePath))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Create a new Attachment object
            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                FileName = request.FileName,
                FileUrl = request.FileUrl,
                FilePath = request.FilePath,
                Timestamp = DateTime.UtcNow
            };

            // Step 3: Save the newly created Attachment object to the database
            const string sql = "INSERT INTO Attachments (Id, FileName, FileUrl, FilePath, Timestamp) VALUES (@Id, @FileName, @FileUrl, @FilePath, @Timestamp)";
            var affectedRows = await _dbConnection.ExecuteAsync(sql, attachment);

            if (affectedRows > 0)
            {
                return attachment.Id.ToString();
            }
            else
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<Attachment> GetAttachment(AttachmentRequestDto request)
        {
            // Step 1: Validate that request.payload.Id is not null
            if (request.Id == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch the Attachment from the database based on the provided attachment ID
            const string sql = "SELECT * FROM Attachments WHERE Id = @Id";
            var attachment = await _dbConnection.QuerySingleOrDefaultAsync<Attachment>(sql, new { Id = request.Id });

            if (attachment != null)
            {
                return attachment;
            }
            else
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }
        }

        public async Task<string> UpdateAttachment(UpdateAttachmentDto request)
        {
            // Step 1: Validate that the request payload contains the necessary parameters
            if (request.Id == null || string.IsNullOrEmpty(request.FileName) || request.FileUrl == null || string.IsNullOrEmpty(request.FilePath))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch the Attachment from the database by Id
            var existingAttachment = await GetAttachment(new AttachmentRequestDto { Id = request.Id });
            if (existingAttachment == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Update the Attachment object with the provided changes
            existingAttachment.FileName = request.FileName;
            existingAttachment.FileUrl = request.FileUrl;
            existingAttachment.FilePath = request.FilePath;
            existingAttachment.Timestamp = DateTime.UtcNow;

            // Step 4: Insert the updated Attachment object to the database
            const string sql = "UPDATE Attachments SET FileName = @FileName, FileUrl = @FileUrl, FilePath = @FilePath, Timestamp = @Timestamp WHERE Id = @Id";
            var affectedRows = await _dbConnection.ExecuteAsync(sql, existingAttachment);

            if (affectedRows > 0)
            {
                return existingAttachment.Id.ToString();
            }
            else
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<bool> DeleteAttachment(DeleteAttachmentDto request)
        {
            // Step 1: Validate that the request payload contains the necessary parameter
            if (request.Id == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch the Attachment from the database by Id
            var existingAttachment = await GetAttachment(new AttachmentRequestDto { Id = request.Id });
            if (existingAttachment == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Delete the Attachment object from the database
            const string sql = "DELETE FROM Attachments WHERE Id = @Id";
            var affectedRows = await _dbConnection.ExecuteAsync(sql, new { Id = request.Id });

            if (affectedRows > 0)
            {
                return true;
            }
            else
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<List<Attachment>> GetListAttachment(ListAttachmentRequestDto request)
        {
            // Step 1: Validate that the ListAttachmentRequestDto contains the necessary pagination parameters
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Set default values for sorting if not provided
            var sortField = string.IsNullOrEmpty(request.SortField) ? "Id" : request.SortField;
            var sortOrder = string.IsNullOrEmpty(request.SortOrder) ? "asc" : request.SortOrder;

            // Step 3: Fetch the list of Attachments from the database table Attachments based on the provided pagination parameters and optional sorting
            var sql = $"SELECT * FROM Attachments ORDER BY {sortField} {sortOrder} OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";
            var attachments = await _dbConnection.QueryAsync<Attachment>(sql, new { PageOffset = request.PageOffset, PageLimit = request.PageLimit });

            return attachments.ToList();
        }

        public async Task HandleAttachment(CreateAttachmentDto newAttachment, Guid? existingAttachmentId, Action<Guid?> updateAttachmentFieldId)
        {
            if (newAttachment != null)
            {
                if (existingAttachmentId != null)
                {
                    var existingAttachment = await GetAttachment(new AttachmentRequestDto { Id = existingAttachmentId });
                    if (existingAttachment != null && !existingAttachment.FilePath.SequenceEqual(newAttachment.FilePath))
                    {
                        await DeleteAttachment(new DeleteAttachmentDto { Id = existingAttachmentId });
                        var newAttachmentId = await CreateAttachment(newAttachment);
                        updateAttachmentFieldId(Guid.Parse(newAttachmentId));
                    }
                }
                else
                {
                    var newAttachmentId = await CreateAttachment(newAttachment);
                    updateAttachmentFieldId(Guid.Parse(newAttachmentId));
                }
            }
            else
            {
                if (existingAttachmentId != null)
                {
                    await DeleteAttachment(new DeleteAttachmentDto { Id = existingAttachmentId });
                    updateAttachmentFieldId(null);
                }
            }
        }
    }
}
