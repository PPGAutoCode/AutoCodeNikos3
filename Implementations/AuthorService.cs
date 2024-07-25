
using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ProjectName.Types;
using ProjectName.Interfaces;
using ProjectName.ControllersExceptions;

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
        if (string.IsNullOrEmpty(request.Name))
        {
            throw new BusinessException("DP-422", "Client Error");
        }

        Guid? imageId = null;
        if (request.Image != null)
        {
            var imageResult = await _imageService.UpsertImage(request.Image);
            imageId = Guid.Parse(imageResult);
        }

        var author = new Author
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Image = imageId,
            Details = request.Details
        };

        try
        {
            await _dbConnection.ExecuteAsync(
                "INSERT INTO Authors (Id, Name, Image, Details) VALUES (@Id, @Name, @Image, @Details)",
                author);
        }
        catch (Exception)
        {
            throw new TechnicalException("DP-500", "Technical Error");
        }

        return author.Id.ToString();
    }

    public async Task<AuthorDto> GetAuthor(AuthorRequestDto request)
    {
        if (request.Id == null)
        {
            throw new BusinessException("DP-422", "Client Error");
        }

        var author = await _dbConnection.QuerySingleOrDefaultAsync<Author>(
            "SELECT * FROM Authors WHERE Id = @Id", new { Id = request.Id });

        if (author == null)
        {
            throw new TechnicalException("DP-404", "Technical Error");
        }

        AuthorDto authorDto = new AuthorDto
        {
            Id = author.Id,
            Name = author.Name,
            Details = author.Details
        };

        if (author.Image != null)
        {
            var image = await _imageService.GetImage(new ImageRequestDto { Id = author.Image.Value });
            authorDto.Image = image;
        }

        return authorDto;
    }

    public async Task<string> UpdateAuthor(UpdateAuthorDto request)
    {
        if (request.Id == null || string.IsNullOrEmpty(request.Name))
        {
            throw new BusinessException("DP-422", "Client Error");
        }

        var existingAuthor = await _dbConnection.QuerySingleOrDefaultAsync<Author>(
            "SELECT * FROM Authors WHERE Id = @Id", new { Id = request.Id });

        if (existingAuthor == null)
        {
            throw new TechnicalException("DP-404", "Technical Error");
        }

        if (request.Image != null)
        {
            var imageResult = await _imageService.UpsertImage(request.Image);
            existingAuthor.Image = Guid.Parse(imageResult);
        }

        existingAuthor.Name = request.Name;
        existingAuthor.Details = request.Details;

        try
        {
            await _dbConnection.ExecuteAsync(
                "UPDATE Authors SET Name = @Name, Image = @Image, Details = @Details WHERE Id = @Id",
                new { Id = existingAuthor.Id, Name = existingAuthor.Name, Image = existingAuthor.Image, Details = existingAuthor.Details });
        }
        catch (Exception)
        {
            throw new TechnicalException("DP-500", "Technical Error");
        }

        return existingAuthor.Id.ToString();
    }

    public async Task<bool> DeleteAuthor(DeleteAuthorDto request)
    {
        if (request.Id == null)
        {
            throw new BusinessException("DP-422", "Client Error");
        }

        var existingAuthor = await _dbConnection.QuerySingleOrDefaultAsync<Author>(
            "SELECT * FROM Authors WHERE Id = @Id", new { Id = request.Id });

        if (existingAuthor == null)
        {
            throw new TechnicalException("DP-404", "Technical Error");
        }

        if (existingAuthor.Image != null)
        {
            await _imageService.DeleteImage(new DeleteImageDto { Id = existingAuthor.Image.Value });
        }

        try
        {
            await _dbConnection.ExecuteAsync(
                "DELETE FROM Authors WHERE Id = @Id", new { Id = request.Id });
        }
        catch (Exception)
        {
            throw new TechnicalException("DP-500", "Technical Error");
        }

        return true;
    }

    public async Task<List<Author>> GetListAuthor(ListAuthorRequestDto request)
    {
        if (request.PageLimit <= 0 || request.PageOffset < 0)
        {
            throw new BusinessException("DP-422", "Client Error");
        }

        string sortField = string.IsNullOrEmpty(request.SortField) ? "Id" : request.SortField;
        string sortOrder = string.IsNullOrEmpty(request.SortOrder) ? "asc" : request.SortOrder;

        var authors = await _dbConnection.QueryAsync<Author>(
            $"SELECT * FROM Authors ORDER BY {sortField} {sortOrder} OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY",
            new { Offset = request.PageOffset, Limit = request.PageLimit });

        return authors.ToList();
    }
}
