CREATE TABLE Users (
    UserId int PRIMARY KEY,
    UserName varchar(50) NOT NULL,
    Email varchar(100) UNIQUE,
    JoinDate date NOT NULL
);