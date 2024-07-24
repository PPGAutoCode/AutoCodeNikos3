
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProjectName.Types;
using ProjectName.Interfaces;
using ProjectName.ControllersExceptions;

public class AttachmentService : IAttachmentService
{
    private readonly IDbConnection _dbConnection;

    public AttachmentService(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<string> CreateAttachment(CreateAttachmentDto request)
    {
        if (string.IsNullOrEmpty(request.FileName) || request.FileUrl == null || string.IsNullOrEmpty(request.FilePath))
        {
            throw new BusinessException("DP-422", "Client Error");
        }

        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            FileName = request.FileName,
            FileUrl = request.FileUrl,
            FilePath = request.FilePath,
            Timestamp = DateTime.UtcNow
        };

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
        if (request.Id == null)
        {
            throw new BusinessException("DP-422", "Client Error");
        }

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
        if (request.Id == null || string.IsNullOrEmpty(request.FileName) || request.FileUrl == null || string.IsNullOrEmpty(request.FilePath))
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
        existingAttachment.FilePath = request.FilePath;
        existingAttachment.Timestamp = DateTime.UtcNow;

        const string updateSql = "UPDATE Attachments SET FileName = @FileName, FileUrl = @FileUrl, FilePath = @FilePath, Timestamp = @Timestamp WHERE Id = @Id";
        var affectedRows = await _dbConnection.ExecuteAsync(updateSql, existingAttachment);

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
        if (request.PageLimit <= 0 || request.PageOffset < 0)
        {
            throw new BusinessException("DP-422", "Client Error");
        }

        var sortField = string.IsNullOrEmpty(request.SortField) ? "Id" : request.SortField;
        var sortOrder = string.IsNullOrEmpty(request.SortOrder) ? "asc" : request.SortOrder;

        var sql = $"SELECT * FROM Attachments ORDER BY {sortField} {sortOrder} OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY";
        var attachments = await _dbConnection.QueryAsync<Attachment>(sql, new { Offset = request.PageOffset, Limit = request.PageLimit });

        return attachments.ToList();
    }

    public async Task HandleAttachment(CreateAttachmentDto newAttachment, Guid? existingAttachmentId, Action<Guid?> updateAttachmentFieldId)
    {
        if (newAttachment != null)
        {
            if (existingAttachmentId != null)
            {
                var existingAttachment = await GetAttachment(new AttachmentRequestDto { Id = existingAttachmentId });
                if (existingAttachment != null && existingAttachment.FilePath != newAttachment.FilePath)
                {
                    await DeleteAttachment(new DeleteAttachmentDto { Id = existingAttachmentId });
                }
            }

            var newAttachmentId = Guid.Parse(await CreateAttachment(newAttachment));
            updateAttachmentFieldId(newAttachmentId);
        }
        else if (existingAttachmentId != null)
        {
            await DeleteAttachment(new DeleteAttachmentDto { Id = existingAttachmentId });
            updateAttachmentFieldId(null);
        }
    }

    public async Task<Attachment> UploadAttachment(CreateAttachmentDto? newAttachment)
    {
        if (newAttachment != null)
        {
            const string checkSql = "SELECT * FROM Attachments WHERE FilePath = @FilePath";
            var existingAttachment = await _dbConnection.QuerySingleOrDefaultAsync<Attachment>(checkSql, new { FilePath = newAttachment.FilePath });

            if (existingAttachment != null)
            {
                return existingAttachment;
            }
            else
            {
                var newAttachmentId = await CreateAttachment(newAttachment);
                return await GetAttachment(new AttachmentRequestDto { Id = Guid.Parse(newAttachmentId) });
            }
        }
        else
        {
            return null;
        }
    }
}
