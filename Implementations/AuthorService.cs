
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
            var imageResult = await _imageService.CreateImage(request.Image);
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
        if (request.Id == Guid.Empty)
        {
            throw new BusinessException("DP-422", "Client Error");
        }

        var author = await _dbConnection.QuerySingleOrDefaultAsync<Author>(
            "SELECT * FROM Authors WHERE Id = @Id", new { request.Id });

        if (author == null)
        {
            throw new TechnicalException("DP-404", "Technical Error");
        }

        Image image = null;
        if (author.Image.HasValue)
        {
            image = await _imageService.GetImage(new ImageRequestDto { Id = author.Image.Value });
        }

        return new AuthorDto
        {
            Id = author.Id,
            Name = author.Name,
            Image = image,
            Details = author.Details
        };
    }

    public async Task<string> UpdateAuthor(UpdateAuthorDto request)
    {
        if (request.Id == null || string.IsNullOrEmpty(request.Name))
        {
            throw new BusinessException("DP-422", "Client Error");
        }

        var author = await _dbConnection.QuerySingleOrDefaultAsync<Author>(
            "SELECT * FROM Authors WHERE Id = @Id", new { request.Id });

        if (author == null)
        {
            throw new TechnicalException("DP-404", "Technical Error");
        }

        if (request.Image != null)
        {
            var imageResult = await _imageService.UpdateImage(request.Image);
            author.Image = Guid.Parse(imageResult);
        }

        author.Name = request.Name;
        author.Details = request.Details;

        try
        {
            await _dbConnection.ExecuteAsync(
                "UPDATE Authors SET Name = @Name, Image = @Image, Details = @Details WHERE Id = @Id",
                new { author.Name, author.Image, author.Details, author.Id });
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
            "SELECT * FROM Authors WHERE Id = @Id", new { request.Id });

        if (author == null)
        {
            throw new TechnicalException("DP-404", "Technical Error");
        }

        if (author.Image.HasValue)
        {
            await _imageService.DeleteImage(new DeleteImageDto { Id = author.Image.Value });
        }

        try
        {
            await _dbConnection.ExecuteAsync(
                "DELETE FROM Authors WHERE Id = @Id", new { request.Id });
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

        var authors = await _dbConnection.QueryAsync<Author>(
            "SELECT * FROM Authors ORDER BY @SortField @SortOrder OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY",
            new { request.SortField, request.SortOrder, request.PageOffset, request.PageLimit });

        var authorDtos = new List<AuthorDto>();
        foreach (var author in authors)
        {
            Image image = null;
            if (author.Image.HasValue)
            {
                image = await _imageService.GetImage(new ImageRequestDto { Id = author.Image.Value });
            }

            authorDtos.Add(new AuthorDto
            {
                Id = author.Id,
                Name = author.Name,
                Image = image,
                Details = author.Details
            });
        }

        return authorDtos;
    }
}
