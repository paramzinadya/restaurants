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
            Console.WriteLine("Database Path: " + databasePath); // Выводим путь к базе данных
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




        // Метод для проверки соединения с базой данных
        public static void CheckDatabaseConnection()
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open(); // Попытка открыть соединение с базой данных
                    MessageBox.Show("Подключение к базе данных успешно!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}");
                    Debug.WriteLine($"Ошибка подключения: {ex.Message}"); // Вывод ошибки в окно вывода отладки
                }
            }
        }

        public static bool CheckIfUsersTableExists()
        {
            try
            {
                using (var connection = new SQLiteConnection(GetConnection()))
                {
                    connection.Open();

                    // Запрос для проверки наличия таблицы Users
                    string query = "SELECT name FROM sqlite_master WHERE type='table' AND name='Users';";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            Console.WriteLine("Таблица Users существует.");
                            return true; // Таблица существует
                        }
                        else
                        {
                            Console.WriteLine("Таблица Users не найдена.");
                            return false; // Таблица не существует
                        }
                    }
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"Ошибка при подключении к базе данных: {ex.Message}");
                return false; // Ошибка при подключении
            }
        }

    }
}
