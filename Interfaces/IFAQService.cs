
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectName.Types;

namespace ProjectName.Interfaces
{
    /// <summary>
    /// Interface for managing FAQ (Frequently Asked Questions) entries.
    /// </summary>
    public interface IFAQService
    {
        /// <summary>
        /// Creates a new FAQ entry.
        /// </summary>
        /// <param name="createFAQDto">The data transfer object containing the information for the new FAQ entry.</param>
        /// <returns>A string representing the unique identifier of the newly created FAQ entry.</returns>
        Task<string> CreateFAQ(CreateFAQDto createFAQDto);

        /// <summary>
        /// Retrieves a specific FAQ entry based on the provided request data.
        /// </summary>
        /// <param name="faqRequestDto">The data transfer object containing the request parameters to retrieve the FAQ entry.</param>
        /// <returns>An FAQ object representing the retrieved FAQ entry.</returns>
        Task<FAQ> GetFAQ(FAQRequestDto faqRequestDto);

        /// <summary>
        /// Updates an existing FAQ entry.
        /// </summary>
        /// <param name="updateFAQDto">The data transfer object containing the updated information for the FAQ entry.</param>
        /// <returns>A string representing the unique identifier of the updated FAQ entry.</returns>
        Task<string> UpdateFAQ(UpdateFAQDto updateFAQDto);

        /// <summary>
        /// Deletes a specific FAQ entry based on the provided request data.
        /// </summary>
        /// <param name="deleteFAQDto">The data transfer object containing the request parameters to delete the FAQ entry.</param>
        /// <returns>A boolean indicating whether the FAQ entry was successfully deleted.</returns>
        Task<bool> DeleteFAQ(DeleteFAQDto deleteFAQDto);

        /// <summary>
        /// Retrieves a list of FAQ entries based on the provided request data.
        /// </summary>
        /// <param name="listFAQRequestDto">The data transfer object containing the request parameters to retrieve the list of FAQ entries.</param>
        /// <returns>A list of FAQ objects representing the retrieved FAQ entries.</returns>
        Task<List<FAQ>> GetListFAQ(ListFAQRequestDto listFAQRequestDto);
    }
}
