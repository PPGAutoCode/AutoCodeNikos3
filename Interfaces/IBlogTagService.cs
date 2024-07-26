
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectName.Types;

namespace ProjectName.Interfaces
{
    /// <summary>
    /// Interface for managing blog tags.
    /// </summary>
    public interface IBlogTagService
    {
        /// <summary>
        /// Creates a new blog tag.
        /// </summary>
        /// <param name="createBlogTagDto">The data transfer object containing the information for the new blog tag.</param>
        /// <returns>A string representing the result of the creation operation.</returns>
        Task<string> CreateBlogTag(CreateBlogTagDto createBlogTagDto);

        /// <summary>
        /// Retrieves a blog tag based on the provided request data.
        /// </summary>
        /// <param name="blogTagRequestDto">The data transfer object containing the request information for the blog tag.</param>
        /// <returns>A BlogTag object representing the retrieved blog tag.</returns>
        Task<BlogTag> GetBlogTag(BlogTagRequestDto blogTagRequestDto);

        /// <summary>
        /// Updates an existing blog tag.
        /// </summary>
        /// <param name="updateBlogTagDto">The data transfer object containing the updated information for the blog tag.</param>
        /// <returns>A string representing the result of the update operation.</returns>
        Task<string> UpdateBlogTag(UpdateBlogTagDto updateBlogTagDto);

        /// <summary>
        /// Deletes a blog tag based on the provided request data.
        /// </summary>
        /// <param name="deleteBlogTagDto">The data transfer object containing the information for the blog tag to be deleted.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        Task<bool> DeleteBlogTag(DeleteBlogTagDto deleteBlogTagDto);

        /// <summary>
        /// Retrieves a list of blog tags based on the provided request data.
        /// </summary>
        /// <param name="listBlogTagRequestDto">The data transfer object containing the request information for the list of blog tags.</param>
        /// <returns>A list of BlogTag objects representing the retrieved blog tags.</returns>
        Task<List<BlogTag>> GetListBlogTag(ListBlogTagRequestDto listBlogTagRequestDto);
    }
}
