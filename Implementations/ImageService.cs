
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
            // Step 1: Validate the request payload
            if (string.IsNullOrEmpty(request.ImageName) || request.ImageData == null || string.IsNullOrEmpty(request.ImagePath))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Modify the file name
            string modifiedFileName = request.ImageName + "_original";

            // Step 3: Create an Image object
            var image = new Image
            {
                Id = Guid.NewGuid(),
                ImageName = modifiedFileName,
                ImageData = request.ImageData,
                ImagePath = request.ImagePath,
                AltText = request.AltText,
                Version = 1,
                Created = DateTime.Now,
                CreatorId = request.CreatorId
            };

            // Step 4: Insert the newly created Image object to the database
            const string sql = "INSERT INTO Images (Id, ImageName, ImageData, ImagePath, AltText, Version, Created, CreatorId) VALUES (@Id, @ImageName, @ImageData, @ImagePath, @AltText, @Version, @Created, @CreatorId)";
            int rowsAffected = await _dbConnection.ExecuteAsync(sql, image);

            if (rowsAffected > 0)
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
            // Step 1: Validate that ImageRequestDto.Id is not null
            if (request.Id == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch the Image from the database based on the provided Image ID
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
            // Step 1: Validate that the request payload contains the necessary parameters
            if (request.Id == null || string.IsNullOrEmpty(request.ImageName) || request.ImageData == null || string.IsNullOrEmpty(request.ImagePath))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch the Image from the database by Id
            var existingImage = await GetImage(new ImageRequestDto { Id = request.Id });
            if (existingImage == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Update the Image object with the provided changes
            existingImage.ImageName = request.ImageName;
            existingImage.ImageData = request.ImageData;
            existingImage.ImagePath = request.ImagePath;
            existingImage.AltText = request.AltText;
            existingImage.Version += 1;
            existingImage.Changed = DateTime.Now;
            existingImage.ChangedUser = request.ChangedUser;

            // Step 4: Insert the updated Image object to the database
            const string updateSql = "UPDATE Images SET ImageName = @ImageName, ImageData = @ImageData, ImagePath = @ImagePath, AltText = @AltText, Version = @Version, Changed = @Changed, ChangedUser = @ChangedUser WHERE Id = @Id";
            int rowsAffected = await _dbConnection.ExecuteAsync(updateSql, existingImage);

            if (rowsAffected > 0)
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
            // Step 1: Validate that the request payload contains the necessary parameter
            if (request.Id == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch the Image from the database by Id
            var existingImage = await GetImage(new ImageRequestDto { Id = request.Id });
            if (existingImage == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Delete the Image object from the database
            const string deleteSql = "DELETE FROM Images WHERE Id = @Id";
            int rowsAffected = await _dbConnection.ExecuteAsync(deleteSql, new { Id = request.Id });

            if (rowsAffected > 0)
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
            // Step 1: Validate that the ListImageRequestDto contains the necessary pagination parameters
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Set default sorting values if not provided
            string sortField = string.IsNullOrEmpty(request.SortField) ? "Id" : request.SortField;
            string sortOrder = string.IsNullOrEmpty(request.SortOrder) ? "asc" : request.SortOrder;

            // Step 3: Fetch the list of Images from the database table Images based on the provided pagination parameters and optional sorting
            string sql = $"SELECT * FROM Images ORDER BY {sortField} {sortOrder} OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY";
            var images = await _dbConnection.QueryAsync<Image>(sql, new { Offset = request.PageOffset, Limit = request.PageLimit });

            return images.ToList();
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
