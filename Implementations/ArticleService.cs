
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
            if (request == null || string.IsNullOrEmpty(request.Title) || request.Author == Guid.Empty || string.IsNullOrEmpty(request.Langcode) || request.BlogCategories == null || !request.BlogCategories.Any())
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Fetch and Map Author
            var authorRequest = new AuthorRequestDto { Id = request.Author };
            var author = await _authorService.GetAuthor(authorRequest);
            if (author == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Fetch BlogCategories
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

            // Fetch or Create BlogTags
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
                        var createdBlogTagId = await _blogTagService.CreateBlogTag(createBlogTagDto);
                        blogTag = await _blogTagService.GetBlogTag(new BlogTagRequestDto { Id = Guid.Parse(createdBlogTagId) });
                    }
                    blogTags.Add(blogTag);
                }
            }

            // Upload Attachment File
            Guid? pdfId = null;
            if (request.Pdf != null)
            {
                var attachmentId = await _attachmentService.UpsertAttachment(request.Pdf);
                pdfId = Guid.Parse(attachmentId);
            }

            // Upload Image File
            Image image = null;
            if (request.Image != null)
            {
                image = await _imageService.UploadImage(request.Image);
            }

            // Create Article Object
            var article = new Article
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Author = author.Id,
                Summary = request.Summary,
                Body = request.Body,
                GoogleDriveId = request.GoogleDriveId,
                HideScrollSpy = request.HideScrollSpy,
                Image = image?.Id,
                Pdf = pdfId,
                Langcode = request.Langcode,
                Status = request.Status,
                Sticky = request.Sticky,
                Promote = request.Promote,
                Version = 1,
                Created = DateTime.UtcNow,
                CreatorId = request.CreatorId
            };

            // Create ArticleBlogCategories and ArticleBlogTags
            var articleBlogCategories = blogCategories.Select(bc => new ArticleBlogCategory
            {
                Id = Guid.NewGuid(),
                ArticleId = article.Id,
                BlogCategoryId = bc.Id
            }).ToList();

            var articleBlogTags = blogTags.Select(bt => new ArticleBlogTag
            {
                Id = Guid.NewGuid(),
                ArticleId = article.Id,
                BlogTagId = bt.Id
            }).ToList();

            // Perform Database Operations in a Single Transaction
            try
            {
                _dbConnection.Open();
                using var transaction = _dbConnection.BeginTransaction();

                await _dbConnection.ExecuteAsync(
                    "INSERT INTO Articles (Id, Title, Author, Summary, Body, GoogleDriveId, HideScrollSpy, Image, Pdf, Langcode, Status, Sticky, Promote, Version, Created, CreatorId) VALUES (@Id, @Title, @Author, @Summary, @Body, @GoogleDriveId, @HideScrollSpy, @Image, @Pdf, @Langcode, @Status, @Sticky, @Promote, @Version, @Created, @CreatorId)",
                    article, transaction);

                await _dbConnection.ExecuteAsync(
                    "INSERT INTO ArticleBlogCategories (Id, ArticleId, BlogCategoryId) VALUES (@Id, @ArticleId, @BlogCategoryId)",
                    articleBlogCategories, transaction);

                await _dbConnection.ExecuteAsync(
                    "INSERT INTO ArticleBlogTags (Id, ArticleId, BlogTagId) VALUES (@Id, @ArticleId, @BlogTagId)",
                    articleBlogTags, transaction);

                transaction.Commit();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
            finally
            {
                _dbConnection.Close();
            }

            return article.Id.ToString();
        }
    }
}
