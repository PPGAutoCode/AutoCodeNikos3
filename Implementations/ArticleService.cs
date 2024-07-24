
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

        public ArticleService(IDbConnection dbConnection, 
                              IAuthorService authorService, 
                              IBlogCategoryService blogCategoryService, 
                              IBlogTagService blogTagService, 
                              IAttachmentService attachmentService, 
                              IImageService imageService)
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
            // Validation
            if (string.IsNullOrEmpty(request.Title) || 
                request.Author == Guid.Empty || 
                string.IsNullOrEmpty(request.Langcode) || 
                request.BlogCategories == null || 
                request.BlogCategories.Count == 0)
            {
                throw new BusinessException("DP-422", "Required parameters are missing.");
            }

            // Fetch Author
            var authorRequest = new AuthorRequestDto { Id = request.Author };
            var author = await _authorService.GetAuthor(authorRequest);
            if (author == null)
            {
                throw new TechnicalException("DP-404", "Author not found.");
            }

            // Fetch Blog Categories
            var blogCategoryIds = new List<Guid>();
            foreach (var categoryId in request.BlogCategories)
            {
                var blogCategoryRequest = new BlogCategoryRequestDto { Id = categoryId };
                var blogCategory = await _blogCategoryService.GetBlogCategory(blogCategoryRequest);
                if (blogCategory == null)
                {
                    throw new TechnicalException("DP-404", "Blog category not found.");
                }
                blogCategoryIds.Add(blogCategory.Id);
            }

            // Fetch Blog Tags
            var blogTagIds = new List<Guid>();
            if (request.BlogTags != null)
            {
                foreach (var tagName in request.BlogTags)
                {
                    var blogTagRequest = new BlogTagRequestDto { Name = tagName };
                    var blogTag = await _blogTagService.GetBlogTag(blogTagRequest);
                    if (blogTag != null)
                    {
                        blogTagIds.Add(blogTag.Id);
                    }
                    else
                    {
                        var newTagId = await _blogTagService.CreateBlogTag(new CreateBlogTagDto { Name = tagName });
                        blogTagIds.Add(new Guid(newTagId));
                    }
                }
            }

            // Upload PDF Attachment
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
            foreach (var categoryId in blogCategoryIds)
            {
                articleBlogCategories.Add(new ArticleBlogCategory
                {
                    Id = Guid.NewGuid(),
                    ArticleId = article.Id,
                    BlogCategoryId = categoryId
                });
            }

            var articleBlogTags = new List<ArticleBlogTag>();
            foreach (var tagId in blogTagIds)
            {
                articleBlogTags.Add(new ArticleBlogTag
                {
                    Id = Guid.NewGuid(),
                    ArticleId = article.Id,
                    BlogTagId = tagId
                });
            }

            // Insert into database
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    var insertArticleQuery = @"INSERT INTO Articles (Id, Title, Author, Summary, Body, GoogleDriveId, HideScrollSpy, Image, PDF, Langcode, Status, Sticky, Promote, Version, Created, CreatorId) 
                                                VALUES (@Id, @Title, @Author, @Summary, @Body, @GoogleDriveId, @HideScrollSpy, @Image, @PDF, @Langcode, @Status, @Sticky, @Promote, @Version, @Created, @CreatorId)";
                    await _dbConnection.ExecuteAsync(insertArticleQuery, article, transaction);

                    foreach (var category in articleBlogCategories)
                    {
                        var insertCategoryQuery = @"INSERT INTO ArticleBlogCategories (Id, ArticleId, BlogCategoryId) 
                                                     VALUES (@Id, @ArticleId, @BlogCategoryId)";
                        await _dbConnection.ExecuteAsync(insertCategoryQuery, category, transaction);
                    }

                    foreach (var tag in articleBlogTags)
                    {
                        var insertTagQuery = @"INSERT INTO ArticleBlogTags (Id, ArticleId, BlogTagId) 
                                                VALUES (@Id, @ArticleId, @BlogTagId)";
                        await _dbConnection.ExecuteAsync(insertTagQuery, tag, transaction);
                    }

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "An error occurred while saving the article.");
                }
            }

            return article.Id.ToString();
        }
    }
}
