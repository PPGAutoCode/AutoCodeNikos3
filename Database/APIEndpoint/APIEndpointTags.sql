
CREATE TABLE APIEndpointTags (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    APIEndpointId uniqueidentifier NOT NULL,
    APITagId uniqueidentifier NOT NULL
);
