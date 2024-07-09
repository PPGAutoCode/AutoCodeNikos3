
CREATE TABLE ApiTags (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    Version int NULL,
    Created datetime2(7) NOT NULL,
    Changed datetime2(7) NOT NULL,
    CreatorId uniqueidentifier NOT NULL,
    ChangedUser uniqueidentifier NOT NULL
);
