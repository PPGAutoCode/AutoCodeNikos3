
namespace ProjectName.Types
{
    public class ArticleDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public Guid AuthorId { get; set; }
        public string? Summary { get; set; }
        public string? Body { get; set; }
        public string? GoogleDriveId { get; set; }
        public bool HideScrollSpy { get; set; }
        public Guid? ImageId { get; set; }
        public Guid? PdfId { get; set; }
        public string Langcode { get; set; }
        public bool Status { get; set; }
        public bool Sticky { get; set; }
        public bool Promote { get; set; }
        public int? Version { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Changed { get; set; }
        public Guid CreatorId { get; set; }
        public Guid? ChangedUser { get; set; }
    }
}
