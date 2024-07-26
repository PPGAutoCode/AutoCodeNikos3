
namespace ProjectName.Types
{
    public class Request<T>
    {
        public RequestHeader Header { get; set; }
        public T Payload { get; set; }
    }
}
