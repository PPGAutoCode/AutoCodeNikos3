
namespace ProjectName.Types
{
    public class UpdateBlogTagDto
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public Guid ChangedUser { get; set; }
    }
}
