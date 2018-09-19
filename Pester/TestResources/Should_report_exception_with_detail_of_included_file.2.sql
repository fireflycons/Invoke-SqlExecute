-- Re-insert the same value to get a PK violation
-- If this insert is moved to another line, the test assertion will fail until it is updated.
INSERT INTO test
VALUES
    (1)
GO
