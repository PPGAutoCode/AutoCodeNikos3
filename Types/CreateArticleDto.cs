
namespace ProjectName.Types
{
    public class CreateArticleDto
    {
        public string Title { get; set; }
        public Guid Author { get; set; }
        public string? Summary { get; set; }
        public string? Body { get; set; }
        public string? GoogleDriveId { get; set; }
        public bool HideScrollSpy { get; set; }
        public UpdateImageDto? Image { get; set; }
        public UpdateAttachmentDto? Pdf { get; set; }
        public string Langcode { get; set; }
        public bool Status { get; set; }
        public bool Sticky { get; set; }
        public bool Promote { get; set; }
        public List<Guid> BlogCategories { get; set; }
        public List<string>? BlogTags { get; set; }
        public Guid CreatorId { get; set; }
    }
}
