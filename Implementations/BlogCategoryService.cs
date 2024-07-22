
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
    public class BlogCategoryService : IBlogCategoryService
    {
        private readonly IDbConnection _dbConnection;

        public BlogCategoryService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<string> CreateBlogCategory(CreateBlogCategoryDto request)
        {
            // Step 1: Validate the request payload contains the necessary parameter ("Name").
            if (string.IsNullOrEmpty(request.Name))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Validate that the provided parent category ID exists if it's included in the request payload.
            if (request.Parent.HasValue)
            {
                var parentExists = await _dbConnection.ExecuteScalarAsync<bool>(
                    "SELECT COUNT(1) FROM BlogCategories WHERE Id = @Parent",
                    new { Parent = request.Parent.Value });

                if (!parentExists)
                {
                    throw new TechnicalException("DP-404", "Technical Error");
                }
            }

            // Step 3: Create a new blogCategory object with the provided details.
            var blogCategory = new BlogCategory
            {
                Id = Guid.NewGuid(),
                Parent = request.Parent,
                Name = request.Name
            };

            // Step 4: Save the newly created BlogCategory object to the database BlogCategories table.
            try
            {
                await _dbConnection.ExecuteAsync(
                    "INSERT INTO BlogCategories (Id, Parent, Name) VALUES (@Id, @Parent, @Name)",
                    blogCategory);

                return blogCategory.Id.ToString();
            }
            catch
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<BlogCategory> GetBlogCategory(BlogCategoryRequestDto request)
        {
            // Step 1: Validate that BlogCategoryRequestDto.Id is not null.
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch the BlogCategory from the database based on the provided BlogCategory ID.
            var blogCategory = await _dbConnection.QuerySingleOrDefaultAsync<BlogCategory>(
                "SELECT * FROM BlogCategories WHERE Id = @Id",
                new { Id = request.Id });

            if (blogCategory == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            return blogCategory;
        }

        public async Task<string> UpdateBlogCategory(UpdateBlogCategoryDto request)
        {
            // Step 1: Validate that the request payload contains the necessary parameters ("Id" and "Name").
            if (request.Id == Guid.Empty || string.IsNullOrEmpty(request.Name))
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch the BlogCategory from the database by Id.
            var blogCategory = await _dbConnection.QuerySingleOrDefaultAsync<BlogCategory>(
                "SELECT * FROM BlogCategories WHERE Id = @Id",
                new { Id = request.Id });

            if (blogCategory == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: If the BlogCategory is a subcategory, set Parent to the parent BlogCategory ID provided.
            blogCategory.Parent = request.Parent ?? blogCategory.Parent;
            blogCategory.Name = request.Name;

            // Step 4: Save the updated BlogCategory object to the database.
            try
            {
                await _dbConnection.ExecuteAsync(
                    "UPDATE BlogCategories SET Parent = @Parent, Name = @Name WHERE Id = @Id",
                    new { Id = blogCategory.Id, Parent = blogCategory.Parent, Name = blogCategory.Name });

                return blogCategory.Id.ToString();
            }
            catch
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<bool> DeleteBlogCategory(DeleteBlogCategoryDto request)
        {
            // Step 1: Validate that the request payload contains the necessary parameter ("Id").
            if (request.Id == Guid.Empty)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch the BlogCategory from the database by Id.
            var blogCategory = await _dbConnection.QuerySingleOrDefaultAsync<BlogCategory>(
                "SELECT * FROM BlogCategories WHERE Id = @Id",
                new { Id = request.Id });

            if (blogCategory == null)
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 3: Delete the BlogCategory object from the database.
            try
            {
                await _dbConnection.ExecuteAsync(
                    "DELETE FROM BlogCategories WHERE Id = @Id",
                    new { Id = request.Id });

                return true;
            }
            catch
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }

        public async Task<List<BlogCategory>> GetListBlogCategory(ListBlogCategoryRequestDto request)
        {
            // Step 1: Validate that the ListBlogCategoryRequestDto contains the necessary pagination parameters (PageLimit and PageOffset).
            if (request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Check if ListBlogCategoryRequestDto.SortField and ListBlogCategoryRequestDto.SortOrder are null or empty, set default values.
            var sortField = string.IsNullOrEmpty(request.SortField) ? "Id" : request.SortField;
            var sortOrder = string.IsNullOrEmpty(request.SortOrder) ? "asc" : request.SortOrder;

            // Step 3: Fetch the list of BlogCategories from the database table BlogCategories based on the provided pagination parameters and optional sorting.
            try
            {
                var blogCategories = await _dbConnection.QueryAsync<BlogCategory>(
                    $"SELECT * FROM BlogCategories ORDER BY {sortField} {sortOrder} OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY",
                    new { Offset = request.PageOffset, Limit = request.PageLimit });

                return blogCategories.ToList();
            }
            catch
            {
                throw new TechnicalException("DP-500", "Technical Error");
            }
        }
    }
}
