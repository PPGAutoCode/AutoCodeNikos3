
namespace ProjectName.Types
{
    public class UpdateAttachmentDto
    {
        public Guid? Id { get; set; }
        public string? FileName { get; set; }
        public byte[] FileUrl { get; set; }
    }
}
