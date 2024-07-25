
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
    public class ArticleService : IArticleService
    {
        private readonly IDbConnection _dbConnection;
        private readonly IAuthorService _authorService;
        private readonly IBlogCategoryService _blogCategoryService;
        private readonly IBlogTagService _blogTagService;
        private readonly IAttachmentService _attachmentService;
        private readonly IImageService _imageService;

        public ArticleService(IDbConnection dbConnection, IAuthorService authorService, IBlogCategoryService blogCategoryService, IBlogTagService blogTagService, IAttachmentService attachmentService, IImageService imageService)
        {
            _dbConnection = dbConnection;
            _authorService = authorService;
            _blogCategoryService = blogCategoryService;
            _blogTagService = blogTagService;
            _attachmentService = attachmentService;
            _imageService = imageService;
        }

        public async Task<string> CreateArticle(CreateArticleDto request)
        {
            // Validation Logic
            if (string.IsNullOrEmpty(request.Title) || request.Author == Guid.Empty || 
                string.IsNullOrEmpty(request.Langcode) || request.BlogCategories == null || 
                request.BlogCategories.Count == 0)
            {
                throw new BusinessException("DP-422", "Required parameters are missing.");
            }

            // Fetch Author
            var authorRequest = new AuthorRequestDto { Id = request.Author };
            var author = await _authorService.GetAuthor(authorRequest);
            if (author == null)
            {
                throw new BusinessException("DP-422", "Author not found.");
            }

            // Fetch Blog Categories
            var blogCategories = new List<BlogCategory>();
            foreach (var categoryId in request.BlogCategories)
            {
                var categoryRequest = new BlogCategoryRequestDto { Id = categoryId };
                var category = await _blogCategoryService.GetBlogCategory(categoryRequest);
                if (category != null)
                {
                    blogCategories.Add(category);
                }
            }

            // Fetch Blog Tags
            var blogTags = new List<BlogTag>();
            if (request.BlogTags != null)
            {
                foreach (var tagName in request.BlogTags)
                {
                    var tagRequest = new BlogTagRequestDto { Name = tagName };
                    var tag = await _blogTagService.GetBlogTag(tagRequest);
                    if (tag != null)
                    {
                        blogTags.Add(tag);
                    }
                    else
                    {
                        var newTagId = await _blogTagService.CreateBlogTag(new CreateBlogTagDto { Name = tagName });
                        var newTag = await _blogTagService.GetBlogTag(new BlogTagRequestDto { Id = newTagId });
                        blogTags.Add(newTag);
                    }
                }
            }

            // Upload Attachment
            Attachment pdf = null;
            if (request.PDF != null)
            {
                pdf = await _attachmentService.UploadAttachment(request.PDF);
            }

            // Upload Image
            Image image = null;
            if (request.Image != null)
            {
                image = await _imageService.UploadImage(request.Image);
            }

            // Create Article
            var article = new Article
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Author = author,
                Summary = request.Summary,
                Body = request.Body,
                GoogleDriveId = request.GoogleDriveId,
                HideScrollSpy = request.HideScrollSpy,
                Image = image,
                PDF = pdf,
                Langcode = request.Langcode,
                Status = request.Status,
                Sticky = request.Sticky,
                Promote = request.Promote,
                Version = 1,
                Created = DateTime.UtcNow,
                CreatorId = request.CreatorId
            };

            // Prepare ArticleBlogCategories and ArticleBlogTags
            var articleBlogCategories = new List<ArticleBlogCategory>();
            foreach (var category in blogCategories)
            {
                articleBlogCategories.Add(new ArticleBlogCategory
                {
                    Id = Guid.NewGuid(),
                    ArticleId = article.Id,
                    BlogCategoryId = category.Id
                });
            }

            var articleBlogTags = new List<ArticleBlogTag>();
            foreach (var tag in blogTags)
            {
                articleBlogTags.Add(new ArticleBlogTag
                {
                    Id = Guid.NewGuid(),
                    ArticleId = article.Id,
                    BlogTagId = tag.Id
                });
            }

            // Database Operations in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Insert Article
                    var insertArticleQuery = @"INSERT INTO Articles (Id, Title, AuthorId, Summary, Body, GoogleDriveId, HideScrollSpy, ImageId, PDFId, Langcode, Status, Sticky, Promote, Version, Created, CreatorId) 
                                                VALUES (@Id, @Title, @AuthorId, @Summary, @Body, @GoogleDriveId, @HideScrollSpy, @ImageId, @PDFId, @Langcode, @Status, @Sticky, @Promote, @Version, @Created, @CreatorId)";
                    await _dbConnection.ExecuteAsync(insertArticleQuery, new
                    {
                        Id = article.Id,
                        Title = article.Title,
                        AuthorId = article.Author.Id,
                        Summary = article.Summary,
                        Body = article.Body,
                        GoogleDriveId = article.GoogleDriveId,
                        HideScrollSpy = article.HideScrollSpy,
                        ImageId = image?.Id,
                        PDFId = pdf?.Id,
                        Langcode = article.Langcode,
                        Status = article.Status,
                        Sticky = article.Sticky,
                        Promote = article.Promote,
                        Version = article.Version,
                        Created = article.Created,
                        CreatorId = article.CreatorId
                    }, transaction);

                    // Insert ArticleBlogCategories
                    var insertCategoryQuery = @"INSERT INTO ArticleBlogCategories (Id, ArticleId, BlogCategoryId) VALUES (@Id, @ArticleId, @BlogCategoryId)";
                    foreach (var category in articleBlogCategories)
                    {
                        await _dbConnection.ExecuteAsync(insertCategoryQuery, new
                        {
                            Id = category.Id,
                            ArticleId = category.ArticleId,
                            BlogCategoryId = category.BlogCategoryId
                        }, transaction);
                    }

                    // Insert ArticleBlogTags
                    var insertTagQuery = @"INSERT INTO ArticleBlogTags (Id, ArticleId, BlogTagId) VALUES (@Id, @ArticleId, @BlogTagId)";
                    foreach (var tag in articleBlogTags)
                    {
                        await _dbConnection.ExecuteAsync(insertTagQuery, new
                        {
                            Id = tag.Id,
                            ArticleId = tag.ArticleId,
                            BlogTagId = tag.BlogTagId
                        }, transaction);
                    }

                    // Commit Transaction
                    transaction.Commit();
                }
                catch (Exception)
                {
                    // Rollback Transaction
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "An error occurred while creating the article.");
                }
            }

            // Return ArticleId
            return article.Id.ToString();
        }
    }
}
