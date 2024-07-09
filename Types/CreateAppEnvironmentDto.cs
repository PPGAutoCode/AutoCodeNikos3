
namespace ProjectName.Types
{
    public class CreateAppEnvironmentDto
    {
        public string Name { get; set; }
        public int? Version { get; set; }
        public DateTime Created { get; set; }
        public Guid CreatorId { get; set; }
    }
}
