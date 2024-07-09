
namespace ProjectName.Types
{
    public class UpdateAppEnvironmentDto
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public int? Version { get; set; }
        public DateTime Changed { get; set; }
        public Guid ChangedUser { get; set; }
    }
}
