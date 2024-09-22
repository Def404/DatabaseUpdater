using DatabaseUpdater;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text;
using System.Text.Json;

internal class Program
{
    private static string _databaseFilePath = "";

    private static async Task Main(string[] args)
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = factory.CreateLogger("Program");

        string helpText = @"Информация

Параметры:
-h: Информация о команде

Аргументы: 
[Путь к файлу с базами] [Путь к файлу с sql скриптом]";


        /* Аргументы 
         * [] -- пустой аргумент
         * [-f] [путь к файлу с бд] -- путь к файлу с бд
         */


        if (args.Length == 0)
        { 
            logger.LogInformation("Введите путь к файлу для подключения к базам данных\nДля выходы введите /q");

            while (String.IsNullOrEmpty(_databaseFilePath))
            {
                var databaseFilePath = Console.ReadLine();

                
                if (databaseFilePath == null || String.IsNullOrEmpty(databaseFilePath.Trim()))
                {
                    logger.LogError("Для дальнейшей работы Вы должны обязательно ввести путь к файлу для полдключения к базам данных");
                    logger.LogInformation("Для выходы введите /q");
                    continue;
                }

                if (!Path.Exists(databaseFilePath))
                {
                    logger.LogError("Вы указали неверный путь к файлу для полдключения к базам данных");
                    logger.LogInformation("Для выходы введите /q");
                    continue;
                }

                if (databaseFilePath.Equals("/q"))
                {
                    return;
                }

                _databaseFilePath = databaseFilePath;
            }
        }

        if(args.Length == 2 && args[0].Equals("-f"))
        {
            var databaseFilePath = args[1];

            if (databaseFilePath == null || String.IsNullOrEmpty(databaseFilePath.Trim()))
            {
                logger.LogError("Для дальнейшей работы Вы должны обязательно ввести путь к файлу для полдключения к базам данных");
                return;
            }

            if (!Path.Exists(databaseFilePath))
            {
                logger.LogError("Вы указали неверный путь к файлу для полдключения к базам данных");
                return;
            }

            _databaseFilePath = databaseFilePath;
        }

        var databaseFileJson = "";

        try
        {
            using (var fs = File.OpenRead(_databaseFilePath))
            {
                byte[] buffer = new byte[fs.Length];

                await fs.ReadAsync(buffer, 0, buffer.Length);

                databaseFileJson = Encoding.Default.GetString(buffer);
            }

            if (String.IsNullOrEmpty(databaseFileJson))
            {
                logger.LogError("Файл для полдключения к базам данных пуст");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return;
        }

        var databases = new List<Database>();

        try
        {
            databases = JsonSerializer.Deserialize<List<Database>>(databaseFileJson);

            if (databases == null || databases.Count == 0)
            {
                logger.LogError("Не удалось прочитать информацию в файле");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return;
        }

        /*if (args.Length == 1)
        {
            if (args[0].Contains("-h"))
            {
                logger.LogInformation(helpText);
                return;
            }
        }
        else if (args.Length == 2)
        {
            var databaseFilePath = args[0];
            var updateSqlFilePath = args[1];

            if (!Path.Exists(databaseFilePath))
            {
                logger.LogError("Database file path not exists");
                return;
            }

            if (!Path.Exists(updateSqlFilePath))
            {
                logger.LogError("Update sql file path not exists");
                return;
            }

            var databaseFileJson = "";

            using (var fs = File.OpenRead(databaseFilePath))
            {
                byte[] buffer = new byte[fs.Length];

                await fs.ReadAsync(buffer, 0, buffer.Length);

                databaseFileJson = Encoding.Default.GetString(buffer);
            }

            if (String.IsNullOrEmpty(databaseFileJson))
            {
                logger.LogError("Database file is empty");
                return;
            }

            var databases = JsonSerializer.Deserialize<List<Database>>(databaseFileJson);

            if (databases == null || databases.Count == 0)
            {
                logger.LogError("Database file is error");
                return;
            }

            var updateSqlStr = "";

            using (var fs = File.OpenRead(updateSqlFilePath))
            {
                byte[] buffer = new byte[fs.Length];

                await fs.ReadAsync(buffer, 0, buffer.Length);

                updateSqlStr = Encoding.Default.GetString(buffer);
            }

            if (String.IsNullOrEmpty(updateSqlStr))
            {
                logger.LogError("Update sql file is empty");
                return;
            }

            foreach (var database in databases)
            {
                logger.LogInformation($"Start database update: {database.Name}");

                try
                {
                    await using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
                    await using var connection = await dataSource.OpenConnectionAsync();
                    await using var transaction = await connection.BeginTransactionAsync();

                    await using var command = new NpgsqlCommand(updateSqlStr, connection, transaction);
                    await command.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();
                    logger.LogInformation($"Finish database update {database.Name}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    logger.LogError($"No changes have been applied to the database: {database.Name}");
                    continue;
                }
            }
        }
        else
        {
            logger.LogInformation(helpText);
            return;
        }*/
    }
}