
namespace ProjectName.Types
{
    public class RequestHeader
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid Application { get; set; } = new Guid("03FC0B90-DFAD-11EE-8D86-0800200C9A66");
        public string Bank { get; set; }
        public Guid UserId { get; set; }
    }
}
