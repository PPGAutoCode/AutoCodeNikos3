
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
    public class ApiTagService : IApiTagService
    {
        private readonly IDbConnection _dbConnection;

        public ApiTagService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<string> CreateApiTag(CreateApiTagDto request)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var apiTag = new ApiTag
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Version = request.Version,
                Created = request.Created,
                CreatorId = request.CreatorId
            };

            const string sql = @"
                INSERT INTO ApiTags (Id, Name, Version, Created, CreatorId)
                VALUES (@Id, @Name, @Version, @Created, @CreatorId)";

            try
            {
                await _dbConnection.ExecuteAsync(sql, apiTag);
                return apiTag.Id.ToString();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<ApiTag> GetApiTag(ApiTagRequestDto request)
        {
            if (request.Id == Guid.Empty && string.IsNullOrEmpty(request.Name))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            ApiTag apiTag;

            if (request.Id != Guid.Empty)
            {
                const string sql = "SELECT * FROM ApiTags WHERE Id = @Id";
                apiTag = await _dbConnection.QuerySingleOrDefaultAsync<ApiTag>(sql, new { request.Id });
            }
            else
            {
                const string sql = "SELECT * FROM ApiTags WHERE Name = @Name";
                apiTag = await _dbConnection.QuerySingleOrDefaultAsync<ApiTag>(sql, new { request.Name });
            }

            if (apiTag == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            return apiTag;
        }

        public async Task<string> UpdateApiTag(UpdateApiTagDto request)
        {
            if (request.Id == Guid.Empty || string.IsNullOrEmpty(request.Name))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            const string selectSql = "SELECT * FROM ApiTags WHERE Id = @Id";
            var existingApiTag = await _dbConnection.QuerySingleOrDefaultAsync<ApiTag>(selectSql, new { request.Id });

            if (existingApiTag == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            existingApiTag.Name = request.Name;
            existingApiTag.Version = request.Version;
            existingApiTag.Changed = request.Changed;
            existingApiTag.ChangedUser = request.ChangedUser;

            const string updateSql = @"
                UPDATE ApiTags
                SET Name = @Name, Version = @Version, Changed = @Changed, ChangedUser = @ChangedUser
                WHERE Id = @Id";

            try
            {
                await _dbConnection.ExecuteAsync(updateSql, existingApiTag);
                return existingApiTag.Id.ToString();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<bool> DeleteApiTag(DeleteApiTagDto request)
        {
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            const string selectSql = "SELECT * FROM ApiTags WHERE Id = @Id";
            var existingApiTag = await _dbConnection.QuerySingleOrDefaultAsync<ApiTag>(selectSql, new { request.Id });

            if (existingApiTag == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            const string deleteSql = "DELETE FROM ApiTags WHERE Id = @Id";

            try
            {
                await _dbConnection.ExecuteAsync(deleteSql, new { request.Id });
                return true;
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<List<ApiTag>> GetListApiTag(ListApiTagRequestDto request)
        {
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var sortField = string.IsNullOrEmpty(request.SortField) ? "Id" : request.SortField;
            var sortOrder = string.IsNullOrEmpty(request.SortOrder) ? "asc" : request.SortOrder;

            var sql = $@"
                SELECT * FROM ApiTags
                ORDER BY {sortField} {sortOrder}
                OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";

            try
            {
                var apiTags = await _dbConnection.QueryAsync<ApiTag>(sql, new { request.PageOffset, request.PageLimit });
                return apiTags.ToList();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }
    }
}
