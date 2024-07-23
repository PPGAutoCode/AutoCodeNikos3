
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProjectName.ControllersExceptions;
using ProjectName.Interfaces;
using ProjectName.Types;

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
            // Step 1: Validate all fields of request.payload are not null except from [Summary, Body, Image, PDF, BlogCategories, BlogTags, GoogleDriveID]
            if (request.Title == null || request.Author == Guid.Empty || request.Langcode == null || request.CreatorId == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch and Validate Author
            var authorRequest = new AuthorRequestDto { Id = request.Author };
            var author = await _authorService.GetAuthor(authorRequest);
            if (author == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Get the BlogCategories Ids from request.BlogCategories and fetch the whole entity of BlogCategory
            var blogCategoryIds = new List<Guid>();
            foreach (var categoryId in request.BlogCategories)
            {
                var blogCategoryRequest = new BlogCategoryRequestDto { Id = categoryId };
                var blogCategory = await _blogCategoryService.GetBlogCategory(blogCategoryRequest);
                if (blogCategory == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
                blogCategoryIds.Add(blogCategory.Id);
            }

            // Step 4: If request.BlogTags is not null, Get the BlogTags names from request.BlogTags and fetch the whole entity of BlogTag
            var blogTagIds = new List<Guid>();
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
                        blogTagIds.Add(Guid.Parse(newBlogTagId));
                    }
                    else
                    {
                        blogTagIds.Add(blogTag.Id);
                    }
                }
            }

            // Step 5: Upload Attachment File
            string pdfId = null;
            if (request.PDF != null)
            {
                pdfId = await _attachmentService.CreateAttachment(request.PDF);
            }

            // Step 6: Upload Image File
            string imageId = null;
            if (request.Image != null)
            {
                imageId = await _imageService.CreateImage(request.Image);
            }

            // Step 7: Create new Article object
            var article = new Article
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Author = request.Author,
                Summary = request.Summary,
                Body = request.Body,
                GoogleDriveID = request.GoogleDriveID,
                HideScrollSpy = request.HideScrollSpy,
                Image = imageId != null ? Guid.Parse(imageId) : (Guid?)null,
                PDF = pdfId != null ? Guid.Parse(pdfId) : (Guid?)null,
                Langcode = request.Langcode,
                Status = request.Status,
                Sticky = request.Sticky,
                Promote = request.Promote,
                Version = 1,
                Created = DateTime.UtcNow,
                CreatorId = request.CreatorId
            };

            // Step 8: Create new list of ArticleBlogCategories objects
            var articleBlogCategories = blogCategoryIds.Select(categoryId => new ArticleBlogCategory
            {
                Id = Guid.NewGuid(),
                ArticleId = article.Id,
                BlogCategoryId = categoryId
            }).ToList();

            // Step 9: Create new list of ArticleBlogTags objects
            var articleBlogTags = blogTagIds.Select(tagId => new ArticleBlogTag
            {
                Id = Guid.NewGuid(),
                ArticleId = article.Id,
                BlogTagId = tagId
            }).ToList();

            // Step 10: In a single SQL transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    var insertArticleQuery = @"INSERT INTO Article (Id, Title, Author, Summary, Body, GoogleDriveID, HideScrollSpy, Image, PDF, Langcode, Status, Sticky, Promote, Version, Created, CreatorId) 
                                               VALUES (@Id, @Title, @Author, @Summary, @Body, @GoogleDriveID, @HideScrollSpy, @Image, @PDF, @Langcode, @Status, @Sticky, @Promote, @Version, @Created, @CreatorId)";
                    await _dbConnection.ExecuteAsync(insertArticleQuery, article, transaction);

                    var insertArticleBlogCategoriesQuery = @"INSERT INTO ArticleBlogCategories (Id, ArticleId, BlogCategoryId) 
                                                             VALUES (@Id, @ArticleId, @BlogCategoryId)";
                    await _dbConnection.ExecuteAsync(insertArticleBlogCategoriesQuery, articleBlogCategories, transaction);

                    var insertArticleBlogTagsQuery = @"INSERT INTO ArticleBlogTags (Id, ArticleId, BlogTagId) 
                                                       VALUES (@Id, @ArticleId, @BlogTagId)";
                    await _dbConnection.ExecuteAsync(insertArticleBlogTagsQuery, articleBlogTags, transaction);

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
                }
            }

            return article.Id.ToString();
        }
    }
}
