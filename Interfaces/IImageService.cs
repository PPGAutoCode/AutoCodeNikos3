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
        /// <param name="createImageDto">Data transfer object containing information for creating a new image.</param>
        /// <returns>A string representing the result of the image creation operation.</returns>
        Task<string> CreateImage(CreateImageDto createImageDto);

        /// <summary>
        /// Retrieves an image based on the provided request data.
        /// </summary>
        /// <param name="imageRequestDto">Data transfer object containing information for requesting an image.</param>
        /// <returns>An Image object representing the requested image.</returns>
        Task<Image> GetImage(ImageRequestDto imageRequestDto);

        /// <summary>
        /// Updates an existing image based on the provided data.
        /// </summary>
        /// <param name="updateImageDto">Data transfer object containing information for updating an image.</param>
        /// <returns>A string representing the result of the image update operation.</returns>
        Task<string> UpdateImage(UpdateImageDto updateImageDto);

        /// <summary>
        /// Deletes an image based on the provided data.
        /// </summary>
        /// <param name="deleteImageDto">Data transfer object containing information for deleting an image.</param>
        /// <returns>A boolean indicating whether the image was successfully deleted.</returns>
        Task<bool> DeleteImage(DeleteImageDto deleteImageDto);

        /// <summary>
        /// Retrieves a list of images based on the provided request data.
        /// </summary>
        /// <param name="listImageRequestDto">Data transfer object containing information for requesting a list of images.</param>
        /// <returns>A list of Image objects representing the requested images.</returns>
        Task<List<Image>> GetListImage(ListImageRequestDto listImageRequestDto);

        /// <summary>
        /// Handles an image operation, which can involve creating a new image or updating an existing one.
        /// </summary>
        /// <param name="newImage">Data transfer object containing information for creating a new image.</param>
        /// <param name="existingImageId">Optional GUID for an existing image to be updated.</param>
        /// <param name="updateImageFieldId">Action to update the image field ID.</param>
        Task HandleImage(CreateImageDto newImage, Guid? existingImageId, Action<Guid?> updateImageFieldId);

        /// <summary>
        /// Uploads an image based on the provided data.
        /// </summary>
        /// <param name="newAttachment">Optional data transfer object containing information for creating a new image attachment.</param>
        /// <returns>A string representing the result of the image upload operation.</returns>
        Task<Image> UploadImage(CreateImageDto? newAttachment);
    }
}
