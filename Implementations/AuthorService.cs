
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

        Author author = new Author
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Details = request.Details
        };

        if (request.Image != null)
        {
            var imageId = await _imageService.CreateImage(request.Image);
            author.Image = Guid.Parse(imageId);
        }

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
        if (request.Id == Guid.Empty)
        {
            throw new BusinessException("DP-422", "Client Error");
        }

        var author = await _dbConnection.QuerySingleOrDefaultAsync<Author>(
            "SELECT Id, Name, Image, Details FROM Authors WHERE Id = @Id",
            new { Id = request.Id });

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
            var image = await _imageService.GetImage(new ImageRequestDto { Id = author.Image });
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

        var author = await _dbConnection.QuerySingleOrDefaultAsync<Author>(
            "SELECT Id, Name, Image, Details FROM Authors WHERE Id = @Id",
            new { Id = request.Id });

        if (author == null)
        {
            throw new TechnicalException("DP-404", "Technical Error");
        }

        if (request.Image != null)
        {
            var imageId = await _imageService.UpdateImage(request.Image);
            author.Image = Guid.Parse(imageId);
        }

        author.Name = request.Name;
        author.Details = request.Details;

        try
        {
            await _dbConnection.ExecuteAsync(
                "UPDATE Authors SET Name = @Name, Image = @Image, Details = @Details WHERE Id = @Id",
                author);
        }
        catch (Exception)
        {
            throw new TechnicalException("DP-500", "Technical Error");
        }

        return author.Id.ToString();
    }

    public async Task<bool> DeleteAuthor(DeleteAuthorDto request)
    {
        if (request.Id == Guid.Empty)
        {
            throw new BusinessException("DP-422", "Client Error");
        }

        var author = await _dbConnection.QuerySingleOrDefaultAsync<Author>(
            "SELECT Id, Name, Image, Details FROM Authors WHERE Id = @Id",
            new { Id = request.Id });

        if (author == null)
        {
            throw new TechnicalException("DP-404", "Technical Error");
        }

        if (author.Image != null)
        {
            await _imageService.DeleteImage(new DeleteImageDto { Id = author.Image });
        }

        try
        {
            await _dbConnection.ExecuteAsync(
                "DELETE FROM Authors WHERE Id = @Id",
                new { Id = request.Id });
        }
        catch (Exception)
        {
            throw new TechnicalException("DP-500", "Technical Error");
        }

        return true;
    }

    public async Task<List<AuthorDto>> GetListAuthor(ListAuthorRequestDto request)
    {
        if (request.PageLimit <= 0 || request.PageOffset < 0)
        {
            throw new BusinessException("DP-422", "Client Error");
        }

        string sortField = request.SortField ?? "Id";
        string sortOrder = request.SortOrder ?? "asc";

        var authors = await _dbConnection.QueryAsync<Author>(
            $"SELECT Id, Name, Image, Details FROM Authors ORDER BY {sortField} {sortOrder} OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY",
            new { Offset = request.PageOffset, Limit = request.PageLimit });

        List<AuthorDto> authorDtos = new List<AuthorDto>();

        foreach (var author in authors)
        {
            AuthorDto authorDto = new AuthorDto
            {
                Id = author.Id,
                Name = author.Name,
                Details = author.Details
            };

            if (author.Image != null)
            {
                var image = await _imageService.GetImage(new ImageRequestDto { Id = author.Image });
                authorDto.Image = image;
            }

            authorDtos.Add(authorDto);
        }

        return authorDtos;
    }
}
