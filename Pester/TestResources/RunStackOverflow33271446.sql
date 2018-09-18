/*
    https://stackoverflow.com/questions/33271446/invoke-sqlcmd-runs-script-twice
*/
insert into dbo.s ( b) select  'hello world'
insert into dbo.t (s_id, b) -- purposely missing id to cause an error
select 1, 'good morning'
