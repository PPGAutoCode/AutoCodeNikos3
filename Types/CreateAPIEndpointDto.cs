
namespace ProjectName.Types
{
    public class CreateAPIEndpointDto
    {
        public string ApiName { get; set; }
        public string? ApiScope { get; set; }
        public string? ApiScopeProduction { get; set; }
        public List<string>? ApiTags { get; set; }
        public bool Deprecated { get; set; }
        public string? Description { get; set; }
        public UpdateAttachmentDto? Documentation { get; set; }
        public string? EndpointUrls { get; set; }
        public Guid AppEnvironment { get; set; }
        public UpdateAttachmentDto? Swagger { get; set; }
        public UpdateAttachmentDto? Tour { get; set; }
        public string? ApiVersion { get; set; }
        public string Langcode { get; set; }
        public bool? Sticky { get; set; }
        public bool? Promote { get; set; }
        public string UrlAlias { get; set; }
        public bool Published { get; set; }
    }
}
