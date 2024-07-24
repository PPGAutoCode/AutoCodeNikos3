
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
        private readonly IAttachmentService _attachmentService;
        private readonly IImageService _imageService;

        public ArticleService(IDbConnection dbConnection, IAttachmentService attachmentService, IImageService imageService)
        {
            _dbConnection = dbConnection;
            _attachmentService = attachmentService;
            _imageService = imageService;
        }

        public async Task<bool> DeleteArticle(DeleteArticleDto deleteArticleDto)
        {
            // Step 1: Validate Request Payload
            if (deleteArticleDto == null || deleteArticleDto.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch Existing Article
            var article = await _dbConnection.QuerySingleOrDefaultAsync<Article>("SELECT * FROM Articles WHERE Id = @Id", new { Id = deleteArticleDto.Id });
            if (article == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Delete Associated Attachment
            if (article.PDF != null)
            {
                var deleteAttachmentDto = new DeleteAttachmentDto { Id = article.PDF };
                await _attachmentService.DeleteAttachment(deleteAttachmentDto);
            }

            // Step 4: Delete Related Image
            if (article.Image != null)
            {
                var deleteImageDto = new DeleteImageDto { Id = article.Image };
                await _imageService.DeleteImage(deleteImageDto);
            }

            // Step 5: Perform Database Updates in a Single Transaction
            using (var transaction = _dbConnection.BeginTransaction())
            {
                try
                {
                    // Delete ArticleBlogCategories
                    await _dbConnection.ExecuteAsync("DELETE FROM ArticleBlogCategories WHERE ArticleId = @ArticleId", new { ArticleId = article.Id }, transaction);

                    // Delete ArticleBlogTags
                    await _dbConnection.ExecuteAsync("DELETE FROM ArticleBlogTags WHERE ArticleId = @ArticleId", new { ArticleId = article.Id }, transaction);

                    // Delete Article
                    await _dbConnection.ExecuteAsync("DELETE FROM Articles WHERE Id = @Id", new { Id = article.Id }, transaction);

                    // Commit the transaction
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw new TechnicalException("DP-500", "Technical Error");
                }
            }

            return true;
        }
    }
}
