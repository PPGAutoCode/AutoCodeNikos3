
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
            if (request.Title == null || request.Author == null || request.HideScrollSpy == null || request.Langcode == null || request.Status == null || request.Sticky == null || request.Promote == null || request.BlogCategories == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch and Map Author
            var authorRequestDto = new AuthorRequestDto { Id = request.Author.Id };
            var author = await _authorService.GetAuthor(authorRequestDto);
            if (author == null)
            {
                throw new BusinessException("DP-404", "Technical Error");
            }

            // Step 3: Get the BlogCategories Ids from request.BlogCategories
            var blogCategoriesIds = new List<Guid>();
            foreach (var category in request.BlogCategories)
            {
                var blogCategoryRequestDto = new BlogCategoryRequestDto { Id = category.Id };
                var blogCategory = await _blogCategoryService.GetBlogCategory(blogCategoryRequestDto);
                if (blogCategory == null)
                {
                    throw new BusinessException("DP-404", "Technical Error");
                }
                blogCategoriesIds.Add(blogCategory.Id);
            }

            // Step 4: If request.BlogTags is not null, Get the BlogTags names from request.BlogTags
            var blogTagsIds = new List<Guid>();
            if (request.BlogTags != null)
            {
                foreach (var tag in request.BlogTags)
                {
                    var blogTagRequestDto = new BlogTagRequestDto { Name = tag.Name };
                    var blogTag = await _blogTagService.GetBlogTag(blogTagRequestDto);
                    if (blogTag == null)
                    {
                        var createBlogTagDto = new CreateBlogTagDto { Name = tag.Name };
                        var newBlogTagId = await _blogTagService.CreateBlogTag(createBlogTagDto);
                        blogTagsIds.Add(Guid.Parse(newBlogTagId));
                    }
                    else
                    {
                        blogTagsIds.Add(blogTag.Id);
                    }
                }
            }

            // Step 5: Upload Attachment File
            Attachment pdf = null;
            if (request.PDF != null)
            {
                pdf = await _attachmentService.UploadAttachment(request.PDF);
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
                HideScrollSpy = request.HideScrollSpy.Value,
                Image = image,
                PDF = pdf,
                Langcode = request.Langcode,
                Status = request.Status.Value,
                Sticky = request.Sticky.Value,
                Promote = request.Promote.Value,
                Version = 1,
                Created = DateTime.UtcNow,
                CreatorId = request.CreatorId
            };

            // Step 8: Create new list of ArticleBlogCategories objects
            var articleBlogCategories = blogCategoriesIds.Select(categoryId => new ArticleBlogCategory
            {
                Id = Guid.NewGuid(),
                ArticleId = article.Id,
                BlogCategoryId = categoryId
            }).ToList();

            // Step 9: Create new list of ArticleBlogTags objects
            var articleBlogTags = blogTagsIds.Select(tagId => new ArticleBlogTag
            {
                Id = Guid.NewGuid(),
                ArticleId = article.Id,
                BlogTagId = tagId
            }).ToList();

            // Step 10: Perform Database Operations in a Single Transaction
            try
            {
                _dbConnection.Open();
                using var transaction = _dbConnection.BeginTransaction();

                await _dbConnection.ExecuteAsync(
                    "INSERT INTO Articles (Id, Title, Author, Summary, Body, GoogleDriveId, HideScrollSpy, Image, PDF, Langcode, Status, Sticky, Promote, Version, Created, CreatorId) VALUES (@Id, @Title, @Author, @Summary, @Body, @GoogleDriveId, @HideScrollSpy, @Image, @PDF, @Langcode, @Status, @Sticky, @Promote, @Version, @Created, @CreatorId)",
                    new
                    {
                        article.Id,
                        article.Title,
                        Author = article.Author.Id,
                        article.Summary,
                        article.Body,
                        article.GoogleDriveId,
                        article.HideScrollSpy,
                        Image = article.Image?.Id,
                        PDF = article.PDF?.Id,
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
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
            finally
            {
                if (_dbConnection.State == ConnectionState.Open)
                    _dbConnection.Close();
            }

            // Step 11: Return ArticleId
            return article.Id.ToString();
        }
    }
}
