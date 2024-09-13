using DatabaseUpdater;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text;
using System.Text.Json;

internal class Program
{
    private static async Task Main(string[] args)
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = factory.CreateLogger("Program");

        string helpText = @"Информация

Параметры:
-h: Информация о команде

Аргументы: 
[Путь к файлу с базами] [Путь к файлу с sql скриптом]";

        if (args.Length == 1)
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
        }
    }
}