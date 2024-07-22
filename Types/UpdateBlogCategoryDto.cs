
namespace ProjectName.Types
{
    public class UpdateBlogCategoryDto
    {
        public Guid? Id { get; set; }
        public Guid? Parent { get; set; }
        public string? Name { get; set; }
    }
}
