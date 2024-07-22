
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
        /// <param name="createImageDto">Data transfer object containing the image creation details.</param>
        /// <returns>A string representing the result of the image creation operation.</returns>
        Task<string> CreateImage(CreateImageDto createImageDto);

        /// <summary>
        /// Retrieves an image based on the provided request data.
        /// </summary>
        /// <param name="imageRequestDto">Data transfer object containing the image request details.</param>
        /// <returns>An Image object representing the retrieved image.</returns>
        Task<Image> GetImage(ImageRequestDto imageRequestDto);

        /// <summary>
        /// Updates an existing image based on the provided data.
        /// </summary>
        /// <param name="updateImageDto">Data transfer object containing the image update details.</param>
        /// <returns>A string representing the result of the image update operation.</returns>
        Task<string> UpdateImage(UpdateImageDto updateImageDto);

        /// <summary>
        /// Deletes an image based on the provided data.
        /// </summary>
        /// <param name="deleteImageDto">Data transfer object containing the image deletion details.</param>
        /// <returns>A boolean indicating the success of the image deletion operation.</returns>
        Task<bool> DeleteImage(DeleteImageDto deleteImageDto);

        /// <summary>
        /// Retrieves a list of images based on the provided request data.
        /// </summary>
        /// <param name="listImageRequestDto">Data transfer object containing the list image request details.</param>
        /// <returns>A list of Image objects representing the retrieved images.</returns>
        Task<List<Image>> GetListImage(ListImageRequestDto listImageRequestDto);

        /// <summary>
        /// Handles an image operation, potentially involving additional actions.
        /// </summary>
        /// <param name="createImageDto">Data transfer object containing the image creation details.</param>
        /// <param name="guid">An optional Guid parameter.</param>
        /// <param name="action">An action to be performed with the Guid.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HandleImage(CreateImageDto createImageDto, Guid? guid, Action<Guid?> action);
    }
}
