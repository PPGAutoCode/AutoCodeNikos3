
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
            // Step 1: Validate the request payload
            if (request == null || string.IsNullOrEmpty(request.Title) || request.Author == Guid.Empty || request.BlogCategories == null || !request.BlogCategories.Any() || string.IsNullOrEmpty(request.Langcode) || request.Status == null || request.Sticky == null || request.Promote == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch and Map Author
            var authorRequest = new AuthorRequestDto { Id = request.Author };
            var author = await _authorService.GetAuthor(authorRequest);
            if (author == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 4: Get the BlogCategories from request.BlogCategories
            var blogCategories = new List<BlogCategory>();
            foreach (var categoryId in request.BlogCategories)
            {
                var blogCategoryRequest = new BlogCategoryRequestDto { Id = categoryId };
                var blogCategory = await _blogCategoryService.GetBlogCategory(blogCategoryRequest);
                if (blogCategory == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
                blogCategories.Add(blogCategory);
            }

            // Step 4: If request.BlogTags is not null, Get the BlogTags from request.BlogTags
            var blogTags = new List<BlogTag>();
            if (request.BlogTags != null)
            {
                foreach (var tagName in request.BlogTags)
                {
                    var blogTagRequest = new BlogTagRequestDto { Name = tagName };
                    var blogTag = await _blogTagService.GetBlogTag(blogTagRequest);
                    if (blogTag == null)
                    {
                        var createBlogTagDto = new CreateBlogTagDto { Name = tagName };
                        var newBlogTagId = await _blogTagService.CreateBlogTag(createBlogTagDto);
                        blogTag = await _blogTagService.GetBlogTag(new BlogTagRequestDto { Id = Guid.Parse(newBlogTagId) });
                    }
                    blogTags.Add(blogTag);
                }
            }

            // Step 5: Upload Attachment File
            Attachment pdf = null;
            if (request.Pdf != null)
            {
                pdf = await _attachmentService.UploadAttachment(request.Pdf);
            }

            // Step 6: Upload Image File
            Image image = null;
            if (request.Image != null)
            {
                image = await _imageService.UploadImage(request.Image);
            }

            // Step 7: Create new Article object
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
                Pdf = pdf,
                Langcode = request.Langcode,
                Status = request.Status,
                Sticky = request.Sticky,
                Promote = request.Promote,
                Version = 1,
                Created = DateTime.UtcNow,
                CreatorId = request.CreatorId
            };

            // Step 8: Create new list of ArticleBlogCategories objects
            var articleBlogCategories = blogCategories.Select(category => new ArticleBlogCategory
            {
                Id = Guid.NewGuid(),
                ArticleId = article.Id,
                BlogCategoryId = category.Id
            }).ToList();

            // Step 9: Create new list of ArticleBlogTags objects
            var articleBlogTags = blogTags.Select(tag => new ArticleBlogTag
            {
                Id = Guid.NewGuid(),
                ArticleId = article.Id,
                BlogTagId = tag.Id
            }).ToList();

            // Step 10: Perform Database Operations in a Single Transaction
            try
            {
                _dbConnection.Open();
                using var transaction = _dbConnection.BeginTransaction();
                try
                {
                    await _dbConnection.ExecuteAsync(
                        "INSERT INTO Articles (Id, Title, AuthorId, Summary, Body, GoogleDriveId, HideScrollSpy, ImageId, PdfId, Langcode, Status, Sticky, Promote, Version, Created, CreatorId) VALUES (@Id, @Title, @AuthorId, @Summary, @Body, @GoogleDriveId, @HideScrollSpy, @ImageId, @PdfId, @Langcode, @Status, @Sticky, @Promote, @Version, @Created, @CreatorId)",
                        new
                        {
                            article.Id,
                            article.Title,
                            AuthorId = article.Author.Id,
                            article.Summary,
                            article.Body,
                            article.GoogleDriveId,
                            article.HideScrollSpy,
                            ImageId = article.Image?.Id,
                            PdfId = article.Pdf?.Id,
                            article.Langcode,
                            article.Status,
                            article.Sticky,
                            article.Promote,
                            article.Version,
                            article.Created,
                            article.CreatorId
                        },
                        transaction
                    );

                    await _dbConnection.ExecuteAsync(
                        "INSERT INTO ArticleBlogCategories (Id, ArticleId, BlogCategoryId) VALUES (@Id, @ArticleId, @BlogCategoryId)",
                        articleBlogCategories,
                        transaction
                    );

                    await _dbConnection.ExecuteAsync(
                        "INSERT INTO ArticleBlogTags (Id, ArticleId, BlogTagId) VALUES (@Id, @ArticleId, @BlogTagId)",
                        articleBlogTags,
                        transaction
                    );

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
                }
            }
            finally
            {
                _dbConnection.Close();
            }

            // Step 11: Return ArticleId from the database
            return article.Id.ToString();
        }
    }
}
