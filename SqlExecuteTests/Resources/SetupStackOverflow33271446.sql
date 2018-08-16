create table dbo.s (id int identity primary key, b varchar(50))
create table dbo.t (id int primary key, s_id int, b varchar(50))
alter table dbo.t add constraint fk_t foreign key (s_id) references dbo.s(id)
