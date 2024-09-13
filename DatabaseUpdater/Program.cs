using DatabaseUpdater;
using System.IO;
using System.Text;
using System.Text.Json;

internal class Program
{
    private static async Task Main(string[] args)
    {

        if (args.Length == 1)
        {
            if (args[0].Contains("-h"))
            {
                Console.WriteLine("help");
            }
        }
        else if (args.Length == 2)
        {
            var databaseFilePath = args[0];
            var updateSqlFilePath = args[1];

            if (!Path.Exists(databaseFilePath))
            {
                Console.WriteLine("Database file path not exists");
                return;
            }

            if (!Path.Exists(updateSqlFilePath))
            {
                Console.WriteLine("Update sql file path not exists");
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
                Console.WriteLine("Database file is empty");
                return;
            }

            var databases = JsonSerializer.Deserialize<List<Database>>(databaseFileJson);

            if (databases == null || databases.Count == 0) 
            {
                Console.WriteLine("Database file is error");
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
                Console.WriteLine("Update sql file is empty");
                return;
            }

            Console.WriteLine("Hello, World!");
        }
        else
        {
            Console.WriteLine("help");
            return;
        }
    }
}