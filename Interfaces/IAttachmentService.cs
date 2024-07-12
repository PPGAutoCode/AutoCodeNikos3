
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectName.Types;

namespace ProjectName.Interfaces
{
    /// Interface for managing attachments.
    public interface IAttachmentService
    {
        /// Creates a new attachment
        /// <param name="createAttachmentDto">The data transfer object containing information to create the attachment.</param>
        /// <returns>The ID of the created attachment.</returns>
        Task<string> CreateAttachment(CreateAttachmentDto createAttachmentDto);
        
        /// Retrieves an attachment by its request details.
        /// <param name="attachmentRequestDto">The data transfer object containing the request details to retrieve the attachment.</param>
        /// <returns>The requested attachment.</returns>
        Task<Attachment> GetAttachment(AttachmentRequestDto attachmentRequestDto);
        
        /// Updates an existing attachment.
        /// <param name="updateAttachmentDto">The data transfer object containing information to update the attachment.</param>
        /// <returns>The ID of the updated attachment.</returns>
        Task<string> UpdateAttachment(UpdateAttachmentDto updateAttachmentDto);
        
        /// Deletes an attachment.
        /// <param name="deleteAttachmentDto">The data transfer object containing information to delete the attachment.</param>
        /// <returns>True if the attachment was deleted successfully, otherwise false.</returns>
        Task<bool> DeleteAttachment(DeleteAttachmentDto deleteAttachmentDto);
        
        /// Retrieves a list of attachments based on the request details.
        /// <param name="listAttachmentRequestDto">The data transfer object containing the request details to retrieve the list of attachments.</param>
        /// <returns>A list of attachments.</returns>
        Task<List<Attachment>> GetListAttachment(ListAttachmentRequestDto listAttachmentRequestDto);
    }
}
