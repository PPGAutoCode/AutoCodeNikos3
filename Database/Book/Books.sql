#Path: Book
#File: Books.sql
CREATE TABLE Books (
    Id uniqueidentifier PRIMARY KEY,
    Title nvarchar(200) NOT NULL,
    AuthorId uniqueidentifier NOT NULL,
    PublishedDate datetime NULL,
    FOREIGN KEY (AuthorId) REFERENCES Authors(Id)
);