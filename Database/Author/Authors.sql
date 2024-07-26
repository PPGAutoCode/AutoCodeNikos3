
CREATE TABLE Authors (
    Id uniqueidentifier PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    Image uniqueidentifier NULL,
    Details nvarchar(max) NULL
);
