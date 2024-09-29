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
            logger.LogInformation("Введите путь к файлу для подключения к базам данных. Для выходы введите !q");

            while (String.IsNullOrEmpty(_databaseFilePath))
            {
                var databaseFilePath = Console.ReadLine();

                if (databaseFilePath == null || String.IsNullOrEmpty(databaseFilePath.Trim()))
                {
                    logger.LogError("Для дальнейшей работы Вы должны обязательно ввести путь к файлу для полдключения к базам данных");
                    logger.LogInformation("Для выходы введите !q");
                    continue;
                }

                if (databaseFilePath.Equals("!q"))
                {
                    return;
                }

                if (!Path.Exists(databaseFilePath))
                {
                    logger.LogError("Вы указали неверный путь к файлу для полдключения к базам данных");
                    logger.LogInformation("Для выходы введите !q");
                    continue;
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

        logger.LogInformation($"Было найдено {databases.Count} БД");

        while (true)
        {
            logger.LogInformation("Введите SQL команду. Чтобы закончить ввод команды введите !s. Чтобы выйти введите !q");

            string sqlCommand = "";
            string? line = "";

            line = Console.ReadLine();

            while (line != null && !line.Equals("!s"))
            {
                if (line.Equals("!q"))
                    return;

                sqlCommand += line;
                line = Console.ReadLine();
            }

            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.Write("\r" + new string(' ', Console.BufferWidth) + "\r");
            Console.WriteLine("================");

            if (!String.IsNullOrEmpty(sqlCommand.Trim()))
            {
                foreach (var database in databases)
                {

                    Task task = new Task(() =>
                    {
                        logger.LogInformation($"Начало обновления базы: {database.Name}");

                        try
                        {
                            using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
                            using var connection =  dataSource.OpenConnection();
                            using var transaction = connection.BeginTransaction();

                            using var command = new NpgsqlCommand(sqlCommand, connection, transaction);
                            command.ExecuteNonQuery();

                            transaction.Commit();
                            logger.LogInformation($"Обновление бызы {database.Name} завершено");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex.Message);
                            logger.LogError($"Для базы: {database.Name} не применились изменения, попробуйте еще раз");
                        }
                    });

                    task.Start();
                    task.Wait();
                }
            }
            else
            {
                logger.LogError("Вы ввели пустую SQL команду");
            }
        }
    }
}