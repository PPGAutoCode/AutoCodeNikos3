
CREATE TABLE Images (
    Id uniqueidentifier PRIMARY KEY,
    ImageName nvarchar(100) NOT NULL,
    ImageFile nvarchar(max) NOT NULL,
    AltText nvarchar(500),
    Version int,
    Created datetime2(7),
    Changed datetime2(7),
    CreatorId uniqueidentifier,
    ChangedUser uniqueidentifier
);
