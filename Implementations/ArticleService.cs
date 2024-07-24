
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProjectName.Types;
using ProjectName.Interfaces;
using ProjectName.ControllersExceptions;

namespace ProjectName.Services
{
    public class ArticleService : IArticleService
    {
        private readonly IDbConnection _dbConnection;
        private readonly IBlogCategoryService _blogCategoryService;
        private readonly IBlogTagService _blogTagService;

        public ArticleService(IDbConnection dbConnection, IBlogCategoryService blogCategoryService, IBlogTagService blogTagService)
        {
            _dbConnection = dbConnection;
            _blogCategoryService = blogCategoryService;
            _blogTagService = blogTagService;
        }

        public async Task<List<Article>> GetListArticle(ListArticleRequestDto request)
        {
            // Step 1: Validate the request
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Invalid pagination parameters.");
            }

            // Step 2: Fetch Articles
            string sortField = request.SortField ?? "Id";
            string sortOrder = request.SortOrder ?? "asc";
            string sql = $"SELECT * FROM Articles ORDER BY {sortField} {sortOrder} OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";
            var articles = (await _dbConnection.QueryAsync<Article>(sql, new { request.PageOffset, request.PageLimit })).ToList();

            // Step 3: Fetch and Map Associated BlogCategories
            foreach (var article in articles)
            {
                sql = "SELECT BlogCategoryId FROM ArticleBlogCategories WHERE ArticleId = @ArticleId";
                var blogCategoryIds = (await _dbConnection.QueryAsync<Guid>(sql, new { ArticleId = article.Id })).ToList();
                article.BlogCategories = new List<BlogCategory>();

                foreach (var categoryId in blogCategoryIds)
                {
                    var blogCategoryRequestDto = new BlogCategoryRequestDto { Id = categoryId };
                    var blogCategory = await _blogCategoryService.GetBlogCategory(blogCategoryRequestDto);
                    if (blogCategory == null)
                    {
                        throw new TechnicalException("DP-404", "BlogCategory not found.");
                    }
                    article.BlogCategories.Add(blogCategory);
                }
            }

            // Step 4: Fetch and Map Related Tags
            foreach (var article in articles)
            {
                sql = "SELECT BlogTagId FROM ArticleBlogTags WHERE ArticleId = @ArticleId";
                var blogTagIds = (await _dbConnection.QueryAsync<Guid>(sql, new { ArticleId = article.Id })).ToList();

                if (blogTagIds.Any())
                {
                    article.BlogTags = new List<BlogTag>();
                    foreach (var tagId in blogTagIds)
                    {
                        var blogTagRequestDto = new BlogTagRequestDto { Id = tagId };
                        var blogTag = await _blogTagService.GetBlogTag(blogTagRequestDto);
                        if (blogTag == null)
                        {
                            throw new TechnicalException("DP-404", "BlogTag not found.");
                        }
                        article.BlogTags.Add(blogTag);
                    }
                }
                else
                {
                    article.BlogTags = null;
                }
            }

            // Step 5: Return the list of Articles
            return articles;
        }
    }
}
