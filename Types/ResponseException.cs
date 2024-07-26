
namespace ProjectName.Types
{
    public class ResponseException : System.Exception
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
    }
}
