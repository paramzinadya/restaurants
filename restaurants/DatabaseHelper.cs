using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.Windows.Forms;

namespace restaurants
{
    public static class DatabaseHelper
    {
        // Метод для получения полного пути к базе данных
        private static string GetDatabasePath()
        {
            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string databaseRelativePath = "restaurants_database.db"; // Путь к базе данных
            string databasePath = System.IO.Path.Combine(projectDirectory, databaseRelativePath);
            //Console.WriteLine("Database Path: " + databasePath); // Выводим путь к базе данных
            return databasePath;
        }

        // Строка подключения к базе данных SQLite
        private static string ConnectionString = $"Data Source={GetDatabasePath()};Version=3;";

        // Метод для получения подключения к базе данных
        public static SQLiteConnection GetConnection()
        {
            string connectionString = $"Data Source={GetDatabasePath()};Version=3;";
            return new SQLiteConnection(connectionString);
        }
    }
}
