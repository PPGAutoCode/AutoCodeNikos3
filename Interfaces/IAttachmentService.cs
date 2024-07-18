using System;
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
        /// <param name="createAttachmentDto">Data transfer object for creating an attachment.</param>
        /// <returns>The ID of the created attachment.</returns>
        Task<string> CreateAttachment(CreateAttachmentDto createAttachmentDto);

        /// <summary>
        /// Retrieves an attachment by its request details.
        /// </summary>
        /// <param name="attachmentRequestDto">Data transfer object for requesting an attachment.</param>
        /// <returns>The requested attachment.</returns>
        Task<Attachment> GetAttachment(AttachmentRequestDto attachmentRequestDto);

        /// <summary>
        /// Updates an existing attachment.
        /// </summary>
        /// <param name="updateAttachmentDto">Data transfer object for updating an attachment.</param>
        /// <returns>The ID of the updated attachment.</returns>
        Task<string> UpdateAttachment(UpdateAttachmentDto updateAttachmentDto);

        /// <summary>
        /// Deletes an attachment.
        /// </summary>
        /// <param name="deleteAttachmentDto">Data transfer object for deleting an attachment.</param>
        /// <returns>True if the attachment was deleted successfully, otherwise false.</returns>
        Task<bool> DeleteAttachment(DeleteAttachmentDto deleteAttachmentDto);

        /// <summary>
        /// Retrieves a list of attachments based on the request details.
        /// </summary>
        /// <param name="listAttachmentRequestDto">Data transfer object for requesting a list of attachments.</param>
        /// <returns>A list of attachments.</returns>
        Task<List<Attachment>> GetListAttachment(ListAttachmentRequestDto listAttachmentRequestDto);

        /// <summary>
        /// Handles an attachment, optionally associating it with a GUID and performing an action.
        /// </summary>
        /// <param name="createAttachmentDto">Data transfer object for creating an attachment.</param>
        /// <param name="guid">Optional GUID to associate with the attachment.</param>
        /// <param name="action">Action to perform with the GUID.</param>
        Task HandleAttachment(CreateAttachmentDto createAttachmentDto, Guid? guid, Action<Guid?> action);
    }
}
