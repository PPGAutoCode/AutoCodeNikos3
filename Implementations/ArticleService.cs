
using System;
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

        public ArticleService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task CreateArticleAsync(Article article)
        {
            ValidateArticle(article);

            const string sql = @"
                INSERT INTO Articles (Title, Content, AuthorId, CreatedAt)
                VALUES (@Title, @Content, @AuthorId, @CreatedAt);
            ";

            try
            {
                await _dbConnection.ExecuteAsync(sql, article);
            }
            catch (Exception ex)
            {
                throw new TechnicalException("TECH_001", "An error occurred while creating the article.");
            }
        }

        public async Task<Article> GetArticleByIdAsync(int id)
        {
            const string sql = @"
                SELECT * FROM Articles WHERE Id = @Id;
            ";

            try
            {
                return await _dbConnection.QuerySingleOrDefaultAsync<Article>(sql, new { Id = id });
            }
            catch (Exception ex)
            {
                throw new TechnicalException("TECH_002", "An error occurred while retrieving the article.");
            }
        }

        private void ValidateArticle(Article article)
        {
            if (article == null)
            {
                throw new BusinessException("BUS_001", "Article cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(article.Title))
            {
                throw new BusinessException("BUS_002", "Article title cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(article.Content))
            {
                throw new BusinessException("BUS_003", "Article content cannot be null or empty.");
            }

            if (article.AuthorId <= 0)
            {
                throw new BusinessException("BUS_004", "Invalid AuthorId.");
            }
        }
    }
}
