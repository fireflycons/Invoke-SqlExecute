/*
    https://sqldevelopmentwizard.blogspot.com/2016/12/invoke-sqlcmd-and-error-results.html

    Issue #2
*/

IF EXISTS (SELECT 1 FROM sys.procedures WHERE [name] = 'geterror' and [schema_id] = 1)
	DROP PROCEDURE dbo.geterror
GO

CREATE PROCEDURE dbo.geterror
as
    create table #t(n int not null)

    insert into #t(n) values(null)
go

EXEC dbo.geterror


