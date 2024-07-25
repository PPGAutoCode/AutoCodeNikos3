
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
    public class ImageService : IImageService
    {
        private readonly IDbConnection _dbConnection;

        public ImageService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<string> CreateImage(CreateImageDto request)
        {
            if (string.IsNullOrEmpty(request.ImageName) || string.IsNullOrEmpty(request.ImageFile))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var image = new Image
            {
                Id = Guid.NewGuid(),
                ImageName = request.ImageName + "_original",
                ImageFile = request.ImageFile,
                AltText = request.AltText,
                Version = 1,
                Created = DateTime.Now,
                CreatorId = request.CreatorId
            };

            const string sql = @"
                INSERT INTO Images (Id, ImageName, ImageFile, AltText, Version, Created, CreatorId)
                VALUES (@Id, @ImageName, @ImageFile, @AltText, @Version, @Created, @CreatorId)";

            try
            {
                await _dbConnection.ExecuteAsync(sql, image);
                return image.Id.ToString();
            }
            catch (Exception ex)
            {
                throw new TechnicalException("DP-500", "Technical Error CreateImage" +ex.Message);
            }
        }

        public async Task<Image> GetImage(ImageRequestDto request)
        {
            if (request.Id == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            const string sql = "SELECT * FROM Images WHERE Id = @Id";

            try
            {
                var image = await _dbConnection.QuerySingleOrDefaultAsync<Image>(sql, new { request.Id });
                if (image == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error Create Image");
                }
                return image;
            }
            catch (Exception ex)
            {
                if (ex is TechnicalException)
                {
                    throw;
                }
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<string> UpdateImage(UpdateImageDto request)
        {
            if (request.Id == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var image = await GetImage(new ImageRequestDto { Id = request.Id });
            if (image == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            if (!string.IsNullOrEmpty(request.ImageName)) image.ImageName = request.ImageName;
            if (!string.IsNullOrEmpty(request.ImageFile)) image.ImageFile = request.ImageFile;
            if (!string.IsNullOrEmpty(request.AltText)) image.AltText = request.AltText;
            image.Version += 1;
            image.Changed = DateTime.Now;
            image.ChangedUser = request.ChangedUser;

            const string sql = @"
                UPDATE Images 
                SET ImageName = @ImageName, ImageFile = @ImageFile, AltText = @AltText, Version = @Version, Changed = @Changed, ChangedUser = @ChangedUser
                WHERE Id = @Id";

            try
            {
                await _dbConnection.ExecuteAsync(sql, image);
                return image.Id.ToString();
            }
            catch (Exception ex)
            {
                throw new TechnicalException("DP-500", "Technical Error UpdateImage" + ex.Message);
            }
        }

        public async Task<bool> DeleteImage(DeleteImageDto request)
        {
            if (request.Id == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var image = await GetImage(new ImageRequestDto { Id = request.Id });
            if (image == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            const string sql = "DELETE FROM Images WHERE Id = @Id";

            try
            {
                await _dbConnection.ExecuteAsync(sql, new { request.Id });
                return true;
            }
            catch (Exception ex)
            {
                throw new TechnicalException("DP-500", "Technical Error DeleteImage"+ ex.Message);
            }
        }

        public async Task<List<Image>> GetListImage(ListImageRequestDto request)
        {
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var sortField = string.IsNullOrEmpty(request.SortField) ? "Id" : request.SortField;
            var sortOrder = string.IsNullOrEmpty(request.SortOrder) ? "asc" : request.SortOrder;

            var sql = $"SELECT * FROM Images ORDER BY {sortField} {sortOrder} OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";

            try
            {
                var images = await _dbConnection.QueryAsync<Image>(sql, new { request.PageOffset, request.PageLimit });
                return images.ToList();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<string> UpsertImage(UpdateImageDto request)
        {
            if (request.Id == null)
            {
                var createImageDto = new CreateImageDto
                {
                    ImageName = request.ImageName,
                    ImageFile = request.ImageFile,
                    AltText = request.AltText,
                    CreatorId = request.ChangedUser
                };
                return await CreateImage(createImageDto);
            }
            else if (!string.IsNullOrEmpty(request.ImageName) || !string.IsNullOrEmpty(request.ImageFile))
            {
                return await UpdateImage(request);
            }
            else
            {
                await DeleteImage(new DeleteImageDto { Id = request.Id });
                return request.Id.ToString();
            }
        }
    }
}
