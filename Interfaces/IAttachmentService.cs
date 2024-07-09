
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectName.Types;

namespace ProjectName.Interfaces
{
    /// <summary>
    /// Interface for managing attachments.
    /// </summary>
    public interface IAttachmentService
    {
        /// <summary>
        /// Creates a new attachment.
        /// </summary>
        /// <param name="createAttachmentDto">The data transfer object containing information to create the attachment.</param>
        /// <returns>The ID of the created attachment.</returns>
        Task<string> CreateAttachment(CreateAttachmentDto createAttachmentDto);

        /// <summary>
        /// Retrieves an attachment by its request details.
        /// </summary>
        /// <param name="attachmentRequestDto">The data transfer object containing the request details to retrieve the attachment.</param>
        /// <returns>The requested attachment.</returns>
        Task<Attachment> GetAttachment(AttachmentRequestDto attachmentRequestDto);

        /// <summary>
        /// Updates an existing attachment.
        /// </summary>
        /// <param name="updateAttachmentDto">The data transfer object containing information to update the attachment.</param>
        /// <returns>The ID of the updated attachment.</returns>
        Task<string> UpdateAttachment(UpdateAttachmentDto updateAttachmentDto);

        /// <summary>
        /// Deletes an attachment.
        /// </summary>
        /// <param name="deleteAttachmentDto">The data transfer object containing information to delete the attachment.</param>
        /// <returns>True if the attachment was deleted successfully, otherwise false.</returns>
        Task<bool> DeleteAttachment(DeleteAttachmentDto deleteAttachmentDto);

        /// <summary>
        /// Retrieves a list of attachments based on the request details.
        /// </summary>
        /// <param name="listAttachmentRequestDto">The data transfer object containing the request details to retrieve the list of attachments.</param>
        /// <returns>A list of attachments.</returns>
        Task<List<Attachment>> GetListAttachment(ListAttachmentRequestDto listAttachmentRequestDto);
    }
}
