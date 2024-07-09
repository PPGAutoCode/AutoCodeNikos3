CREATE TABLE Attachments (
    Id uniqueidentifier PRIMARY KEY,
    FileName nvarchar(100) NOT NULL UNIQUE,
    FileUrl varbinary(max) NOT NULL,
    Timestamp datetime NOT NULL
);