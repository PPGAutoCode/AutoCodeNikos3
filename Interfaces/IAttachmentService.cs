
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
        /// Creates a new attachment based on the provided data transfer object.
        /// </summary>
        /// <param name="createAttachmentDto">Data transfer object containing information for creating a new attachment.</param>
        /// <returns>A string representing the result of the creation operation.</returns>
        Task<string> CreateAttachment(CreateAttachmentDto createAttachmentDto);

        /// <summary>
        /// Retrieves an attachment based on the provided request data transfer object.
        /// </summary>
        /// <param name="attachmentRequestDto">Data transfer object containing request information for retrieving an attachment.</param>
        /// <returns>An Attachment object matching the request.</returns>
        Task<Attachment> GetAttachment(AttachmentRequestDto attachmentRequestDto);

        /// <summary>
        /// Updates an existing attachment based on the provided data transfer object.
        /// </summary>
        /// <param name="updateAttachmentDto">Data transfer object containing information for updating an attachment.</param>
        /// <returns>A string representing the result of the update operation.</returns>
        Task<string> UpdateAttachment(UpdateAttachmentDto updateAttachmentDto);

        /// <summary>
        /// Deletes an attachment based on the provided data transfer object.
        /// </summary>
        /// <param name="deleteAttachmentDto">Data transfer object containing information for deleting an attachment.</param>
        /// <returns>A boolean indicating the success of the deletion operation.</returns>
        Task<bool> DeleteAttachment(DeleteAttachmentDto deleteAttachmentDto);

        /// <summary>
        /// Retrieves a list of attachments based on the provided request data transfer object.
        /// </summary>
        /// <param name="listAttachmentRequestDto">Data transfer object containing request information for retrieving a list of attachments.</param>
        /// <returns>A list of Attachment objects matching the request.</returns>
        Task<List<Attachment>> GetListAttachment(ListAttachmentRequestDto listAttachmentRequestDto);

        /// <summary>
        /// Upserts an attachment based on the provided data transfer object.
        /// </summary>
        /// <param name="updateAttachmentDto">Data transfer object containing information for upserting an attachment.</param>
        /// <param name="attachment">The Attachment object to be upserted.</param>
        /// <returns>A string representing the result of the upsert operation.</returns>
        Task<string> UpsertAttachment(UpdateAttachmentDto updateAttachmentDto, Attachment attachment);
    }
}
