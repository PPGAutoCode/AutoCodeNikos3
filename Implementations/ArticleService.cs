
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

        public async Task<List<Article>> GetListArticle(ListArticleRequestDto requestDto)
        {
            // Validation Logic
            if (requestDto == null || requestDto.PageLimit <= 0 || requestDto.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Fetch Articles
            string sortField = requestDto.SortField ?? "Id";
            string sortOrder = requestDto.SortOrder ?? "asc";
            string sql = $"SELECT * FROM Articles ORDER BY {sortField} {sortOrder} OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";
            var articles = await _dbConnection.QueryAsync<Article>(sql, new { requestDto.PageOffset, requestDto.PageLimit });

            // Fetch and Map Associated BlogCategories
            foreach (var article in articles)
            {
                sql = "SELECT BlogCategoryId FROM ArticleBlogCategories WHERE ArticleId = @ArticleId";
                var blogCategoryIds = await _dbConnection.QueryAsync<Guid>(sql, new { ArticleId = article.Id });
                article.BlogCategories = new List<BlogCategory>();
                foreach (var blogCategoryId in blogCategoryIds)
                {
                    var blogCategoryRequestDto = new BlogCategoryRequestDto { Id = blogCategoryId };
                    var blogCategory = await _blogCategoryService.GetBlogCategory(blogCategoryRequestDto);
                    if (blogCategory == null)
                    {
                        throw new TechnicalException("DP-404", "Technical Error");
                    }
                    article.BlogCategories.Add(blogCategory);
                }
            }

            // Fetch and Map Related Tags
            foreach (var article in articles)
            {
                sql = "SELECT BlogTagId FROM ArticleBlogTags WHERE ArticleId = @ArticleId";
                var blogTagIds = await _dbConnection.QueryAsync<Guid>(sql, new { ArticleId = article.Id });
                article.BlogTags = new List<BlogTag>();
                foreach (var blogTagId in blogTagIds)
                {
                    var blogTagRequestDto = new BlogTagRequestDto { Id = blogTagId };
                    var blogTag = await _blogTagService.GetBlogTag(blogTagRequestDto);
                    article.BlogTags.Add(blogTag);
                }
            }

            return articles.ToList();
        }
    }
}
