
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectName.Types;

namespace ProjectName.Interfaces
{
    /// <summary>
    /// Interface for managing image-related operations.
    /// </summary>
    public interface IImageService
    {
        /// <summary>
        /// Creates a new image based on the provided data.
        /// </summary>
        /// <param name="createImageDto">Data transfer object containing information needed to create an image.</param>
        /// <returns>A string representing the result of the image creation operation.</returns>
        Task<string> CreateImage(CreateImageDto createImageDto);

        /// <summary>
        /// Retrieves an image based on the provided request data.
        /// </summary>
        /// <param name="imageRequestDto">Data transfer object containing information needed to retrieve an image.</param>
        /// <returns>An Image object representing the retrieved image.</returns>
        Task<Image> GetImage(ImageRequestDto imageRequestDto);

        /// <summary>
        /// Updates an existing image based on the provided data.
        /// </summary>
        /// <param name="updateImageDto">Data transfer object containing information needed to update an image.</param>
        /// <returns>A string representing the result of the image update operation.</returns>
        Task<string> UpdateImage(UpdateImageDto updateImageDto);

        /// <summary>
        /// Deletes an image based on the provided data.
        /// </summary>
        /// <param name="deleteImageDto">Data transfer object containing information needed to delete an image.</param>
        /// <returns>A boolean indicating whether the image was successfully deleted.</returns>
        Task<bool> DeleteImage(DeleteImageDto deleteImageDto);

        /// <summary>
        /// Retrieves a list of images based on the provided request data.
        /// </summary>
        /// <param name="listImageRequestDto">Data transfer object containing information needed to retrieve a list of images.</param>
        /// <returns>A list of Image objects representing the retrieved images.</returns>
        Task<List<Image>> GetListImage(ListImageRequestDto listImageRequestDto);

        /// <summary>
        /// Handles an image operation, potentially involving additional actions.
        /// </summary>
        /// <param name="createImageDto">Data transfer object containing information needed to create an image.</param>
        /// <param name="guid">An optional Guid parameter.</param>
        /// <param name="action">An action to be performed with the Guid.</param>
        Task HandleImage(CreateImageDto createImageDto, Guid? guid, Action<Guid?> action);
    }
}
