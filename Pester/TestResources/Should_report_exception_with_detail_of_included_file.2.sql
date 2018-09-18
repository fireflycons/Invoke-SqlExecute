-- Re-insert the same value to get a PK violation
print '2.0'
INSERT INTO test
VALUES
    (1)
GO
