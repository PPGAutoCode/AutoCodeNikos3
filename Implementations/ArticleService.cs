
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

        public async Task<Article> GetArticle(ArticleRequestDto articleRequestDto)
        {
            // Step 1: Validate Request Payload
            if (articleRequestDto.Id == null && string.IsNullOrEmpty(articleRequestDto.Title))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch Article
            Article article;
            if (articleRequestDto.Id != null)
            {
                article = await _dbConnection.QuerySingleOrDefaultAsync<Article>("SELECT * FROM Articles WHERE Id = @Id", new { Id = articleRequestDto.Id });
            }
            else
            {
                article = await _dbConnection.QuerySingleOrDefaultAsync<Article>("SELECT * FROM Articles WHERE Title = @Title", new { Title = articleRequestDto.Title });
            }

            if (article == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Fetch Associated BlogCategories
            var blogCategoryIds = await _dbConnection.QueryAsync<Guid>("SELECT BlogCategoryId FROM ArticleBlogCategories WHERE ArticleId = @ArticleId", new { ArticleId = article.Id });
            var blogCategories = new List<BlogCategory>();
            foreach (var blogCategoryId in blogCategoryIds)
            {
                var blogCategoryRequestDto = new BlogCategoryRequestDto { Id = blogCategoryId };
                var blogCategory = await _blogCategoryService.GetBlogCategory(blogCategoryRequestDto);
                blogCategories.Add(blogCategory);
            }

            // Step 4: Fetch Associated BlogTags
            var blogTagIds = await _dbConnection.QueryAsync<Guid>("SELECT BlogTagId FROM ArticleBlogTags WHERE ArticleId = @ArticleId", new { ArticleId = article.Id });
            var blogTags = new List<BlogTag>();
            foreach (var blogTagId in blogTagIds)
            {
                var blogTagRequestDto = new BlogTagRequestDto { Id = blogTagId };
                var blogTag = await _blogTagService.GetBlogTag(blogTagRequestDto);
                blogTags.Add(blogTag);
            }

            // Step 5: Map and Return the Article
            article.BlogCategories = blogCategories;
            article.BlogTags = blogTags;
            return article;
        }
    }
}
