
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectName.Types;

namespace ProjectName.Interfaces
{
    /// <summary>
    /// Interface for managing attachment-related operations.
    /// </summary>
    public interface IAttachmentService
    {
        /// <summary>
        /// Creates a new attachment.
        /// </summary>
        /// <param name="createAttachmentDto">Data transfer object for creating an attachment.</param>
        /// <returns>A string representing the result of the creation operation.</returns>
        Task<string> CreateAttachment(CreateAttachmentDto createAttachmentDto);

        /// <summary>
        /// Retrieves an attachment based on the provided request data.
        /// </summary>
        /// <param name="attachmentRequestDto">Data transfer object for requesting an attachment.</param>
        /// <returns>An Attachment object.</returns>
        Task<Attachment> GetAttachment(AttachmentRequestDto attachmentRequestDto);

        /// <summary>
        /// Updates an existing attachment.
        /// </summary>
        /// <param name="updateAttachmentDto">Data transfer object for updating an attachment.</param>
        /// <returns>A string representing the result of the update operation.</returns>
        Task<string> UpdateAttachment(UpdateAttachmentDto updateAttachmentDto);

        /// <summary>
        /// Deletes an attachment.
        /// </summary>
        /// <param name="deleteAttachmentDto">Data transfer object for deleting an attachment.</param>
        /// <returns>A boolean indicating the success of the deletion operation.</returns>
        Task<bool> DeleteAttachment(DeleteAttachmentDto deleteAttachmentDto);

        /// <summary>
        /// Retrieves a list of attachments based on the provided request data.
        /// </summary>
        /// <param name="listAttachmentRequestDto">Data transfer object for requesting a list of attachments.</param>
        /// <returns>A list of Attachment objects.</returns>
        Task<List<Attachment>> GetListAttachment(ListAttachmentRequestDto listAttachmentRequestDto);

        /// <summary>
        /// Upserts an attachment. This operation will update the attachment if it exists or create a new one if it does not.
        /// </summary>
        /// <param name="updateAttachmentDto">Data transfer object for updating an attachment.</param>
        /// <returns>A string representing the result of the upsert operation.</returns>
        Task<string> UpsertAttachment(UpdateAttachmentDto updateAttachmentDto);
    }
}
