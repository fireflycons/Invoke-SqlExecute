:on error exit
PRINT 'Change database to single-user mode'
ALTER DATABASE [Test1]
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
ALTER DATABASE [Test1]
SET MULTI_USER;
GO         
PRINT 'After database to multi-user mode'
