
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
        /// Creates a new author based on the provided data.
        /// </summary>
        /// <param name="createAuthorDto">Data transfer object containing information for creating a new author.</param>
        /// <returns>A string indicating the result of the operation.</returns>
        Task<string> CreateAuthor(CreateAuthorDto createAuthorDto);

        /// <summary>
        /// Retrieves an author based on the provided request data.
        /// </summary>
        /// <param name="authorRequestDto">Data transfer object containing request information for retrieving an author.</param>
        /// <returns>An AuthorDto object representing the retrieved author.</returns>
        Task<AuthorDto> GetAuthor(AuthorRequestDto authorRequestDto);

        /// <summary>
        /// Updates an existing author based on the provided data.
        /// </summary>
        /// <param name="updateAuthorDto">Data transfer object containing information for updating an author.</param>
        /// <returns>A string indicating the result of the operation.</returns>
        Task<string> UpdateAuthor(UpdateAuthorDto updateAuthorDto);

        /// <summary>
        /// Deletes an author based on the provided data.
        /// </summary>
        /// <param name="deleteAuthorDto">Data transfer object containing information for deleting an author.</param>
        /// <returns>A boolean indicating whether the operation was successful.</returns>
        Task<bool> DeleteAuthor(DeleteAuthorDto deleteAuthorDto);

        /// <summary>
        /// Retrieves a list of authors based on the provided request data.
        /// </summary>
        /// <param name="listAuthorRequestDto">Data transfer object containing request information for retrieving a list of authors.</param>
        /// <returns>A list of AuthorDto objects representing the retrieved authors.</returns>
        Task<List<AuthorDto>> GetListAuthor(ListAuthorRequestDto listAuthorRequestDto);
    }
}
