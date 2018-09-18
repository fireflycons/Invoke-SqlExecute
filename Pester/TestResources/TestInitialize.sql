if DB_ID(N'Test-InvokeSqExecute') IS NOT NULL
begin
	drop database [Test-InvokeSqExecute]
end
go

create database [Test-InvokeSqExecute]
go

use [Test-InvokeSqExecute]
GO

-- Assets for https://stackoverflow.com/questions/33271446/invoke-sqlcmd-runs-script-twice
create table dbo.s
(
	id int identity primary key,
	b varchar(50)
)
create table dbo.t
(
	id int primary key,
	s_id int,
	b varchar(50)
)

alter table dbo.t add constraint fk_t foreign key (s_id) references dbo.s(id)

