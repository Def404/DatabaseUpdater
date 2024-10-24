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
					_logger.LogError("Указан неверный путь к файлу для подключения к базам данных.");
					continue;
				}
			}
		}
		else if (args.Length == 1 && args[0].Equals("-h"))
		{
			var helpText = @"Информация о приложении и доступных параметрах.

Параметры:
-h: Информация о приложении и доступных параметрах.
-d: Указывает путь к файлу с базами данных.
-s: Указывает путь к файлу с SQL скриптом.

Примеры использования:
1. Без аргументов: программа запросит путь к файлу с базами данных, SQL запрос вводится вручную.
2. -d [путь к файлу с базами]: указывает путь к файлу с базами данных, SQL запрос вводится вручную..
3. -s [путь к файлу с SQL скриптом]: указывает путь к файлу с SQL скриптом.
4. -d [путь к файлу с базами] -s [путь к файлу с SQL скриптом]: указывает пути к файлам с базами данных и SQL скриптом.";

			_logger.LogInformation(helpText);

			return;
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
				_logger.LogError("Неверные аргументы. Используйте -h для справки.");
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
				_logger.LogError("Неверные аргументы. Используйте -h для справки.");
				return;
			}
			
			if (String.IsNullOrEmpty(_databaseFilePath) || String.IsNullOrEmpty(_sqlFilePath))
			{
				_logger.LogError("Неверные аргументы. Используйте -h для справки.");
				return;
			}
		}
		else
		{
			_logger.LogError("Неверное количество аргументов. Используйте -h для справки.");
			return;
		}

		if (!String.IsNullOrEmpty(_databaseFilePath) && String.IsNullOrEmpty(_sqlFilePath))
		{
			var databases = await ReadDatabaseFileAsync(_databaseFilePath);

			if (databases == null || databases.Count == 0)
			{
				_logger.LogError("Не удалось получить базы данных из файла.");
				return;
			}

			while (true)
			{
				_logger.LogInformation("Введите SQL команду. Чтобы закончить ввод команды введите !s. Чтобы выйти из приложения введите !q");

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

				if(ContainsProhibitedSqlCommands(sqlCommand))
				{
					_logger.LogError("Введена запрещенная SQL команда.");
					continue;
				}

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
					_logger.LogError("Указан неверный путь к файлу для подключения к базам данных.");
					continue;
				}
			}

			var databases = await ReadDatabaseFileAsync(_databaseFilePath);

			if (databases == null || databases.Count == 0)
			{
				_logger.LogError("Не удалось получить базы данных из файла.");
				return;
			}
			
			var query = await ReadSqlFileAsync(_sqlFilePath);

			if(String.IsNullOrEmpty(query))
			{
				_logger.LogError("Не удалось получить SQL запрос из файла.");
				return;
			}

			if (ContainsProhibitedSqlCommands(query))
			{
				_logger.LogError("Введена запрещенная SQL команда.");
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
				_logger.LogError("Не удалось получить базы данных из файла.");
				return;
			}

			var query = await ReadSqlFileAsync(_sqlFilePath);

			if (String.IsNullOrEmpty(query))
			{
				_logger.LogError("Не удалось получить SQL запрос из файла.");
				return;
			}

			if (ContainsProhibitedSqlCommands(query))
			{
				_logger.LogError("Введена запрещенная SQL команда.");
				return;
			}

			await ExecuteSqlAsync(query, databases);

			return;
		}
		else
		{
			_logger.LogError("Неверные аргументы. Используйте -h для справки.");
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
				_logger.LogError("Файл для подключения к базам данных пуст.");
				return databases;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError($"Ошибка при чтении файла баз данных: {ex.Message}");
			return databases;
		}

		try
		{
			databases = JsonSerializer.Deserialize<List<Database>>(databaseFileJson);

			if (databases == null || databases.Count == 0)
			{
				_logger.LogError("Не удалось прочитать информацию в файле баз данных.");
				return databases;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError($"Ошибка при десериализации файла баз данных: {ex.Message}");
			return databases;
		}

		_logger.LogInformation($"Базы данных {databases.Count} успешно загружены.");

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
				_logger.LogError("Файл с SQL запросом пуст.");
				return query;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError($"Ошибка при чтении файла SQL запроса: {ex.Message}");
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
				if(database.NeedChange == false)
				{
					_logger.LogInformation($"Изменения для базы данных {database.Name} не требуются.");
					continue;
				}

				Task task = new Task(() =>
				{
					_logger.LogInformation($"Начало обновления базы данных: {database.Name}");

					try
					{
						using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
						using var connection = dataSource.OpenConnection();
						using var transaction = connection.BeginTransaction();

						using var command = new NpgsqlCommand(sqlCommand, connection, transaction);
						command.ExecuteNonQuery();

						transaction.Commit();

						_logger.LogInformation($"Обновление базы данных {database.Name} завершено.");
					}
					catch (Exception ex)
					{
						_logger.LogError($"Ошибка при обновлении базы данных {database.Name}: {ex.Message}");
						_logger.LogError($"Для базы данных {database.Name} не применились изменения, попробуйте еще раз.");
					}
				});

				task.Start();
				task.Wait();
			}
		}
		else
		{
			_logger.LogError("Введена пустая SQL команда.");
		}
	}

	private static bool ContainsProhibitedSqlCommands(string sqlCommand)
	{
		var prohibitedCommands = new List<string>
		{
			"CREATE DATABASE",
			"DROP DATABASE",
			"CREATE ROLE",
			"ALTER ROLE",
			"DROP ROLE",
			"ALTER SYSTEM",
			"CREATE EXTENSION",
			"DROP EXTENSION",
			"GRANT",
			"REVOKE",
			"DROP TABLE",
			"DROP SCHEMA",
			"TRUNCATE"
		};

		foreach (var command in prohibitedCommands)
		{
			if (sqlCommand.ToUpper().Contains(command))
			{
				return true;
			}
		}

		return false;
	}
}