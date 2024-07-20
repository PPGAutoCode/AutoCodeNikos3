
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
            if (request.FileName == null || request.Image == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Modify the file name
            string modifiedFileName = request.FileName + "_original";

            // Step 3: Create an Image object
            var image = new Image
            {
                Id = Guid.NewGuid(),
                FileName = modifiedFileName,
                ImageData = request.Image,
                AltText = request.AltText,
                Version = 1, // Assuming initial version is 1
                Created = DateTime.UtcNow,
                Changed = DateTime.UtcNow,
                CreatorId = request.CreatorId,
                ChangedUser = request.ChangedUser
            };

            // Step 4: Insert the newly created Image object to the database
            const string sql = @"
                INSERT INTO Images (Id, FileName, ImageData, AltText, Version, Created, Changed, CreatorId, ChangedUser)
                VALUES (@Id, @FileName, @ImageData, @AltText, @Version, @Created, @Changed, @CreatorId, @ChangedUser)";

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
            // Step 1: Validate that request.payload.Id is not null
            if (request.Id == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch the Image from the database based on the provided Image ID
            const string sql = "SELECT * FROM Images WHERE Id = @Id";

            try
            {
                var image = await _dbConnection.QuerySingleOrDefaultAsync<Image>(sql, new { request.Id });
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
            // Step 1: Validate that the request payload contains the necessary parameters
            if (request.Id == null || request.FileName == null || request.Image == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch the Image from the database by Id
            const string fetchSql = "SELECT * FROM Images WHERE Id = @Id";
            var existingImage = await _dbConnection.QuerySingleOrDefaultAsync<Image>(fetchSql, new { request.Id });

            if (existingImage == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Modify the file name
            string modifiedFileName = request.FileName + "_original";

            // Step 4: Update the Image object with the provided changes
            existingImage.FileName = modifiedFileName;
            existingImage.ImageData = request.Image;
            existingImage.AltText = request.AltText;
            existingImage.Changed = DateTime.UtcNow;

            // Step 5: Insert the updated Image object to the database
            const string updateSql = @"
                UPDATE Images 
                SET FileName = @FileName, ImageData = @ImageData, AltText = @AltText, Changed = @Changed 
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
            // Step 1: Validate that the request payload contains the necessary parameter
            if (request.Id == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch the Image from the database by Id
            const string fetchSql = "SELECT * FROM Images WHERE Id = @Id";
            var existingImage = await _dbConnection.QuerySingleOrDefaultAsync<Image>(fetchSql, new { request.Id });

            if (existingImage == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Delete the Image object from the database
            const string deleteSql = "DELETE FROM Images WHERE Id = @Id";

            try
            {
                await _dbConnection.ExecuteAsync(deleteSql, new { request.Id });
                return true;
            }
            catch (Exception)
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<List<Image>> GetListImage(ListImageRequestDto request)
        {
            // Step 1: Validate that the request payload contains the necessary parameters
            if (request.PageLimit == null || request.PageOffset == null)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch the list of Images from the database based on the provided pagination parameters
            var sql = "SELECT * FROM Images";

            if (!string.IsNullOrEmpty(request.SortField) && !string.IsNullOrEmpty(request.SortOrder))
            {
                sql += $" ORDER BY {request.SortField} {request.SortOrder}";
            }

            sql += " OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY";

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
    }
}
