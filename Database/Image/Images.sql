CREATE TABLE Images (
    Id uniqueidentifier PRIMARY KEY,
    FileName nvarchar(100) NOT NULL,
    ImageData varbinary(max) NOT NULL,
    ImagePath nvarchar(500) UNIQUE NOT NULL,
    AltText nvarchar(500),
    Version int,
    Created datetime2(7) NOT NULL UNIQUE,
    Changed datetime2(7) UNIQUE,
    CreatorId uniqueidentifier NOT NULL UNIQUE,
    ChangedUser uniqueidentifier
);