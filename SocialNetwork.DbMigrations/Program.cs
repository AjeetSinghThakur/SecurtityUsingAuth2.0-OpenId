using System;
using System.Configuration;
using System.Reflection;
using DbUp;

namespace SocialNetwork.DbMigrations
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["SocialNetwork"].ConnectionString ??
                "Server=.\\sqlexpress;Database=BethanysPieShop4;Trusted_Connection=True;MultipleActiveResultSets=true";

            var upgrader =
                DeployChanges.To
                    .SqlDatabase(connectionString)
                    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                    .LogToConsole()
                    .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(result.Error);
                Console.ResetColor();
#if DEBUG
                Console.ReadLine();
#endif
                return -1;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success!");
            Console.ResetColor();
            return 0;

        }
    }
}
