# DatabaseUpdater

## ��������� 

`-h`: ���������� � �������

## �������� 

- ���� � ����� � ������ ������
- ���� � ����� � sql ��������

## ������ 

- ������ �������

```shell
.\DatabaseUpdater.exe D:/MyProject/databases.json D:/MyProject/updatesql.sql
```

## ���� � ������ ������

- ���� ������ ������ ��� `json` 
- ������������ ���������:
    - Name: �������� ��
    - ConnectionString: ������ ����������� � ��
    - NeedChange: ��������� �� ��������� � ��
- ������ �����:

```json
[
  {
    "Name": "Test1",
    "ConnectionString": "Host=localhost;Username=login;Password=pass;Database=test1",
    "NeedChange": true
  },
  {
    "Name": "Test2",
    "ConnectionString": "Host=localhost;Username=login;Password=pass;Database=test2",
    "NeedChange": true
  }
]
```

## ���� � ����� � sql ��������

- ���� ������ ������ ��� `sql`
- ������� ��������� ������ ������� 
- ������� ����������� � ����� ����������!!!
- ������ �����: 


```sql
CREATE TABLE films (
    code        char(5) CONSTRAINT firstkey PRIMARY KEY,
    title       varchar(40) NOT NULL,
    did         integer NOT NULL,
    date_prod   date,
    kind        varchar(10),
    len         interval hour to minute
);

CREATE TABLE test (
    id          serial PRIMARY KEY,
    title       varchar(40) NOT NULL,
);
```