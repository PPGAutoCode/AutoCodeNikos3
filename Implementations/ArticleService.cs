
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
            // Validation Logic
            if (string.IsNullOrEmpty(request.Title) || request.Author == Guid.Empty || string.IsNullOrEmpty(request.Langcode) || request.BlogCategories == null || !request.BlogCategories.Any())
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Fetch and Map Author
            var authorRequest = new AuthorRequestDto { Id = request.Author };
            var author = await _authorService.GetAuthor(authorRequest);
            if (author == null)
            {
                throw new BusinessException("DP-404", "Technical Error");
            }

            // Fetch BlogCategories
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

            // Fetch or Create BlogTags
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
                        var createTagDto = new CreateBlogTagDto { Name = tagName };
                        var newTagId = await _blogTagService.CreateBlogTag(createTagDto);
                        var newTag = await _blogTagService.GetBlogTag(new BlogTagRequestDto { Id = Guid.Parse(newTagId) });
                        if (newTag != null)
                        {
                            blogTags.Add(newTag);
                        }
                    }
                }
            }

            // Upload Attachment File
            Attachment pdf = null;
            if (request.PDF != null)
            {
                pdf = await _attachmentService.UploadAttachment(request.PDF);
            }

            // Upload Image File
            Image image = null;
            if (request.Image != null)
            {
                image = await _imageService.UploadImage(request.Image);
            }

            // Create new Article object
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

            // Create new list of ArticleBlogCategories objects
            var articleBlogCategories = blogCategories.Select(category => new ArticleBlogCategory
            {
                Id = Guid.NewGuid(),
                ArticleId = article.Id,
                BlogCategoryId = category.Id
            }).ToList();

            // Create new list of ArticleBlogTags objects
            var articleBlogTags = blogTags.Select(tag => new ArticleBlogTag
            {
                Id = Guid.NewGuid(),
                ArticleId = article.Id,
                BlogTagId = tag.Id
            }).ToList();

            // Perform Database Operations in a Single Transaction
            try
            {
                _dbConnection.Open();
                using (var transaction = _dbConnection.BeginTransaction())
                {
                    await _dbConnection.ExecuteAsync(
                        "INSERT INTO Articles (Id, Title, AuthorId, Summary, Body, GoogleDriveId, HideScrollSpy, ImageId, PDFId, Langcode, Status, Sticky, Promote, Version, Created, CreatorId) " +
                        "VALUES (@Id, @Title, @AuthorId, @Summary, @Body, @GoogleDriveId, @HideScrollSpy, @ImageId, @PDFId, @Langcode, @Status, @Sticky, @Promote, @Version, @Created, @CreatorId)",
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
                            PDFId = article.Pdf?.Id,
                            article.Langcode,
                            article.Status,
                            article.Sticky,
                            article.Promote,
                            article.Version,
                            article.Created,
                            article.CreatorId
                        }, transaction);

                    await _dbConnection.ExecuteAsync(
                        "INSERT INTO ArticleBlogCategories (Id, ArticleId, BlogCategoryId) VALUES (@Id, @ArticleId, @BlogCategoryId)",
                        articleBlogCategories, transaction);

                    await _dbConnection.ExecuteAsync(
                        "INSERT INTO ArticleBlogTags (Id, ArticleId, BlogTagId) VALUES (@Id, @ArticleId, @BlogTagId)",
                        articleBlogTags, transaction);

                    transaction.Commit();
                }
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
            finally
            {
                if (_dbConnection.State == ConnectionState.Open)
                    _dbConnection.Close();
            }

            return article.Id.ToString();
        }
    }
}
