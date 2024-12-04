using System;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace restaurants
{
    public static class UserAuthenticator
    {
        // Метод для получения соединения с базой данных
        private static SQLiteConnection GetConnection()
        {
            string connectionString = $"Data Source={GetDatabasePath()};Version=3;";
            return new SQLiteConnection(connectionString);
        }

        // Получаем путь к базе данных
        private static string GetDatabasePath()
        {
            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string databaseRelativePath = "restaurants_database.db"; // Путь к базе данных
            return System.IO.Path.Combine(projectDirectory, databaseRelativePath);
        }

        // Метод для хеширования пароля
        private static string HashPassword(string password)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(password);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        // Метод для проверки логина и пароля
        public static bool Authenticate(string login, string password)
        {
            string hashedPassword = HashPassword(password); // Хешируем введённый пароль

            try
            {
                using (var connection = GetConnection())
                {
                    connection.Open();

                    // SQL-запрос для проверки логина и пароля
                    string query = "SELECT COUNT(*) FROM Users WHERE Login = @login AND Password = @password";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", hashedPassword);

                        var result = command.ExecuteScalar();

                        // Проверка, что результат не null
                        int count = result != DBNull.Value ? Convert.ToInt32(result) : 0;

                        // Если запись найдена (count > 0), значит логин и пароль верны
                        return count > 0;
                    }
                }
            }
            catch (SQLiteException sqlEx)
            {
                MessageBox.Show($"Ошибка базы данных: {sqlEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Неизвестная ошибка: {ex.Message}");
                return false;
            }
        }
    }
}
