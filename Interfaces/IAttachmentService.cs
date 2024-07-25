
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectName.Types;

namespace ProjectName.Interfaces
{
    /// <summary>
    /// Interface for managing attachments, providing CRUD and list operations.
    /// </summary>
    public interface IAttachmentService
    {
        /// <summary>
        /// Creates a new attachment based on the provided data transfer object.
        /// </summary>
        /// <param name="createAttachmentDto">The data transfer object containing the information to create the attachment.</param>
        /// <returns>A string representing the result of the creation operation.</returns>
        Task<string> CreateAttachment(CreateAttachmentDto createAttachmentDto);

        /// <summary>
        /// Retrieves an attachment based on the provided request data transfer object.
        /// </summary>
        /// <param name="attachmentRequestDto">The data transfer object containing the request information to retrieve the attachment.</param>
        /// <returns>An Attachment object representing the retrieved attachment.</returns>
        Task<Attachment> GetAttachment(AttachmentRequestDto attachmentRequestDto);

        /// <summary>
        /// Updates an existing attachment based on the provided data transfer object.
        /// </summary>
        /// <param name="updateAttachmentDto">The data transfer object containing the information to update the attachment.</param>
        /// <returns>A string representing the result of the update operation.</returns>
        Task<string> UpdateAttachment(UpdateAttachmentDto updateAttachmentDto);

        /// <summary>
        /// Deletes an attachment based on the provided data transfer object.
        /// </summary>
        /// <param name="deleteAttachmentDto">The data transfer object containing the information to delete the attachment.</param>
        /// <returns>A boolean indicating the success of the deletion operation.</returns>
        Task<bool> DeleteAttachment(DeleteAttachmentDto deleteAttachmentDto);

        /// <summary>
        /// Retrieves a list of attachments based on the provided request data transfer object.
        /// </summary>
        /// <param name="listAttachmentRequestDto">The data transfer object containing the request information to retrieve the list of attachments.</param>
        /// <returns>A list of Attachment objects representing the retrieved attachments.</returns>
        Task<List<Attachment>> GetListAttachment(ListAttachmentRequestDto listAttachmentRequestDto);

        /// <summary>
        /// Upserts an attachment based on the provided data transfer object. This operation will either update an existing attachment or create a new one if it does not exist.
        /// </summary>
        /// <param name="updateAttachmentDto">The data transfer object containing the information to upsert the attachment.</param>
        /// <param name="attachment">The optional Attachment object to be used in the upsert operation.</param>
        /// <returns>A string representing the result of the upsert operation.</returns>
        Task<string> UpsertAttachment(UpdateAttachmentDto updateAttachmentDto, Attachment attachment = null);
    }
}
