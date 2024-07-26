
namespace ProjectName.Types
{
    public class UpdateImageDto
    {
        public Guid Id { get; set; }
        public string? ImageName { get; set; }
        public string? ImageFile { get; set; }
        public string? AltText { get; set; }
    }
}
