
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ProjectName.Types;
using ProjectName.Interfaces;
using ProjectName.ControllersExceptions;

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
            if (string.IsNullOrEmpty(request.FileName) || request.FileUrl == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                FileName = request.FileName,
                FileUrl = request.FileUrl,
                Timestamp = DateTime.UtcNow
            };

            const string sql = "INSERT INTO Attachments (Id, FileName, FileUrl, Timestamp) VALUES (@Id, @FileName, @FileUrl, @Timestamp)";
            var affectedRows = await _dbConnection.ExecuteAsync(sql, attachment);

            if (affectedRows == 0)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }

            return attachment.Id.ToString();
        }

        public async Task<Attachment> GetAttachment(AttachmentRequestDto request)
        {
            if (request.Id == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            const string sql = "SELECT * FROM Attachments WHERE Id = @Id";
            var attachment = await _dbConnection.QuerySingleOrDefaultAsync<Attachment>(sql, new { Id = request.Id });

            if (attachment == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            return attachment;
        }

        public async Task<string> UpdateAttachment(UpdateAttachmentDto request)
        {
            if (request.Id == null || string.IsNullOrEmpty(request.FileName) || request.FileUrl == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            const string selectSql = "SELECT * FROM Attachments WHERE Id = @Id";
            var existingAttachment = await _dbConnection.QuerySingleOrDefaultAsync<Attachment>(selectSql, new { Id = request.Id });

            if (existingAttachment == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            existingAttachment.FileName = request.FileName;
            existingAttachment.FileUrl = request.FileUrl;
            existingAttachment.Timestamp = DateTime.UtcNow;

            const string updateSql = "UPDATE Attachments SET FileName = @FileName, FileUrl = @FileUrl, Timestamp = @Timestamp WHERE Id = @Id";
            var affectedRows = await _dbConnection.ExecuteAsync(updateSql, existingAttachment);

            if (affectedRows == 0)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }

            return existingAttachment.Id.ToString();
        }

        public async Task<bool> DeleteAttachment(DeleteAttachmentDto request)
        {
            if (request.Id == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            const string selectSql = "SELECT * FROM Attachments WHERE Id = @Id";
            var existingAttachment = await _dbConnection.QuerySingleOrDefaultAsync<Attachment>(selectSql, new { Id = request.Id });

            if (existingAttachment == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            const string deleteSql = "DELETE FROM Attachments WHERE Id = @Id";
            var affectedRows = await _dbConnection.ExecuteAsync(deleteSql, new { Id = request.Id });

            if (affectedRows == 0)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }

            return true;
        }

        public async Task<List<Attachment>> GetListAttachment(ListAttachmentRequestDto request)
        {
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var sql = "SELECT * FROM Attachments";
            if (!string.IsNullOrEmpty(request.SortField) && !string.IsNullOrEmpty(request.SortOrder))
            {
                sql += $" ORDER BY {request.SortField} {request.SortOrder}";
            }
            sql += " OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";

            var attachments = await _dbConnection.QueryAsync<Attachment>(sql, new { PageOffset = request.PageOffset, PageLimit = request.PageLimit });

            if (attachments == null)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }

            return attachments.AsList();
        }
    }
}
