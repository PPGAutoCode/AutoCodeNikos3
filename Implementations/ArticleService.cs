
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
        private readonly IAttachmentService _attachmentService;
        private readonly IImageService _imageService;
        private readonly IBlogCategoryService _blogCategoryService;
        private readonly IBlogTagService _blogTagService;

        public ArticleService(IDbConnection dbConnection, IAuthorService authorService, IAttachmentService attachmentService, IImageService imageService, IBlogCategoryService blogCategoryService, IBlogTagService blogTagService)
        {
            _dbConnection = dbConnection;
            _authorService = authorService;
            _attachmentService = attachmentService;
            _imageService = imageService;
            _blogCategoryService = blogCategoryService;
            _blogTagService = blogTagService;
        }

        public async Task<Article> GetArticle(ArticleRequestDto request)
        {
            // Step 1: Validate Request Payload
            if (request.Id == null && string.IsNullOrEmpty(request.Title))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            ArticleDto articleDto = null;

            // Step 4: Fetch ArticleDto
            if (request.Id != null)
            {
                articleDto = await _dbConnection.QuerySingleOrDefaultAsync<ArticleDto>("SELECT * FROM Articles WHERE Id = @Id", new { Id = request.Id });
            }
            else if (!string.IsNullOrEmpty(request.Title))
            {
                articleDto = await _dbConnection.QuerySingleOrDefaultAsync<ArticleDto>("SELECT * FROM Articles WHERE Title = @Title", new { Title = request.Title });
            }

            if (articleDto == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 5: Fetch and Map Author
            var authorRequestDto = new AuthorRequestDto { Id = articleDto.Author };
            var author = await _authorService.GetAuthor(authorRequestDto);
            if (author == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 6: Fetch and Map Attachment
            Attachment attachment = null;
            if (articleDto.Pdf != null)
            {
                var attachmentRequestDto = new AttachmentRequestDto { Id = articleDto.Pdf };
                attachment = await _attachmentService.GetAttachment(attachmentRequestDto);
                if (attachment == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
            }

            // Step 7: Fetch and Map Image
            Image image = null;
            if (articleDto.Image != null)
            {
                var imageRequestDto = new ImageRequestDto { Id = articleDto.Image };
                image = await _imageService.GetImage(imageRequestDto);
                if (image == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
            }

            // Step 10: Fetch Associated BlogCategories
            var blogCategoryIds = await _dbConnection.QueryAsync<Guid>("SELECT BlogCategoryId FROM ArticleBlogCategories WHERE ArticleId = @ArticleId", new { ArticleId = articleDto.Id });
            var blogCategories = new List<BlogCategory>();
            foreach (var categoryId in blogCategoryIds)
            {
                var blogCategoryRequestDto = new BlogCategoryRequestDto { Id = categoryId };
                var blogCategory = await _blogCategoryService.GetBlogCategory(blogCategoryRequestDto);
                if (blogCategory != null)
                {
                    blogCategories.Add(blogCategory);
                }
            }

            // Step 12: Fetch Associated BlogTags
            var blogTagIds = await _dbConnection.QueryAsync<Guid>("SELECT BlogTagId FROM ArticleBlogTags WHERE ArticleId = @ArticleId", new { ArticleId = articleDto.Id });
            var blogTags = new List<BlogTag>();
            if (blogTagIds.Any())
            {
                foreach (var tagId in blogTagIds)
                {
                    var blogTagRequestDto = new BlogTagRequestDto { Id = tagId };
                    var blogTag = await _blogTagService.GetBlogTag(blogTagRequestDto);
                    if (blogTag != null)
                    {
                        blogTags.Add(blogTag);
                    }
                }
            }
            else
            {
                blogTags = null;
            }

            // Step 13: Map and Return the Article
            var article = new Article
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
                BlogCategories = blogCategories,
                BlogTags = blogTags,
                Version = articleDto.Version,
                Created = articleDto.Created,
                Changed = articleDto.Changed,
                CreatorId = articleDto.CreatorId,
                ChangedUser = articleDto.ChangedUser
            };

            return article;
        }
    }
}
