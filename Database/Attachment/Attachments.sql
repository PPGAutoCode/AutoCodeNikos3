CREATE TABLE Attachments (
    Id uniqueidentifier PRIMARY KEY,
    FileName nvarchar(100) NOT NULL,
    FileUrl varbinary(max),
    FilePath nvarchar(500),
    File nvarchar(max) NOT NULL,
    Timestamp datetime NOT NULL
);