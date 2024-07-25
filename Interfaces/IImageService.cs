
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
        /// Creates a new image based on the provided data transfer object.
        /// </summary>
        /// <param name="createImageDto">Data transfer object containing information for creating a new image.</param>
        /// <returns>A string representing the result of the image creation operation.</returns>
        Task<string> CreateImage(CreateImageDto createImageDto);

        /// <summary>
        /// Retrieves an image based on the provided request data transfer object.
        /// </summary>
        /// <param name="imageRequestDto">Data transfer object containing request information for retrieving an image.</param>
        /// <returns>An Image object representing the retrieved image.</returns>
        Task<Image> GetImage(ImageRequestDto imageRequestDto);

        /// <summary>
        /// Updates an existing image based on the provided data transfer object.
        /// </summary>
        /// <param name="updateImageDto">Data transfer object containing information for updating an image.</param>
        /// <returns>A string representing the result of the image update operation.</returns>
        Task<string> UpdateImage(UpdateImageDto updateImageDto);

        /// <summary>
        /// Deletes an image based on the provided data transfer object.
        /// </summary>
        /// <param name="deleteImageDto">Data transfer object containing information for deleting an image.</param>
        /// <returns>A boolean indicating the success or failure of the image deletion operation.</returns>
        Task<bool> DeleteImage(DeleteImageDto deleteImageDto);

        /// <summary>
        /// Retrieves a list of images based on the provided request data transfer object.
        /// </summary>
        /// <param name="listImageRequestDto">Data transfer object containing request information for retrieving a list of images.</param>
        /// <returns>A list of Image objects representing the retrieved images.</returns>
        Task<List<Image>> GetListImage(ListImageRequestDto listImageRequestDto);

        /// <summary>
        /// Inserts or updates an image based on the provided data transfer object.
        /// </summary>
        /// <param name="updateImageDto">Data transfer object containing information for upserting an image.</param>
        /// <returns>A string representing the result of the image upsert operation.</returns>
        Task<string> UpsertImage(UpdateImageDto updateImageDto);
    }
}
