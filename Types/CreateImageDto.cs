
namespace ProjectName.Types
{
    public class CreateImageDto
    {
        public string FileName { get; set; }
        public byte[] Image { get; set; }
        public string? AltText { get; set; }
    }
}
