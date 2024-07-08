
namespace ProjectName.Types
{
    public class Response<T>
    {
        public T Payload { get; set; }
        public ResponseException Exception { get; set; }
    }
}
