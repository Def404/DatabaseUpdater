using DatabaseUpdater;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text;
using System.Text.Json;

internal class Program
{
	private static string _databaseFilePath = "";
	private static string _sqlFilePath = "";

	private static ILogger _logger;

	private static async Task Main(string[] args)
	{
		using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
		_logger = factory.CreateLogger("Program");

		if (args.Length == 0)
		{
			var databaseFilePath = "";

			while (String.IsNullOrEmpty(_databaseFilePath))
			{
				_logger.LogInformation("Введите путь к файлу для подключения к базам данных.");

				databaseFilePath = Console.ReadLine();

				if (IsValidFilePath(databaseFilePath))
				{
					_databaseFilePath = databaseFilePath;
				}
				else
				{
					_logger.LogError("Вы указали неверный путь к файлу для полдключения к базам данных");
					continue;
				}
			}
		}
		else if (args.Length == 2)
		{
			if (args[0].Equals("-d") && IsValidFilePath(args[1]))
			{
				_databaseFilePath = args[1];
			}
			else if (args[0].Equals("-s") && IsValidFilePath(args[1]))
			{
				_sqlFilePath = args[1];
			}
			else
			{
				_logger.LogError("Неверные аргументы");
				return;
			}
		}
		else if (args.Length == 4)
		{
			if (args[0].Equals("-d") && IsValidFilePath(args[1]) && args[2].Equals("-s") && IsValidFilePath(args[3]))
			{
				_databaseFilePath = args[1];
				_sqlFilePath = args[3];
			}
			else if (args[0].Equals("-s") && IsValidFilePath(args[1]) && args[2].Equals("-d") && IsValidFilePath(args[3]))
			{
				_sqlFilePath = args[1];
				_databaseFilePath = args[3];
			}
			else
			{
				_logger.LogError("Неверные аргументы");
				return;
			}
			
			if (String.IsNullOrEmpty(_databaseFilePath) || String.IsNullOrEmpty(_sqlFilePath))
			{
				_logger.LogError("Неверные аргументы");
				return;
			}
		}
		else
		{
			_logger.LogError("Неверное количество аргументов");
			return;
		}

		if (!String.IsNullOrEmpty(_databaseFilePath) && String.IsNullOrEmpty(_sqlFilePath))
		{
			var databases = await ReadDatabaseFileAsync(_databaseFilePath);

			if (databases == null || databases.Count == 0)
			{
				_logger.LogError("Не удалось получить базы данных из файла");
				return;
			}

			while (true)
			{
				_logger.LogInformation("Введите SQL команду. Чтобы закончить ввод команды введите !s.");

				string sqlCommand = "";
				string? line = "";

				line = Console.ReadLine();

				if (line != null && line.Equals("!q"))
				{
					return;
				}

				while (line != null && !line.Equals("!s"))
				{
					sqlCommand += line;
					line = Console.ReadLine();
				}

				Console.SetCursorPosition(0, Console.CursorTop - 1);
				Console.Write("\r" + new string(' ', Console.BufferWidth) + "\r");
				Console.WriteLine("================");


				await ExecuteSqlAsync(sqlCommand, databases);
			}
		}
		else if (String.IsNullOrEmpty(_databaseFilePath) && !String.IsNullOrEmpty(_sqlFilePath))
		{
			var databaseFilePath = "";

			while (String.IsNullOrEmpty(_databaseFilePath))
			{
				_logger.LogInformation("Введите путь к файлу для подключения к базам данных.");

				databaseFilePath = Console.ReadLine();

				if (IsValidFilePath(databaseFilePath))
				{
					_databaseFilePath = databaseFilePath;
				}
				else
				{
					_logger.LogError("Вы указали неверный путь к файлу для полдключения к базам данных");
					continue;
				}
			}

			var databases = await ReadDatabaseFileAsync(_databaseFilePath);

			if (databases == null || databases.Count == 0)
			{
				_logger.LogError("Не удалось получить базы данных из файла");
				return;
			}
			
			var query = await ReadSqlFileAsync(_sqlFilePath);

			if(String.IsNullOrEmpty(query))
			{
				_logger.LogError("Не удалось получить SQL запрос из файла");
				return;
			}

			await ExecuteSqlAsync(query, databases);

			return;
		}
		else if (!String.IsNullOrEmpty(_databaseFilePath) && !String.IsNullOrEmpty(_sqlFilePath))
		{
			var databases = await ReadDatabaseFileAsync(_databaseFilePath);

			if (databases == null || databases.Count == 0)
			{
				_logger.LogError("Не удалось получить базы данных из файла");
				return;
			}

			var query = await ReadSqlFileAsync(_sqlFilePath);

			if (String.IsNullOrEmpty(query))
			{
				_logger.LogError("Не удалось получить SQL запрос из файла");
				return;
			}

			await ExecuteSqlAsync(query, databases);

			return;
		}
		else
		{
			_logger.LogError("Неверные аргументы");
			return;
		}
	}

	private static bool IsValidFilePath(string? path)
	{
		return !String.IsNullOrEmpty(path) && Path.Exists(path);
	}

	private static async Task<List<Database>?> ReadDatabaseFileAsync(string databasePath)
	{
		List<Database>? databases = new List<Database>();

		var databaseFileJson = "";

		try
		{
			using (var fs = File.OpenRead(databasePath))
			{
				byte[] buffer = new byte[fs.Length];

				await fs.ReadAsync(buffer, 0, buffer.Length);

				databaseFileJson = Encoding.Default.GetString(buffer);
			}

			if (String.IsNullOrEmpty(databaseFileJson))
			{
				_logger.LogError("Файл для полдключения к базам данных пуст");
				return databases;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex.Message);
			return databases;
		}

		try
		{
			databases = JsonSerializer.Deserialize<List<Database>>(databaseFileJson);

			if (databases == null || databases.Count == 0)
			{
				_logger.LogError("Не удалось прочитать информацию в файле");
				return databases;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex.Message);
			return databases;
		}

		_logger.LogInformation($"Базы данных {databases.Count} успешно загружены");

		return databases;
	}

	private static async Task<string> ReadSqlFileAsync(string sqlPath)
	{
		var query = "";

		try
		{
			using (var fs = File.OpenRead(sqlPath))
			{
				byte[] buffer = new byte[fs.Length];

				await fs.ReadAsync(buffer, 0, buffer.Length);

				query = Encoding.Default.GetString(buffer);
			}

			if (String.IsNullOrEmpty(query))
			{
				_logger.LogError("Файл с sql запросом пуст");
				return query;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex.Message);
			return query;
		}

		return query;
	}

	private static async Task ExecuteSqlAsync(string sqlCommand, List<Database> databases)
	{
		if (!String.IsNullOrEmpty(sqlCommand.Trim()))
		{
			foreach (var database in databases)
			{
				Task task = new Task(() =>
				{
					_logger.LogInformation($"Начало обновления базы: {database.Name}");

					try
					{
						using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
						using var connection = dataSource.OpenConnection();
						using var transaction = connection.BeginTransaction();

						using var command = new NpgsqlCommand(sqlCommand, connection, transaction);
						command.ExecuteNonQuery();

						transaction.Commit();
						_logger.LogInformation($"Обновление бызы {database.Name} завершено");
					}
					catch (Exception ex)
					{
						_logger.LogError(ex.Message);
						_logger.LogError($"Для базы: {database.Name} не применились изменения, попробуйте еще раз");
					}
				});

				task.Start();
				task.Wait();
			}
		}
		else
		{
			_logger.LogError("Вы ввели пустую SQL команду");
		}
	}
}