internal class Program
{
    private static void Main(string[] args)
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
            var databaseFilePathStr = args[0];
            var updateSqlFilePathStr = args[1];

            if (!Path.Exists(databaseFilePathStr))
            {
                Console.WriteLine("Database file path not exists");
            }

            if (!Path.Exists(updateSqlFilePathStr))
            {
                Console.WriteLine("Update sql file path not exists");
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