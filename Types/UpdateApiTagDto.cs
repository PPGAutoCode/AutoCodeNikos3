
namespace ProjectName.Types
{
    public class UpdateApiTagDto
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public Guid ChangedUser { get; set; }
    }
}
