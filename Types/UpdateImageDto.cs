
namespace ProjectName.Types
{
    public class UpdateImageDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public byte[] Image { get; set; }
        public string? AltText { get; set; }
    }
}
