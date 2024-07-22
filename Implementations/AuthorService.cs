
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
    public class AuthorService : IAuthorService
    {
        private readonly IDbConnection _dbConnection;
        private readonly IImageService _imageService;

        public AuthorService(IDbConnection dbConnection, IImageService imageService)
        {
            _dbConnection = dbConnection;
            _imageService = imageService;
        }

        public async Task<string> CreateAuthor(CreateAuthorDto request)
        {
            // Step 1: Validate the request payload
            if (string.IsNullOrEmpty(request.Name))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Upload Image Files
            Guid? imageId = null;
            if (request.Image != null)
            {
                imageId = Guid.Parse(await _imageService.CreateImage(request.Image));
            }

            // Step 3: Create a new Author object
            var author = new Author
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Image = imageId,
                Details = request.Details
            };

            // Step 4: Insert author in the database table Authors
            const string sql = "INSERT INTO Authors (Id, Name, Image, Details) VALUES (@Id, @Name, @Image, @Details)";
            try
            {
                await _dbConnection.ExecuteAsync(sql, author);
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }

            // Step 5: Return response.payload = Author.Id from the database
            return author.Id.ToString();
        }

        public async Task<Author> GetAuthor(AuthorRequestDto request)
        {
            // Step 1: Validation
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetching from Database
            const string sql = "SELECT * FROM Authors WHERE Id = @Id";
            var author = await _dbConnection.QuerySingleOrDefaultAsync<Author>(sql, new { Id = request.Id });

            // Step 3: Return the Author or handle not found
            if (author == null)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }

            return author;
        }

        public async Task<string> UpdateAuthor(UpdateAuthorDto request)
        {
            // Step 1: Validate UpdateAuthorDto
            if (request.Id == Guid.Empty || string.IsNullOrEmpty(request.Name))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetching Existing Author
            const string selectSql = "SELECT * FROM Authors WHERE Id = @Id";
            var existingAuthor = await _dbConnection.QuerySingleOrDefaultAsync<Author>(selectSql, new { Id = request.Id });

            if (existingAuthor == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Handle Image
            if (request.Image != null)
            {
                await _imageService.HandleImage(request.Image, existingAuthor.Image, imgId => existingAuthor.Image = imgId);
            }

            // Step 4: Update the Author object
            existingAuthor.Name = request.Name;
            existingAuthor.Details = request.Details;

            // Step 5: Perform Database Updates in a Single Transaction
            const string updateSql = "UPDATE Authors SET Name = @Name, Image = @Image, Details = @Details WHERE Id = @Id";
            try
            {
                await _dbConnection.ExecuteAsync(updateSql, existingAuthor);
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }

            // Step 6: Return response.payload = Author.Id.ToString()
            return existingAuthor.Id.ToString();
        }

        public async Task<string> DeleteAuthor(DeleteAuthorDto request)
        {
            // Step 1: Validate Request Payload
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch Existing Author
            const string selectSql = "SELECT * FROM Authors WHERE Id = @Id";
            var existingAuthor = await _dbConnection.QuerySingleOrDefaultAsync<Author>(selectSql, new { Id = request.Id });

            if (existingAuthor == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Delete Related Image
            if (existingAuthor.Image != null)
            {
                var deleteImageDto = new DeleteImageDto { Id = existingAuthor.Image.Value };
                await _imageService.DeleteImage(deleteImageDto);
            }

            // Step 4: Perform Database Updates in a Single Transaction
            const string deleteSql = "DELETE FROM Authors WHERE Id = @Id";
            try
            {
                await _dbConnection.ExecuteAsync(deleteSql, new { Id = request.Id });
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }

            // Step 5: Return response.payload = true
            return "true";
        }

        public async Task<List<Author>> GetListAuthor(ListAuthorRequestDto request)
        {
            // Step 1: Validate the ListAuthorRequestDto
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Set default sorting values if not provided
            var sortField = string.IsNullOrEmpty(request.SortField) ? "Id" : request.SortField;
            var sortOrder = string.IsNullOrEmpty(request.SortOrder) ? "asc" : request.SortOrder;

            // Step 3: Fetch the list of Authors from the database table Authors
            var sql = $"SELECT * FROM Authors ORDER BY {sortField} {sortOrder} OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY";
            var authors = await _dbConnection.QueryAsync<Author>(sql, new { Offset = request.PageOffset, Limit = request.PageLimit });

            // Step 4: Return the list of Authors
            return authors.ToList();
        }
    }
}
