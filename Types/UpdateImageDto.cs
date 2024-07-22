
namespace ProjectName.Types
{
    public class UpdateImageDto
    {
        public Guid? Id { get; set; }
        public string? FileName { get; set; }
        public byte[] ImageData { get; set; }
        public string ImagePath { get; set; }
        public string? AltText { get; set; }
        public Guid ChangedUser { get; set; }
    }
}
