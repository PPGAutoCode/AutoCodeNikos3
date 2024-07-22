
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
    public class BlogTagService : IBlogTagService
    {
        private readonly IDbConnection _dbConnection;

        public BlogTagService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<string> CreateBlogTag(CreateBlogTagDto request)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var existingTag = await _dbConnection.QueryFirstOrDefaultAsync<BlogTag>(
                "SELECT * FROM BlogTags WHERE Name = @Name",
                new { request.Name });

            if (existingTag != null)
            {
                return existingTag.Id.ToString();
            }

            var newBlogTag = new BlogTag
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Version = 1,
                Created = DateTime.Now,
                CreatorId = request.CreatorId
            };

            var sql = @"INSERT INTO BlogTags (Id, Name, Version, Created, CreatorId) 
                        VALUES (@Id, @Name, @Version, @Created, @CreatorId)";

            try
            {
                await _dbConnection.ExecuteAsync(sql, newBlogTag);
                return newBlogTag.Id.ToString();
            }
            catch
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<BlogTag> GetBlogTag(BlogTagRequestDto request)
        {
            if ((request.Id == Guid.Empty || request.Id == null) && string.IsNullOrEmpty(request.Name))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            BlogTag blogTag = null;

            if (request.Id != Guid.Empty && request.Id != null)
            {
                blogTag = await _dbConnection.QueryFirstOrDefaultAsync<BlogTag>(
                    "SELECT * FROM BlogTags WHERE Id = @Id",
                    new { request.Id });
            }
            else if (!string.IsNullOrEmpty(request.Name))
            {
                blogTag = await _dbConnection.QueryFirstOrDefaultAsync<BlogTag>(
                    "SELECT * FROM BlogTags WHERE Name = @Name",
                    new { request.Name });
            }

            return blogTag;
        }

        public async Task<string> UpdateBlogTag(UpdateBlogTagDto request)
        {
            if (request.Id == Guid.Empty || request.Id == null || string.IsNullOrEmpty(request.Name) || request.ChangedUser == Guid.Empty || request.ChangedUser == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var existingTag = await _dbConnection.QueryFirstOrDefaultAsync<BlogTag>(
                "SELECT * FROM BlogTags WHERE Id = @Id",
                new { request.Id });

            if (existingTag == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            existingTag.Name = request.Name;
            existingTag.Version += 1;
            existingTag.Changed = DateTime.Now;
            existingTag.ChangedUser = request.ChangedUser;

            var sql = @"UPDATE BlogTags 
                        SET Name = @Name, Version = @Version, Changed = @Changed, ChangedUser = @ChangedUser 
                        WHERE Id = @Id";

            try
            {
                await _dbConnection.ExecuteAsync(sql, existingTag);
                return existingTag.Id.ToString();
            }
            catch
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<bool> DeleteBlogTag(DeleteBlogTagDto request)
        {
            if (request.Id == Guid.Empty || request.Id == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var existingTag = await _dbConnection.QueryFirstOrDefaultAsync<BlogTag>(
                "SELECT * FROM BlogTags WHERE Id = @Id",
                new { request.Id });

            if (existingTag == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            var sql = @"DELETE FROM BlogTags WHERE Id = @Id";

            try
            {
                await _dbConnection.ExecuteAsync(sql, new { request.Id });
                return true;
            }
            catch
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<List<BlogTag>> GetListBlogTag(ListBlogTagRequestDto request)
        {
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var sql = @"SELECT * FROM BlogTags 
                        ORDER BY 
                        @SortField 
                        @SortOrder 
                        OFFSET @PageOffset ROWS 
                        FETCH NEXT @PageLimit ROWS ONLY";

            var parameters = new
            {
                SortField = request.SortField ?? "Id",
                SortOrder = request.SortOrder ?? "ASC",
                request.PageOffset,
                request.PageLimit
            };

            try
            {
                var blogTags = await _dbConnection.QueryAsync<BlogTag>(sql, parameters);
                return blogTags.ToList();
            }
            catch
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }
    }
}
