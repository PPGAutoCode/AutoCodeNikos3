
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectName.Types;

namespace ProjectName.Interfaces
{
    /// <summary>
    /// Interface for managing API tags.
    /// </summary>
    public interface IApiTagService
    {
        /// <summary>
        /// Creates a new API tag.
        /// </summary>
        /// <param name="createApiTagDto">The data transfer object containing the information for creating the API tag.</param>
        /// <returns>A string representing the result of the creation operation.</returns>
        Task<string> CreateApiTag(CreateApiTagDto createApiTagDto);

        /// <summary>
        /// Retrieves an API tag based on the provided request data.
        /// </summary>
        /// <param name="apiTagRequestDto">The data transfer object containing the request information for retrieving the API tag.</param>
        /// <returns>An ApiTag object representing the retrieved API tag.</returns>
        Task<ApiTag> GetApiTag(ApiTagRequestDto apiTagRequestDto);

        /// <summary>
        /// Updates an existing API tag.
        /// </summary>
        /// <param name="updateApiTagDto">The data transfer object containing the information for updating the API tag.</param>
        /// <returns>A string representing the result of the update operation.</returns>
        Task<string> UpdateApiTag(UpdateApiTagDto updateApiTagDto);

        /// <summary>
        /// Deletes an API tag based on the provided request data.
        /// </summary>
        /// <param name="deleteApiTagDto">The data transfer object containing the information for deleting the API tag.</param>
        /// <returns>A boolean indicating the success or failure of the deletion operation.</returns>
        Task<bool> DeleteApiTag(DeleteApiTagDto deleteApiTagDto);

        /// <summary>
        /// Retrieves a list of API tags based on the provided request data.
        /// </summary>
        /// <param name="listApiTagRequestDto">The data transfer object containing the request information for retrieving the list of API tags.</param>
        /// <returns>A list of ApiTag objects representing the retrieved API tags.</returns>
        Task<List<ApiTag>> GetListApiTag(ListApiTagRequestDto listApiTagRequestDto);
    }
}
