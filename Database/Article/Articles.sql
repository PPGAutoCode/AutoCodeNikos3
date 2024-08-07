CREATE TABLE Articles (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Author UNIQUEIDENTIFIER NOT NULL,
    Summary NVARCHAR(500),
    Body NVARCHAR(MAX),
    GoogleDriveID NVARCHAR(50),
    HideScrollSpy BIT NOT NULL,
    Image UNIQUEIDENTIFIER,
    PDF UNIQUEIDENTIFIER,
    Langcode NVARCHAR(4) NOT NULL,
    Status BIT NOT NULL,
    Sticky BIT NOT NULL,
    Promote BIT NOT NULL,
    Version INT,
    Created DATETIME2(7) NOT NULL,
    Changed DATETIME2(7),
    CreatorId UNIQUEIDENTIFIER NOT NULL,
    ChangedUser UNIQUEIDENTIFIER
);