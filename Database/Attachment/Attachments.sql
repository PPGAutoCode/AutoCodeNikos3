#Path: Attachment
#File: Attachments.sql
CREATE TABLE Attachments (
    Id uniqueidentifier PRIMARY KEY,
    FileName nvarchar(100) NOT NULL,
    File nvarchar(max) NOT NULL,
    Timestamp datetime NOT NULL,
    FileUrl varbinary(max),
    FilePath nvarchar(500)
);