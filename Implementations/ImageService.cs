
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
    public class ImageService : IImageService
    {
        private readonly IDbConnection _dbConnection;

        public ImageService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<string> CreateImage(CreateImageDto request)
        {
            if (string.IsNullOrEmpty(request.ImageName) || request.ImageData == null || string.IsNullOrEmpty(request.ImagePath))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            var image = new Image
            {
                Id = Guid.NewGuid(),
                ImageName = request.ImageName + "_original",
                ImageData = request.ImageData,
                ImagePath = request.ImagePath,
                AltText = request.AltText,
                Version = 1,
                Created = DateTime.Now,
                CreatorId = request.CreatorId
            };

            const string sql = @"
                INSERT INTO Images (Id, ImageName, ImageData, ImagePath, AltText, Version, Created, CreatorId)
                VALUES (@Id, @ImageName, @ImageData, @ImagePath, @AltText, @Version, @Created, @CreatorId)";

            try
            {
                await _dbConnection.ExecuteAsync(sql, image);
                return image.Id.ToString();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
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
                var image = await _dbConnection.QuerySingleOrDefaultAsync<Image>(sql, new { Id = request.Id });
                if (image == null)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
                return image;
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<string> UpdateImage(UpdateImageDto request)
        {
            if (request.Id == null || string.IsNullOrEmpty(request.ImageName) || request.ImageData == null || string.IsNullOrEmpty(request.ImagePath))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            const string selectSql = "SELECT * FROM Images WHERE Id = @Id";
            var existingImage = await _dbConnection.QuerySingleOrDefaultAsync<Image>(selectSql, new { Id = request.Id });

            if (existingImage == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            existingImage.ImageName = request.ImageName;
            existingImage.ImageData = request.ImageData;
            existingImage.ImagePath = request.ImagePath;
            existingImage.AltText = request.AltText;
            existingImage.Version += 1;
            existingImage.Changed = DateTime.Now;
            existingImage.ChangedUser = request.ChangedUser;

            const string updateSql = @"
                UPDATE Images 
                SET ImageName = @ImageName, ImageData = @ImageData, ImagePath = @ImagePath, AltText = @AltText, 
                    Version = @Version, Changed = @Changed, ChangedUser = @ChangedUser 
                WHERE Id = @Id";

            try
            {
                await _dbConnection.ExecuteAsync(updateSql, existingImage);
                return existingImage.Id.ToString();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<bool> DeleteImage(DeleteImageDto request)
        {
            if (request.Id == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            const string selectSql = "SELECT * FROM Images WHERE Id = @Id";
            var existingImage = await _dbConnection.QuerySingleOrDefaultAsync<Image>(selectSql, new { Id = request.Id });

            if (existingImage == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            const string deleteSql = "DELETE FROM Images WHERE Id = @Id";

            try
            {
                await _dbConnection.ExecuteAsync(deleteSql, new { Id = request.Id });
                return true;
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
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

            var sql = $@"
                SELECT * FROM Images 
                ORDER BY {sortField} {sortOrder} 
                OFFSET {request.PageOffset} ROWS 
                FETCH NEXT {request.PageLimit} ROWS ONLY";

            try
            {
                var images = await _dbConnection.QueryAsync<Image>(sql);
                return images.ToList();
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task HandleImage(CreateImageDto newImage, Guid? existingImageId, Action<Guid?> updateImageFieldId)
        {
            if (newImage != null)
            {
                if (existingImageId != null)
                {
                    var existingImage = await GetImage(new ImageRequestDto { Id = existingImageId });
                    if (existingImage != null && existingImage.ImagePath != newImage.ImagePath)
                    {
                        await DeleteImage(new DeleteImageDto { Id = existingImageId });
                    }
                }

                var newImageId = await CreateImage(newImage);
                updateImageFieldId(Guid.Parse(newImageId));
            }
            else
            {
                if (existingImageId != null)
                {
                    await DeleteImage(new DeleteImageDto { Id = existingImageId });
                    updateImageFieldId(null);
                }
            }
        }
    }
}
