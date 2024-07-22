
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectName.Types;

namespace ProjectName.Interfaces
{
    /// <summary>
    /// Interface for managing author-related operations.
    /// </summary>
    public interface IAuthorService
    {
        /// <summary>
        /// Creates a new author.
        /// </summary>
        /// <param name="createAuthorDto">The data transfer object containing the information for the new author.</param>
        /// <returns>A string representing the result of the operation.</returns>
        Task<string> CreateAuthor(CreateAuthorDto createAuthorDto);

        /// <summary>
        /// Retrieves an author based on the provided request data.
        /// </summary>
        /// <param name="authorRequestDto">The data transfer object containing the request information for the author.</param>
        /// <returns>An Author object representing the found author.</returns>
        Task<Author> GetAuthor(AuthorRequestDto authorRequestDto);

        /// <summary>
        /// Updates an existing author.
        /// </summary>
        /// <param name="updateAuthorDto">The data transfer object containing the updated information for the author.</param>
        /// <returns>A string representing the result of the operation.</returns>
        Task<string> UpdateAuthor(UpdateAuthorDto updateAuthorDto);

        /// <summary>
        /// Deletes an author based on the provided request data.
        /// </summary>
        /// <param name="deleteAuthorDto">The data transfer object containing the information for the author to be deleted.</param>
        /// <returns>A boolean indicating whether the operation was successful.</returns>
        Task<bool> DeleteAuthor(DeleteAuthorDto deleteAuthorDto);

        /// <summary>
        /// Retrieves a list of authors based on the provided request data.
        /// </summary>
        /// <param name="listAuthorRequestDto">The data transfer object containing the request information for the list of authors.</param>
        /// <returns>A list of Author objects representing the found authors.</returns>
        Task<List<Author>> GetListAuthor(ListAuthorRequestDto listAuthorRequestDto);
    }
}
