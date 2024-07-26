
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ProjectName.Interfaces;
using ProjectName.Types;
using ProjectName.ControllersExceptions;

namespace ProjectName.Services
{
    public class AppEnvironmentService : IAppEnvironmentService
    {
        private readonly IDbConnection _dbConnection;

        public AppEnvironmentService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<string> CreateAppEnvironment(CreateAppEnvironmentDto request)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var appEnvironment = new AppEnvironment
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Version = 1,
                Created = DateTime.UtcNow,
                Changed = DateTime.UtcNow,
                CreatorId = request.CreatorId,
                ChangedUser = request.CreatorId
            };

            const string sql = @"
                INSERT INTO AppEnvironments (Id, Name, Version, Created, Changed, CreatorId, ChangedUser)
                VALUES (@Id, @Name, @Version, @Created, @Changed, @CreatorId, @ChangedUser);
            ";

            try
            {
                await _dbConnection.ExecuteAsync(sql, appEnvironment);
                return appEnvironment.Id.ToString();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<AppEnvironment> GetAppEnvironment(AppEnvironmentRequestDto request)
        {
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            const string sql = @"
                SELECT * FROM AppEnvironments WHERE Id = @Id;
            ";

            try
            {
                var appEnvironment = await _dbConnection.QuerySingleOrDefaultAsync<AppEnvironment>(sql, new { request.Id });
                if (appEnvironment == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
                return appEnvironment;
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<string> UpdateAppEnvironment(UpdateAppEnvironmentDto request)
        {
            if (request.Id == Guid.Empty || string.IsNullOrEmpty(request.Name))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            const string selectSql = @"
                SELECT * FROM AppEnvironments WHERE Id = @Id;
            ";

            var existingAppEnvironment = await _dbConnection.QuerySingleOrDefaultAsync<AppEnvironment>(selectSql, new { request.Id });
            if (existingAppEnvironment == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            existingAppEnvironment.Name = request.Name;
            existingAppEnvironment.Version += 1;
            existingAppEnvironment.Changed = DateTime.UtcNow;
            existingAppEnvironment.ChangedUser = request.ChangedUser;

            const string updateSql = @"
                UPDATE AppEnvironments
                SET Name = @Name, Version = @Version, Changed = @Changed, ChangedUser = @ChangedUser
                WHERE Id = @Id;
            ";

            try
            {
                await _dbConnection.ExecuteAsync(updateSql, existingAppEnvironment);
                return existingAppEnvironment.Id.ToString();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<bool> DeleteAppEnvironment(DeleteAppEnvironmentDto request)
        {
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            const string selectSql = @"
                SELECT * FROM AppEnvironments WHERE Id = @Id;
            ";

            var existingAppEnvironment = await _dbConnection.QuerySingleOrDefaultAsync<AppEnvironment>(selectSql, new { request.Id });
            if (existingAppEnvironment == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            const string deleteSql = @"
                DELETE FROM AppEnvironments WHERE Id = @Id;
            ";

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

        public async Task<List<AppEnvironment>> GetListAppEnvironment(ListAppEnvironmentRequestDto request)
        {
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var sql = @"
                SELECT * FROM AppEnvironments
                ORDER BY {0} {1}
                OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY;
            ";

            sql = string.Format(sql, request.SortField ?? "Id", request.SortOrder ?? "ASC");

            try
            {
                var appEnvironments = await _dbConnection.QueryAsync<AppEnvironment>(sql, new { request.PageOffset, request.PageLimit });
                return appEnvironments.AsList();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }
    }
}
