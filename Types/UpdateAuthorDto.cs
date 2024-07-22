
namespace ProjectName.Types
{
    public class UpdateAuthorDto
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public CreateImageDto Image { get; set; }
        public string Details { get; set; }
    }
}
