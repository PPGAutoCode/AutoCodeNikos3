
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectName.Types;

namespace ProjectName.Interfaces
{
    /// <summary>
    /// Interface for managing application environments.
    /// </summary>
    public interface IAppEnvironmentService
    {
        /// <summary>
        /// Creates a new application environment.
        /// </summary>
        /// <param name="createAppEnvironmentDto">Data transfer object for creating an application environment.</param>
        /// <returns>A string representing the result of the creation operation.</returns>
        Task<string> CreateAppEnvironment(CreateAppEnvironmentDto createAppEnvironmentDto);

        /// <summary>
        /// Retrieves an application environment based on the provided request data.
        /// </summary>
        /// <param name="appEnvironmentRequestDto">Data transfer object for requesting an application environment.</param>
        /// <returns>An AppEnvironment object representing the requested environment.</returns>
        Task<AppEnvironment> GetAppEnvironment(AppEnvironmentRequestDto appEnvironmentRequestDto);

        /// <summary>
        /// Updates an existing application environment.
        /// </summary>
        /// <param name="updateAppEnvironmentDto">Data transfer object for updating an application environment.</param>
        /// <returns>A string representing the result of the update operation.</returns>
        Task<string> UpdateAppEnvironment(UpdateAppEnvironmentDto updateAppEnvironmentDto);

        /// <summary>
        /// Deletes an application environment based on the provided request data.
        /// </summary>
        /// <param name="deleteAppEnvironmentDto">Data transfer object for deleting an application environment.</param>
        /// <returns>A boolean indicating the success of the deletion operation.</returns>
        Task<bool> DeleteAppEnvironment(DeleteAppEnvironmentDto deleteAppEnvironmentDto);

        /// <summary>
        /// Retrieves a list of application environments based on the provided request data.
        /// </summary>
        /// <param name="listAppEnvironmentRequestDto">Data transfer object for requesting a list of application environments.</param>
        /// <returns>A list of AppEnvironment objects representing the requested environments.</returns>
        Task<List<AppEnvironment>> GetListAppEnvironment(ListAppEnvironmentRequestDto listAppEnvironmentRequestDto);
    }
}
