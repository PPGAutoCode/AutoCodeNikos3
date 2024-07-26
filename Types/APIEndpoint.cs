
namespace ProjectName.Types
{
    public class APIEndpoint
    {
        public Guid Id { get; set; }
        public string ApiName { get; set; }
        public string? ApiScope { get; set; }
        public string? ApiScopeProduction { get; set; }
        public List<ApiTag>? ApiTags { get; set; }
        public bool Deprecated { get; set; }
        public string? Description { get; set; }
        public Guid? Documentation { get; set; }
        public string? EndpointUrls { get; set; }
        public Guid AppEnvironment { get; set; }
        public Guid? Swagger { get; set; }
        public Guid? Tour { get; set; }
        public string? ApiVersion { get; set; }
        public string Langcode { get; set; }
        public bool? Sticky { get; set; }
        public bool? Promote { get; set; }
        public string UrlAlias { get; set; }
        public bool Published { get; set; }
    }
}
