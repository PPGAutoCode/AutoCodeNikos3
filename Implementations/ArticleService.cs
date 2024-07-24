
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
            if (string.IsNullOrEmpty(request.Title) || request.Author == Guid.Empty || request.HideScrollSpy == null || string.IsNullOrEmpty(request.Langcode) || request.Status == null || request.Sticky == null || request.Promote == null || request.BlogCategories == null || !request.BlogCategories.Any())
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch and Map Author
            var authorRequest = new AuthorRequestDto { Id = request.Author };
            var author = await _authorService.GetAuthor(authorRequest);
            if (author == null)
            {
                throw new BusinessException("DP-404", "Technical Error");
            }

            // Step 3: Fetch BlogCategories
            var blogCategories = new List<BlogCategory>();
            foreach (var categoryId in request.BlogCategories)
            {
                var blogCategoryRequest = new BlogCategoryRequestDto { Id = categoryId };
                var blogCategory = await _blogCategoryService.GetBlogCategory(blogCategoryRequest);
                if (blogCategory == null)
                {
                    throw new BusinessException("DP-404", "Technical Error");
                }
                blogCategories.Add(blogCategory);
            }

            // Step 4: Fetch or Create BlogTags
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
                        var blogTagId = await _blogTagService.CreateBlogTag(createBlogTagDto);
                        blogTag = new BlogTag { Id = Guid.Parse(blogTagId), Name = tagName };
                    }
                    blogTags.Add(blogTag);
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

            // Step 10: In a single SQL transaction
            try
            {
                _dbConnection.Open();
                using (var transaction = _dbConnection.BeginTransaction())
                {
                    await _dbConnection.ExecuteAsync("INSERT INTO Articles (Id, Title, Author, Summary, Body, GoogleDriveId, HideScrollSpy, Image, PDF, Langcode, Status, Sticky, Promote, Version, Created, CreatorId) VALUES (@Id, @Title, @Author, @Summary, @Body, @GoogleDriveId, @HideScrollSpy, @Image, @PDF, @Langcode, @Status, @Sticky, @Promote, @Version, @Created, @CreatorId)", article, transaction);

                    await _dbConnection.ExecuteAsync("INSERT INTO ArticleBlogCategories (Id, ArticleId, BlogCategoryId) VALUES (@Id, @ArticleId, @BlogCategoryId)", articleBlogCategories, transaction);

                    await _dbConnection.ExecuteAsync("INSERT INTO ArticleBlogTags (Id, ArticleId, BlogTagId) VALUES (@Id, @ArticleId, @BlogTagId)", articleBlogTags, transaction);

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
