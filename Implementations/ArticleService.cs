
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
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch Articles
            var sql = @"SELECT * FROM Articles 
                        ORDER BY @SortField @SortOrder 
                        OFFSET @PageOffset ROWS 
                        FETCH NEXT @PageLimit ROWS ONLY";

            var parameters = new DynamicParameters();
            parameters.Add("SortField", request.SortField ?? "Id", DbType.String);
            parameters.Add("SortOrder", request.SortOrder ?? "asc", DbType.String);
            parameters.Add("PageOffset", request.PageOffset, DbType.Int32);
            parameters.Add("PageLimit", request.PageLimit, DbType.Int32);

            var articles = await _dbConnection.QueryAsync<Article>(sql, parameters);

            // Step 3: Fetch and Map Associated BlogCategories
            foreach (var article in articles)
            {
                var categoryIds = await _dbConnection.QueryAsync<Guid>(
                    "SELECT BlogCategoryId FROM ArticleBlogCategories WHERE ArticleId = @ArticleId",
                    new { ArticleId = article.Id });

                var categories = new List<BlogCategory>();
                foreach (var categoryId in categoryIds)
                {
                    var categoryRequest = new BlogCategoryRequestDto { Id = categoryId };
                    var category = await _blogCategoryService.GetBlogCategory(categoryRequest);
                    if (category == null)
                    {
                        throw new TechnicalException("DP-404", "Technical Error");
                    }
                    categories.Add(category);
                }
                article.BlogCategories = categories;
            }

            // Step 4: Fetch and Map Related Tags
            foreach (var article in articles)
            {
                var tagNames = await _dbConnection.QueryAsync<string>(
                    "SELECT BlogTagName FROM ArticleBlogTags WHERE ArticleId = @ArticleId",
                    new { ArticleId = article.Id });

                var tags = new List<BlogTag>();
                foreach (var tagName in tagNames)
                {
                    var tagRequest = new BlogTagRequestDto { Name = tagName };
                    var tag = await _blogTagService.GetBlogTag(tagRequest);
                    tags.Add(tag);
                }
                article.BlogTags = tags;
            }

            return articles.ToList();
        }
    }
}
