
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectName.Types;

namespace ProjectName.Interfaces
{
    /// <summary>
    /// Interface for managing blog categories.
    /// </summary>
    public interface IBlogCategoryService
    {
        /// <summary>
        /// Creates a new blog category.
        /// </summary>
        /// <param name="createBlogCategoryDto">The data transfer object containing the information for the new blog category.</param>
        /// <returns>A string representing the result of the operation.</returns>
        Task<string> CreateBlogCategory(CreateBlogCategoryDto createBlogCategoryDto);

        /// <summary>
        /// Retrieves a blog category based on the provided request data.
        /// </summary>
        /// <param name="blogCategoryRequestDto">The data transfer object containing the request information for the blog category.</param>
        /// <returns>A BlogCategory object representing the retrieved blog category.</returns>
        Task<BlogCategory> GetBlogCategory(BlogCategoryRequestDto blogCategoryRequestDto);

        /// <summary>
        /// Updates an existing blog category.
        /// </summary>
        /// <param name="updateBlogCategoryDto">The data transfer object containing the updated information for the blog category.</param>
        /// <returns>A string representing the result of the operation.</returns>
        Task<string> UpdateBlogCategory(UpdateBlogCategoryDto updateBlogCategoryDto);

        /// <summary>
        /// Deletes a blog category based on the provided request data.
        /// </summary>
        /// <param name="deleteBlogCategoryDto">The data transfer object containing the information for the blog category to be deleted.</param>
        /// <returns>A boolean indicating whether the operation was successful.</returns>
        Task<bool> DeleteBlogCategory(DeleteBlogCategoryDto deleteBlogCategoryDto);

        /// <summary>
        /// Retrieves a list of blog categories based on the provided request data.
        /// </summary>
        /// <param name="listBlogCategoryRequestDto">The data transfer object containing the request information for the list of blog categories.</param>
        /// <returns>A list of BlogCategory objects representing the retrieved blog categories.</returns>
        Task<List<BlogCategory>> GetListBlogCategory(ListBlogCategoryRequestDto listBlogCategoryRequestDto);
    }
}
