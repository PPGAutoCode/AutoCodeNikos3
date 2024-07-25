
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

            var result = await _dbConnection.ExecuteAsync(query, attachment);

            if (result > 0)
            {
                return attachment.Id.ToString();
            }
            else
            {
                throw new TechnicalException("DP-500", "Failed to create attachment.");
            }
        }

        public async Task<File> GetAttachment(AttachmentRequestDto request)
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

            return attachment.File;
        }

        public async Task<string> UpdateAttachment(UpdateAttachmentDto request)
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

            if (!string.IsNullOrEmpty(request.FileName))
            {
                attachment.FileName = request.FileName;
            }

            if (!string.IsNullOrEmpty(request.File))
            {
                attachment.File = request.File;
            }

            attachment.Timestamp = DateTime.UtcNow;

            var updateQuery = "UPDATE Attachments SET FileName = @FileName, File = @File, Timestamp = @Timestamp WHERE Id = @Id";
            var result = await _dbConnection.ExecuteAsync(updateQuery, attachment);

            if (result > 0)
            {
                return attachment.Id.ToString();
            }
            else
            {
                throw new TechnicalException("DP-500", "Failed to update attachment.");
            }
        }

        public async Task<bool> DeleteAttachment(DeleteAttachmentDto request)
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

            var deleteQuery = "DELETE FROM Attachments WHERE Id = @Id";
            var result = await _dbConnection.ExecuteAsync(deleteQuery, new { Id = request.Id });

            if (result > 0)
            {
                return true;
            }
            else
            {
                throw new TechnicalException("DP-500", "Failed to delete attachment.");
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
            var attachments = await _dbConnection.QueryAsync<Attachment>(query, new { PageOffset = request.PageOffset, PageLimit = request.PageLimit });

            return attachments.AsList();
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
            else
            {
                if (!string.IsNullOrEmpty(request.FileName) || !string.IsNullOrEmpty(request.File))
                {
                    return await UpdateAttachment(request);
                }
                else
                {
                    var deleteRequest = new DeleteAttachmentDto { Id = request.Id };
                    await DeleteAttachment(deleteRequest);
                    return request.Id.ToString();
                }
            }
        }
    }
}
