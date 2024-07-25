
namespace ProjectName.Types
{
    public class Article
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public Guid Author { get; set; }
        public string? Summary { get; set; }
        public string? Body { get; set; }
        public string? GoogleDriveId { get; set; }
        public bool HideScrollSpy { get; set; }
        public Guid? Image { get; set; }
        public Guid? Pdf { get; set; }
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
