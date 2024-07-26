
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

            var sql = @"
                INSERT INTO BlogTags (Id, Name, Version, Created, CreatorId)
                VALUES (@Id, @Name, @Version, @Created, @CreatorId)";

            try
            {
                await _dbConnection.ExecuteAsync(sql, newBlogTag);
                return newBlogTag.Id.ToString();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<BlogTag> GetBlogTag(BlogTagRequestDto request)
        {
            if (request.Id == null && string.IsNullOrEmpty(request.Name))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            BlogTag blogTag = null;

            if (request.Id != null)
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
            if (request.Id == null || string.IsNullOrEmpty(request.Name) || request.ChangedUser == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var blogTag = await _dbConnection.QueryFirstOrDefaultAsync<BlogTag>(
                "SELECT * FROM BlogTags WHERE Id = @Id",
                new { request.Id });

            if (blogTag == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            blogTag.Name = request.Name;
            blogTag.Version += 1;
            blogTag.Changed = DateTime.Now;
            blogTag.ChangedUser = request.ChangedUser;

            var sql = @"
                UPDATE BlogTags
                SET Name = @Name, Version = @Version, Changed = @Changed, ChangedUser = @ChangedUser
                WHERE Id = @Id";

            try
            {
                await _dbConnection.ExecuteAsync(sql, blogTag);
                return blogTag.Id.ToString();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<bool> DeleteBlogTag(DeleteBlogTagDto request)
        {
            if (request.Id == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var blogTag = await _dbConnection.QueryFirstOrDefaultAsync<BlogTag>(
                "SELECT * FROM BlogTags WHERE Id = @Id",
                new { request.Id });

            if (blogTag == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            var sql = @"
                DELETE FROM BlogTags WHERE Id = @Id";

            try
            {
                await _dbConnection.ExecuteAsync(sql, new { request.Id });
                return true;
            }
            catch (Exception)
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

            if (string.IsNullOrEmpty(request.SortField) || string.IsNullOrEmpty(request.SortOrder))
            {
                request.SortField = "Id";
                request.SortOrder = "asc";
            }

            var sql = $@"
                SELECT * FROM BlogTags
                ORDER BY {request.SortField} {request.SortOrder}
                OFFSET {request.PageOffset} ROWS
                FETCH NEXT {request.PageLimit} ROWS ONLY";

            try
            {
                var blogTags = await _dbConnection.QueryAsync<BlogTag>(sql);
                return blogTags.ToList();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }
    }
}
