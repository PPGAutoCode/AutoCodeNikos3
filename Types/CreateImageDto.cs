
namespace ProjectName.Types
{
    public class CreateImageDto
    {
        public string FileName { get; set; }
        public byte[] ImageData { get; set; }
        public string ImagePath { get; set; }
        public string? AltText { get; set; }
        public Guid CreatorId { get; set; }
    }
}
