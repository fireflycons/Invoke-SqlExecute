/*
    https://sqldevelopmentwizard.blogspot.com/2016/12/invoke-sqlcmd-and-error-results.html

    Issue #2
*/
CREATE OR ALTER PROCEDURE dbo.geterror
as
create table #t(n int not null)

insert into #t(n) values(null)
go

EXEC dbo.geterror


