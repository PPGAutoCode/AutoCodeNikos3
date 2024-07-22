#Path: BlogCategory
#File: BlogCategories.sql

CREATE TABLE BlogCategories (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Parent UNIQUEIDENTIFIER,
    Name NVARCHAR(200) NOT NULL,
    CONSTRAINT FK_BlogCategories_Parent FOREIGN KEY (Parent) REFERENCES BlogCategories(Id)
);