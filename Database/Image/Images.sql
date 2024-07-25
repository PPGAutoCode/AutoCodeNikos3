CREATE TABLE Images (
    Id uniqueidentifier PRIMARY KEY,
    ImageName nvarchar(100) NOT NULL,
    ImageFile nvarchar(max) NOT NULL,
    AltText nvarchar(500) NULL,
    ImageData varbinary(max) NOT NULL,
    ImagePath nvarchar(500) UNIQUE NOT NULL,
    Version int NULL,
    Created datetime2(7) NOT NULL UNIQUE,
    Changed datetime2(7) NULL UNIQUE,
    CreatorId uniqueidentifier NOT NULL,
    ChangedUser uniqueidentifier NULL
);