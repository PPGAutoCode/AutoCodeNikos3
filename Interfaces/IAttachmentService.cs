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
        /// <param name="createAttachmentDto">The data transfer object for creating an attachment.</param>
        /// <returns>The ID of the created attachment.</returns>
        Task<string> CreateAttachment(CreateAttachmentDto createAttachmentDto);

        /// <summary>
        /// Retrieves an attachment by its request data.
        /// </summary>
        /// <param name="attachmentRequestDto">The data transfer object for requesting an attachment.</param>
        /// <returns>The requested attachment.</returns>
        Task<Attachment> GetAttachment(AttachmentRequestDto attachmentRequestDto);

        /// <summary>
        /// Updates an existing attachment.
        /// </summary>
        /// <param name="updateAttachmentDto">The data transfer object for updating an attachment.</param>
        /// <returns>The ID of the updated attachment.</returns>
        Task<string> UpdateAttachment(UpdateAttachmentDto updateAttachmentDto);

        /// <summary>
        /// Deletes an attachment.
        /// </summary>
        /// <param name="deleteAttachmentDto">The data transfer object for deleting an attachment.</param>
        /// <returns>True if the attachment was deleted, false otherwise.</returns>
        Task<bool> DeleteAttachment(DeleteAttachmentDto deleteAttachmentDto);

        /// <summary>
        /// Retrieves a list of attachments based on the request data.
        /// </summary>
        /// <param name="listAttachmentRequestDto">The data transfer object for requesting a list of attachments.</param>
        /// <returns>A list of attachments.</returns>
        Task<List<Attachment>> GetListAttachment(ListAttachmentRequestDto listAttachmentRequestDto);

        /// <summary>
        /// Handles an attachment, optionally associating it with a GUID and performing an action.
        /// </summary>
        /// <param name="createAttachmentDto">The data transfer object for creating an attachment.</param>
        /// <param name="guid">An optional GUID to associate with the attachment.</param>
        /// <param name="action">An action to perform with the GUID.</param>
        Task HandleAttachement(CreateAttachmentDto createAttachmentDto, Guid? guid, Action<Guid?> action);
    }
}
