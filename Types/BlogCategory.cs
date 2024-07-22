
namespace ProjectName.Types
{
    public class BlogCategory
    {
        public Guid Id { get; set; }
        public Guid? Parent { get; set; }
        public string Name { get; set; }
    }
}
