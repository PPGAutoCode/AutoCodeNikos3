
namespace ProjectName.Types
{
    public class Attachment
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public byte[] FileUrl { get; set; }
        public string FilePath { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
