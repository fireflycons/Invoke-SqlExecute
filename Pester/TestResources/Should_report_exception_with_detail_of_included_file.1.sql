print '1.0'
CREATE TABLE test
(
    Id INT NOT NULL PRIMARY KEY,
)

GO

INSERT INTO test
VALUES
    (1)
GO

:r Should_report_exception_with_detail_of_included_file.2.sql
