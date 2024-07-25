
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
            // 1. Validate required parameters
            if (string.IsNullOrEmpty(request.Title) || request.AuthorId == Guid.Empty || string.IsNullOrEmpty(request.Langcode) || request.BlogCategories == null || request.BlogCategories.Count == 0)
            {
                throw new BusinessException("DP-422", "Required parameters are missing.");
            }

            // 2. Fetch and Map Author
            var authorRequest = new AuthorRequestDto { Id = request.AuthorId };
            var author = await _authorService.GetAuthor(authorRequest);
            if (author == null)
            {
                throw new TechnicalException("DP-404", "Author not found.");
            }

            // 3. Fetch Blog Categories
            var blogCategories = new List<BlogCategory>();
            foreach (var categoryId in request.BlogCategories)
            {
                var categoryRequest = new BlogCategoryRequestDto { Id = categoryId };
                var category = await _blogCategoryService.GetBlogCategory(categoryRequest);
                if (category == null)
                {
                    throw new TechnicalException("DP-404", "BlogCategory not found.");
                }
                blogCategories.Add(category);
            }

            // 4. Fetch Blog Tags
            var blogTags = new List<BlogTag>();
            if (request.BlogTags != null)
            {
                foreach (var tagName in request.BlogTags)
                {
                    var tagRequest = new BlogTagRequestDto { Name = tagName };
                    var tag = await _blogTagService.GetBlogTag(tagRequest);
                    if (tag == null)
                    {
                        var newTagId = await _blogTagService.CreateBlogTag(new CreateBlogTagDto { Name = tagName });
                        tag = await _blogTagService.GetBlogTag(new BlogTagRequestDto { Id = newTagId });
                    }
                    blogTags.Add(tag);
                }
            }

            // 5. Upload Attachment File
            Guid? pdfId = null;
            if (request.Pdf != null)
            {
                pdfId = Guid.Parse(await _attachmentService.UpsertAttachment(request.Pdf));
            }

            // 6. Upload Image File
            Image image = null;
            if (request.Image != null)
            {
                image = await _imageService.UploadImage(request.Image);
            }

            // 7. Create new Article object
            var article = new Article
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                AuthorId = request.AuthorId,
                Summary = request.Summary,
                Body = request.Body,
                GoogleDriveId = request.GoogleDriveId,
                HideScrollSpy = request.HideScrollSpy,
                ImageId = image?.Id,
                PdfId = pdfId,
                Langcode = request.Langcode,
                Status = request.Status,
                Sticky = request.Sticky,
                Promote = request.Promote,
                Version = 1,
                Created = DateTime.UtcNow,
                CreatorId = request.CreatorId
            };

            // 8. Create lists for ArticleBlogCategories and ArticleBlogTags
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

            // 9. Perform Database Operations in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Insert article
                    var insertArticleQuery = "INSERT INTO Articles (Id, Title, AuthorId, Summary, Body, GoogleDriveId, HideScrollSpy, ImageId, PdfId, Langcode, Status, Sticky, Promote, Version, Created, CreatorId) " +
                                              "VALUES (@Id, @Title, @AuthorId, @Summary, @Body, @GoogleDriveId, @HideScrollSpy, @ImageId, @PdfId, @Langcode, @Status, @Sticky, @Promote, @Version, @Created, @CreatorId)";
                    await _dbConnection.ExecuteAsync(insertArticleQuery, article, transaction);

                    // Insert ArticleBlogCategories
                    foreach (var articleBlogCategory in articleBlogCategories)
                    {
                        var insertCategoryQuery = "INSERT INTO ArticleBlogCategories (Id, ArticleId, BlogCategoryId) VALUES (@Id, @ArticleId, @BlogCategoryId)";
                        await _dbConnection.ExecuteAsync(insertCategoryQuery, articleBlogCategory, transaction);
                    }

                    // Insert ArticleBlogTags
                    foreach (var articleBlogTag in articleBlogTags)
                    {
                        var insertTagQuery = "INSERT INTO ArticleBlogTags (Id, ArticleId, BlogTagId) VALUES (@Id, @ArticleId, @BlogTagId)";
                        await _dbConnection.ExecuteAsync(insertTagQuery, articleBlogTag, transaction);
                    }

                    // Commit transaction
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "An error occurred while saving the article.");
                }
            }

            // 10. Return ArticleId
            return article.Id.ToString();
        }

        public async Task<Article> GetArticle(ArticleRequestDto request)
        {
            // 1. Validate Request Payload
            if (request.Id == Guid.Empty && string.IsNullOrEmpty(request.Title))
            {
                throw new BusinessException("DP-422", "Either Id or Title must be provided.");
            }

            // 2. Create an ArticleDto instance
            ArticleDto articleDto;

            // 3. Fetch ArticleDto
            if (request.Id != Guid.Empty)
            {
                var selectByIdQuery = "SELECT * FROM Articles WHERE Id = @Id";
                articleDto = await _dbConnection.QuerySingleOrDefaultAsync<ArticleDto>(selectByIdQuery, new { Id = request.Id });
            }
            else
            {
                var selectByTitleQuery = "SELECT * FROM Articles WHERE Title = @Title";
                articleDto = await _dbConnection.QuerySingleOrDefaultAsync<ArticleDto>(selectByTitleQuery, new { Title = request.Title });
            }

            // 4. Fetch and Map Author
            if (articleDto != null)
            {
                var authorRequest = new AuthorRequestDto { Id = articleDto.AuthorId };
                var author = await _authorService.GetAuthor(authorRequest);
                if (author == null)
                {
                    throw new TechnicalException("DP-404", "Author not found.");
                }

                // 5. Fetch and Map Attachment
                Attachment attachment = null;
                if (articleDto.PdfId.HasValue)
                {
                    var attachmentRequest = new AttachmentRequestDto { Id = articleDto.PdfId.Value };
                    attachment = await _attachmentService.GetAttachment(attachmentRequest);
                    if (attachment == null)
                    {
                        throw new TechnicalException("DP-404", "Attachment not found.");
                    }
                }

                // 6. Fetch and Map Image
                Image image = null;
                if (articleDto.ImageId.HasValue)
                {
                    var imageRequest = new ImageRequestDto { Id = articleDto.ImageId.Value };
                    image = await _imageService.GetImage(imageRequest);
                    if (image == null)
                    {
                        throw new TechnicalException("DP-404", "Image not found.");
                    }
                }

                // 7. Fetch Associated BlogCategories
                var blogCategories = new List<BlogCategory>();
                var categoryIdsQuery = "SELECT BlogCategoryId FROM ArticleBlogCategories WHERE ArticleId = @ArticleId";
                var categoryIds = await _dbConnection.QueryAsync<Guid>(categoryIdsQuery, new { ArticleId = articleDto.Id });
                foreach (var categoryId in categoryIds)
                {
                    var categoryRequest = new BlogCategoryRequestDto { Id = categoryId };
                    var category = await _blogCategoryService.GetBlogCategory(categoryRequest);
                    if (category != null)
                    {
                        blogCategories.Add(category);
                    }
                }

                // 8. Fetch Associated BlogTags
                var blogTags = new List<BlogTag>();
                var tagIdsQuery = "SELECT BlogTagId FROM ArticleBlogTags WHERE ArticleId = @ArticleId";
                var tagIds = await _dbConnection.QueryAsync<Guid>(tagIdsQuery, new { ArticleId = articleDto.Id });
                foreach (var tagId in tagIds)
                {
                    var tagRequest = new BlogTagRequestDto { Id = tagId };
                    var tag = await _blogTagService.GetBlogTag(tagRequest);
                    if (tag != null)
                    {
                        blogTags.Add(tag);
                    }
                }

                // 9. Map and Return the Article
                return new Article
                {
                    Id = articleDto.Id,
                    Title = articleDto.Title,
                    Author = author,
                    Summary = articleDto.Summary,
                    Body = articleDto.Body,
                    GoogleDriveId = articleDto.GoogleDriveId,
                    HideScrollSpy = articleDto.HideScrollSpy,
                    Image = image,
                    Pdf = attachment,
                    Langcode = articleDto.Langcode,
                    Status = articleDto.Status,
                    Sticky = articleDto.Sticky,
                    Promote = articleDto.Promote,
                    Version = articleDto.Version,
                    Created = articleDto.Created,
                    Changed = articleDto.Changed,
                    CreatorId = articleDto.CreatorId,
                    ChangedUser = articleDto.ChangedUser,
                    BlogCategories = blogCategories,
                    BlogTags = blogTags
                };
            }

            throw new TechnicalException("DP-404", "Article not found.");
        }
    }
}
