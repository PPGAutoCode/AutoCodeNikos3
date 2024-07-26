#Path: Publisher
#File: Publishers.sql
CREATE TABLE Publishers (
    Id uniqueidentifier PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    Address nvarchar(300) NULL
);