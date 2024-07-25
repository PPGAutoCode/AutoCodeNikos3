
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
        /// <returns>A string representing the result of the creation operation.</returns>
        Task<string> CreateAttachment(CreateAttachmentDto createAttachmentDto);

        /// <summary>
        /// Retrieves an attachment based on the provided request data.
        /// </summary>
        /// <param name="attachmentRequestDto">The data transfer object for requesting an attachment.</param>
        /// <returns>An Attachment object.</returns>
        Task<Attachment> GetAttachment(AttachmentRequestDto attachmentRequestDto);

        /// <summary>
        /// Updates an existing attachment.
        /// </summary>
        /// <param name="updateAttachmentDto">The data transfer object for updating an attachment.</param>
        /// <returns>A string representing the result of the update operation.</returns>
        Task<string> UpdateAttachment(UpdateAttachmentDto updateAttachmentDto);

        /// <summary>
        /// Deletes an attachment.
        /// </summary>
        /// <param name="deleteAttachmentDto">The data transfer object for deleting an attachment.</param>
        /// <returns>A boolean indicating the success of the deletion operation.</returns>
        Task<bool> DeleteAttachment(DeleteAttachmentDto deleteAttachmentDto);

        /// <summary>
        /// Retrieves a list of attachments based on the provided request data.
        /// </summary>
        /// <param name="listAttachmentRequestDto">The data transfer object for requesting a list of attachments.</param>
        /// <returns>A list of Attachment objects.</returns>
        Task<List<Attachment>> GetListAttachment(ListAttachmentRequestDto listAttachmentRequestDto);

        /// <summary>
        /// Updates or inserts an attachment.
        /// </summary>
        /// <param name="attachment">The data transfer object for updating or inserting an attachment.</param>
        /// <returns>A string representing the result of the upsert operation.</returns>
        Task<string> UpsertAttachment(UpdateAttachmentDto attachment);
    }
}
