
namespace ProjectName.Types
{
    public class UpdateFAQDto
    {
        public Guid Id { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public List<Guid> FAQCategories { get; set; }
        public string Langcode { get; set; }
        public bool Status { get; set; }
        public int FaqOrder { get; set; }
        public DateTime Changed { get; set; }
    }
}
