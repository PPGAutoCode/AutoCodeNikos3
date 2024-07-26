
CREATE TABLE APIEndpoints (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    ApiName nvarchar(200) NOT NULL,
    ApiScope nvarchar(200) NULL,
    ApiScopeProduction nvarchar(200) NULL,
    Deprecated bit NULL,
    Description nvarchar(max) NULL,
    Documentation uniqueidentifier NULL,
    EndpointUrls nvarchar(200) NULL,
    AppEnvironment uniqueidentifier NULL,
    Swagger uniqueidentifier NULL,
    Tour uniqueidentifier NULL,
    ApiVersion nvarchar(200) NULL,
    Langcode nvarchar(4) NOT NULL,
    Sticky bit NULL,
    Promote bit NULL,
    UrlAlias nvarchar(200) NOT NULL,
    Published bit NOT NULL
);
