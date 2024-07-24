
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

            const string sql = "INSERT INTO Images (Id, ImageName, ImageData, ImagePath, AltText, Version, Created, CreatorId) VALUES (@Id, @ImageName, @ImageData, @ImagePath, @AltText, @Version, @Created, @CreatorId)";
            var affectedRows = await _dbConnection.ExecuteAsync(sql, image);

            if (affectedRows > 0)
            {
                return image.Id.ToString();
            }
            else
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
            var image = await _dbConnection.QuerySingleOrDefaultAsync<Image>(sql, new { Id = request.Id });

            if (image != null)
            {
                return image;
            }
            else
            {
                throw new TechnicalException("DP-404", "Technical Error");
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

            const string updateSql = "UPDATE Images SET ImageName = @ImageName, ImageData = @ImageData, ImagePath = @ImagePath, AltText = @AltText, Version = @Version, Changed = @Changed, ChangedUser = @ChangedUser WHERE Id = @Id";
            var affectedRows = await _dbConnection.ExecuteAsync(updateSql, existingImage);

            if (affectedRows > 0)
            {
                return existingImage.Id.ToString();
            }
            else
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

            const string sql = "DELETE FROM Images WHERE Id = @Id";
            var affectedRows = await _dbConnection.ExecuteAsync(sql, new { Id = request.Id });

            if (affectedRows > 0)
            {
                return true;
            }
            else
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

            var sql = $"SELECT * FROM Images ORDER BY {sortField} {sortOrder} OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";
            var images = await _dbConnection.QueryAsync<Image>(sql, new { PageOffset = request.PageOffset, PageLimit = request.PageLimit });

            return images.ToList();
        }

        public async Task HandleImage(CreateImageDto newImage, Guid? existingImageId, Action<Guid?> updateImageFieldId)
        {
            if (newImage != null)
            {
                if (existingImageId != null)
                {
                    var existingImage = await GetImage(new ImageRequestDto { Id = existingImageId });
                    if (existingImage.ImagePath != newImage.ImagePath)
                    {
                        await DeleteImage(new DeleteImageDto { Id = existingImageId });
                    }
                }

                var newImageId = Guid.Parse(await CreateImage(newImage));
                updateImageFieldId(newImageId);
            }
            else if (existingImageId != null)
            {
                await DeleteImage(new DeleteImageDto { Id = existingImageId });
                updateImageFieldId(null);
            }
        }

        public async Task<Image> UploadImage(CreateImageDto? newImage)
        {
            if (newImage != null)
            {
                var existingImage = (await GetListImage(new ListImageRequestDto { PageLimit = 1, PageOffset = 0, SortField = "ImagePath", SortOrder = "asc" })).FirstOrDefault(i => i.ImagePath == newImage.ImagePath);

                if (existingImage != null)
                {
                    return existingImage;
                }
                else
                {
                    var newImageId = Guid.Parse(await CreateImage(newImage));
                    return await GetImage(new ImageRequestDto { Id = newImageId });
                }
            }
            else
            {
                return null;
            }
        }
    }
}
