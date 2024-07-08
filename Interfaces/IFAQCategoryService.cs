
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectName.Types;

namespace ProjectName.Interfaces
{
    /// <summary>
    /// Interface for managing FAQ categories.
    /// </summary>
    public interface IFAQCategoryService
    {
        /// <summary>
        /// Creates a new FAQ category.
        /// </summary>
        /// <param name="createFAQCategoryDto">The data transfer object containing the details of the FAQ category to be created.</param>
        /// <returns>A string representing the identifier of the newly created FAQ category.</returns>
        Task<string> CreateFAQCategory(CreateFAQCategoryDto createFAQCategoryDto);

        /// <summary>
        /// Retrieves a specific FAQ category based on the provided request details.
        /// </summary>
        /// <param name="faqCategoryRequestDto">The data transfer object containing the request details for the FAQ category.</param>
        /// <returns>An FAQCategory object representing the requested FAQ category.</returns>
        Task<FAQCategory> GetFAQCategory(FAQCategoryRequestDto faqCategoryRequestDto);

        /// <summary>
        /// Updates an existing FAQ category.
        /// </summary>
        /// <param name="updateFAQCategoryDto">The data transfer object containing the updated details of the FAQ category.</param>
        /// <returns>A string representing the identifier of the updated FAQ category.</returns>
        Task<string> UpdateFAQCategory(UpdateFAQCategoryDto updateFAQCategoryDto);

        /// <summary>
        /// Deletes a specific FAQ category based on the provided details.
        /// </summary>
        /// <param name="deleteFAQCategoryDto">The data transfer object containing the details of the FAQ category to be deleted.</param>
        /// <returns>A boolean indicating whether the FAQ category was successfully deleted.</returns>
        Task<bool> DeleteFAQCategory(DeleteFAQCategoryDto deleteFAQCategoryDto);

        /// <summary>
        /// Retrieves a list of FAQ categories based on the provided request details.
        /// </summary>
        /// <param name="listFAQCategoryRequestDto">The data transfer object containing the request details for the list of FAQ categories.</param>
        /// <returns>A list of FAQCategory objects representing the requested FAQ categories.</returns>
        Task<List<FAQCategory>> GetListFAQCategory(ListFAQCategoryRequestDto listFAQCategoryRequestDto);
    }
}
