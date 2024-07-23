
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProjectName.Interfaces;
using ProjectName.Types;
using ProjectName.ControllersExceptions;

namespace ProjectName.Services
{
    public class ArticleService : IArticleService
    {
        private readonly IDbConnection _dbConnection;
        private readonly IBlogCategoryService _blogCategoryService;

        public ArticleService(IDbConnection dbConnection, IBlogCategoryService blogCategoryService)
        {
            _dbConnection = dbConnection;
            _blogCategoryService = blogCategoryService;
        }

        public async Task<Article> GetArticle(ArticleRequestDto request)
        {
            if (request.Id == null && string.IsNullOrEmpty(request.Title))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            Article article;
            if (request.Id != null)
            {
                article = await _dbConnection.QuerySingleOrDefaultAsync<Article>("SELECT * FROM Articles WHERE Id = @Id", new { Id = request.Id });
            }
            else
            {
                article = await _dbConnection.QuerySingleOrDefaultAsync<Article>("SELECT * FROM Articles WHERE Title = @Title", new { Title = request.Title });
            }

            if (article == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            var blogCategoryIds = await _dbConnection.QueryAsync<Guid>("SELECT BlogCategoryId FROM ArticleBlogCategories WHERE ArticleId = @ArticleId", new { ArticleId = article.Id });
            var blogCategories = new List<BlogCategory>();

            foreach (var categoryId in blogCategoryIds)
            {
                var categoryRequest = new BlogCategoryRequestDto { Id = categoryId };
                var category = await _blogCategoryService.GetBlogCategory(categoryRequest);
                if (category == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
                blogCategories.Add(category);
            }

            var blogTagIds = await _dbConnection.QueryAsync<Guid>("SELECT BlogTagId FROM ArticleBlogTags WHERE ArticleId = @ArticleId", new { ArticleId = article.Id });
            var blogTags = await _dbConnection.QueryAsync<BlogTag>("SELECT * FROM BlogTags WHERE Id IN @Ids", new { Ids = blogTagIds });

            if (blogTags.Count() != blogTagIds.Count())
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            article.BlogCategories = blogCategories;
            article.BlogTags = blogTags.ToList();

            return article;
        }
    }
}
