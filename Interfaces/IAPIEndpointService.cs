
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectName.Types;

namespace ProjectName.Interfaces
{
    /// <summary>
    /// Interface for managing API endpoints.
    /// </summary>
    public interface IAPIEndpointService
    {
        /// <summary>
        /// Creates a new API endpoint.
        /// </summary>
        /// <param name="createAPIEndpointDto">Data transfer object for creating an API endpoint.</param>
        /// <returns>The identifier of the created API endpoint.</returns>
        Task<string> CreateAPIEndpoint(CreateAPIEndpointDto createAPIEndpointDto);

        /// <summary>
        /// Retrieves an API endpoint by its request details.
        /// </summary>
        /// <param name="apiEndpointRequestDto">Data transfer object for requesting an API endpoint.</param>
        /// <returns>The requested API endpoint.</returns>
       Task<APIEndpoint> GetAPIEndpoint(APIEndpointRequestDto apiEndpointRequestDto);

        /// <summary>
        /// Updates an existing API endpoint.
        /// </summary>
        /// <param name="updateAPIEndpointDto">Data transfer object for updating an API endpoint.</param>
        /// <returns>The identifier of the updated API endpoint.</returns>
        Task<string> UpdateAPIEndpoint(UpdateAPIEndpointDto updateAPIEndpointDto);

        /// <summary>
        /// Deletes an API endpoint.
        /// </summary>
        /// <param name="deleteAPIEndpointDto">Data transfer object for deleting an API endpoint.</param>
        /// <returns>True if the API endpoint was deleted successfully, otherwise false.</returns>
        Task<bool> DeleteAPIEndpoint(DeleteAPIEndpointDto deleteAPIEndpointDto);

        /// <summary>
        /// Retrieves a list of API endpoints based on the request details.
        /// </summary>
        /// <param name="listAPIEndpointRequestDto">Data transfer object for requesting a list of API endpoints.</param>
        /// <returns>A list of API endpoints.</returns>
        Task<List<APIEndpoint>> GetListAPIEndpoint(ListAPIEndpointRequestDto listAPIEndpointRequestDto);
    }
}
