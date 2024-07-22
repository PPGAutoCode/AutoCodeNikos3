
namespace ProjectName.Types
{
    public class Image
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string ImageData { get; set; }
        public string ImagePath { get; set; }
        public string? AltText { get; set; }
        public int? Version { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Changed { get; set; }
        public Guid CreatorId { get; set; }
        public Guid? ChangedUser { get; set; }
    }
}