# DatabaseUpdater

## Параметры 

`-h`: Информация о команде

## Аргуметы 

- Путь к файлу с базами данных
- Путь к файлу с sql скриптом

## Запуск 

- Пример запуска

```shell
.\DatabaseUpdater.exe D:/MyProject/databases.json D:/MyProject/updatesql.sql
```

## Файл с базами данных

- Файл должен именть тип `json` 
- Обязательные параметры:
    - Name: название БД
    - ConnectionString: строка подключения к БД
    - NeedChange: применять ли изменения к БД
- Пример файла:

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

## Путь к файлу с sql скриптом

- Файл должен именть тип `sql`
- Команды разделять пустой строкой 
- Команды выполняются в одной транзакции!!!
- Пример файла: 


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