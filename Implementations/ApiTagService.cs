
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

            var existingTag = await _dbConnection.QueryFirstOrDefaultAsync<ApiTag>(
                "SELECT * FROM ApiTags WHERE Name = @Name", new { request.Name });

            if (existingTag != null)
            {
                return existingTag.Id.ToString();
            }

            var newApiTag = new ApiTag
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Version = 1,
                Created = DateTime.Now,
                CreatorId = request.CreatorId
            };

            var sql = @"INSERT INTO ApiTags (Id, Name, Version, Created, CreatorId) 
                        VALUES (@Id, @Name, @Version, @Created, @CreatorId)";

            var rowsAffected = await _dbConnection.ExecuteAsync(sql, newApiTag);

            if (rowsAffected == 0)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }

            return newApiTag.Id.ToString();
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
                apiTag = await _dbConnection.QueryFirstOrDefaultAsync<ApiTag>(
                    "SELECT * FROM ApiTags WHERE Id = @Id", new { request.Id });
            }
            else
            {
                apiTag = await _dbConnection.QueryFirstOrDefaultAsync<ApiTag>(
                    "SELECT * FROM ApiTags WHERE Name = @Name", new { request.Name });
            }

            return apiTag;
        }

        public async Task<string> UpdateApiTag(UpdateApiTagDto request)
        {
            if (request.Id == Guid.Empty || string.IsNullOrEmpty(request.Name) || request.ChangedUser == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var apiTag = await _dbConnection.QueryFirstOrDefaultAsync<ApiTag>(
                "SELECT * FROM ApiTags WHERE Id = @Id", new { request.Id });

            if (apiTag == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            apiTag.Name = request.Name;
            apiTag.Version += 1;
            apiTag.Changed = DateTime.Now;
            apiTag.ChangedUser = request.ChangedUser;

            var sql = @"UPDATE ApiTags 
                        SET Name = @Name, Version = @Version, Changed = @Changed, ChangedUser = @ChangedUser 
                        WHERE Id = @Id";

            var rowsAffected = await _dbConnection.ExecuteAsync(sql, apiTag);

            if (rowsAffected == 0)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }

            return apiTag.Id.ToString();
        }

        public async Task<bool> DeleteApiTag(DeleteApiTagDto request)
        {
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var apiTag = await _dbConnection.QueryFirstOrDefaultAsync<ApiTag>(
                "SELECT * FROM ApiTags WHERE Id = @Id", new { request.Id });

            if (apiTag == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            var rowsAffected = await _dbConnection.ExecuteAsync(
                "DELETE FROM ApiTags WHERE Id = @Id", new { request.Id });

            if (rowsAffected == 0)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }

            return true;
        }

        public async Task<List<ApiTag>> GetListApiTag(ListApiTagRequestDto request)
        {
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new TechnicalException("DP-400", "Technical Error");
            }

            var sortField = string.IsNullOrEmpty(request.SortField) ? "Id" : request.SortField;
            var sortOrder = string.IsNullOrEmpty(request.SortOrder) ? "asc" : request.SortOrder;

            var sql = $@"SELECT * FROM ApiTags 
                         ORDER BY {sortField} {sortOrder} 
                         OFFSET @PageOffset ROWS 
                         FETCH NEXT @PageLimit ROWS ONLY";

            var apiTags = await _dbConnection.QueryAsync<ApiTag>(sql, new { request.PageOffset, request.PageLimit });

            return apiTags.ToList();
        }
    }
}
