
CREATE TABLE Images (
    Id uniqueidentifier PRIMARY KEY,
    FileName nvarchar(100) NOT NULL UNIQUE,
    ImageData varbinary(max) NOT NULL,
    AltText nvarchar(500) NULL,
    Version int NULL,
    Created datetime2(7) NOT NULL,
    Changed datetime2(7) NOT NULL,
    CreatorId uniqueidentifier NOT NULL,
    ChangedUser uniqueidentifier NOT NULL
);
