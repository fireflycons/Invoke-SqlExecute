/*
    https://sqldevelopmentwizard.blogspot.com/2016/12/invoke-sqlcmd-and-error-results.html

    Issue #1
*/
:on error exit

PRINT 'Change database to single-user mode'

ALTER DATABASE [Test-InvokeSqExecute]
SET SINGLE_USER
WITH ROLLBACK IMMEDIATE;
GO

PRINT 'After database to single-user mode'

IF EXISTS (select 1 as res)
    RAISERROR (N'First Error.', 16, 127) WITH NOWAIT
GO

IF EXISTS (select 1 as res)
    RAISERROR (N'Second Error', 16, 127) WITH NOWAIT
GO

ALTER DATABASE [Test-InvokeSqExecute]
SET MULTI_USER;
GO

PRINT 'After database to multi-user mode'
