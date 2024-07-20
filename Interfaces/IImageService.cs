
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
        /// <returns>A string representing the identifier of the newly created image.</returns>
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
        /// <returns>A string representing the identifier of the updated image.</returns>
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
    }
}
