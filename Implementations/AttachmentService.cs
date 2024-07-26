
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ProjectName.Interfaces;
using ProjectName.Types;
using ProjectName.ControllersExceptions;

namespace ProjectName.Implementation
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
            if (string.IsNullOrEmpty(request.FileName) || string.IsNullOrEmpty(request.File))
            {
                throw new BusinessException("DP-422", "FileName and File cannot be null.");
            }

            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                FileName = request.FileName,
                File = request.File,
                Timestamp = DateTime.UtcNow
            };

            var query = "INSERT INTO Attachments (Id, FileName, File, Timestamp) VALUES (@Id, @FileName, @File, @Timestamp)";

            try
            {
                await _dbConnection.ExecuteAsync(query, attachment);
                return attachment.Id.ToString();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "An error occurred while saving the attachment.");
            }
        }

        public async Task<Attachment> GetAttachment(AttachmentRequestDto request)
        {
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Id cannot be null.");
            }

            var query = "SELECT * FROM Attachments WHERE Id = @Id";
            var attachment = await _dbConnection.QuerySingleOrDefaultAsync<Attachment>(query, new { Id = request.Id });

            if (attachment == null)
            {
                throw new TechnicalException("DP-404", "Attachment not found.");
            }

            return attachment;
        }

        public async Task<string> UpdateAttachment(UpdateAttachmentDto request)
        {
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Id cannot be null.");
            }

            var existingAttachment = await GetAttachment(new AttachmentRequestDto { Id = request.Id });

            if (existingAttachment == null)
            {
                throw new TechnicalException("DP-404", "Attachment not found.");
            }

            if (!string.IsNullOrEmpty(request.FileName))
            {
                existingAttachment.FileName = request.FileName;
            }

            if (!string.IsNullOrEmpty(request.File))
            {
                existingAttachment.File = request.File;
            }

            existingAttachment.Timestamp = DateTime.UtcNow;

            var query = "UPDATE Attachments SET FileName = @FileName, File = @File, Timestamp = @Timestamp WHERE Id = @Id";

            try
            {
                await _dbConnection.ExecuteAsync(query, existingAttachment);
                return existingAttachment.Id.ToString();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "An error occurred while updating the attachment.");
            }
        }

        public async Task<bool> DeleteAttachment(DeleteAttachmentDto request)
        {
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Id cannot be null.");
            }

            var existingAttachment = await GetAttachment(new AttachmentRequestDto { Id = request.Id });

            if (existingAttachment == null)
            {
                throw new TechnicalException("DP-404", "Attachment not found.");
            }

            var query = "DELETE FROM Attachments WHERE Id = @Id";

            try
            {
                await _dbConnection.ExecuteAsync(query, new { Id = request.Id });
                return true;
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "An error occurred while deleting the attachment.");
            }
        }

        public async Task<List<Attachment>> GetListAttachment(ListAttachmentRequestDto request)
        {
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "PageLimit must be greater than 0 and PageOffset cannot be negative.");
            }

            if (string.IsNullOrEmpty(request.SortField))
            {
                request.SortField = "Id";
            }

            if (string.IsNullOrEmpty(request.SortOrder))
            {
                request.SortOrder = "asc";
            }

            var query = $"SELECT * FROM Attachments ORDER BY {request.SortField} {request.SortOrder} OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";

            try
            {
                var attachments = await _dbConnection.QueryAsync<Attachment>(query, new { PageOffset = request.PageOffset, PageLimit = request.PageLimit });
                return attachments.AsList();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "An error occurred while fetching the attachments.");
            }
        }

        public async Task<string> UpsertAttachment(UpdateAttachmentDto request)
        {
            if (request.Id == Guid.Empty)
            {
                var createAttachmentDto = new CreateAttachmentDto
                {
                    FileName = request.FileName,
                    File = request.File
                };
                return await CreateAttachment(createAttachmentDto);
            }

            if (!string.IsNullOrEmpty(request.FileName) || !string.IsNullOrEmpty(request.File))
            {
                return await UpdateAttachment(request);
            }

            await DeleteAttachment(new DeleteAttachmentDto { Id = request.Id });
            return request.Id.ToString();
        }
    }
}
