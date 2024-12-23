using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Security.Cryptography;

namespace restaurants
{
    public partial class Registration : Form
    {
        public Registration()
        {
            InitializeComponent();
        }
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

        static bool CheckIfUserExists(string connectionString, string login)
        {
            string query = "SELECT COUNT(*) FROM Users WHERE Login = @login";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@login", login);

                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0; // Если количество больше 0, значит продукт существует
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(loginTextBox.Text) || string.IsNullOrEmpty(password1.Text) || string.IsNullOrEmpty(password2.Text))
            {
                MessageBox.Show("Заполните все поля.");
            }
            if (password1.Text != password2.Text)
            {
                MessageBox.Show("Пароли не совпадают!");
            }

            string login = loginTextBox.Text;
            string password = password1.Text;
            string hashedPassword = HashPassword(password);
            int userId = 0;
            string connectionString = $"Data Source={GetDatabasePath()};Version=3;";
            if (CheckIfUserExists(connectionString, login))
            {
                MessageBox.Show("Пользователь с таким логином уже существует!");
            }
            else
            {
                try
                {
                    using (var connection = GetConnection())
                    {
                        connection.Open();

                        // SQL-запрос для проверки логина и пароля
                        string query = "INSERT INTO Users (Login, Password) VALUES (@login, @password); SELECT last_insert_rowid();";
                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@login", login);
                            command.Parameters.AddWithValue("@password", hashedPassword);
                            userId = Convert.ToInt32(command.ExecuteScalar());
                        }

                        using (SQLiteTransaction transaction = connection.BeginTransaction())
                        {
                            string insertQuery = @"INSERT INTO AccessList (MenuId, UserId, Read, [Add], Edit, [Delete])
                                       VALUES (@MenuId, @UserId, @Read, @Add, @Edit, @Delete)";

                            using (SQLiteCommand command = new SQLiteCommand(insertQuery, connection, transaction))
                            {
                                // Внесем значения согласно таблице
                                var menuRights = new[]
                                {
                                new { MenuId = 1, Read = 1, Add = 1, Edit = 1, Delete = 1 },
                                new { MenuId = 2, Read = 1, Add = 1, Edit = 1, Delete = 1 },
                                new { MenuId = 3, Read = 1, Add = 1, Edit = 1, Delete = 1 },
                                new { MenuId = 4, Read = 1, Add = 1, Edit = 1, Delete = 1 },
                                new { MenuId = 5, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 6, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 7, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 8, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 9, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 10, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 11, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 12, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 13, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 14, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 15, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 16, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 17, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 18, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 19, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 20, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 21, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 22, Read = 1, Add = 1, Edit = 1, Delete = 1 },
                                new { MenuId = 23, Read = 0, Add = 0, Edit = 0, Delete = 0 },
                                new { MenuId = 24, Read = 0, Add = 0, Edit = 0, Delete = 0 }
                    };

                                foreach (var menuRight in menuRights)
                                {
                                    command.Parameters.Clear();
                                    command.Parameters.AddWithValue("@MenuId", menuRight.MenuId);
                                    command.Parameters.AddWithValue("@UserId", userId);
                                    command.Parameters.AddWithValue("@Read", menuRight.Read);
                                    command.Parameters.AddWithValue("@Add", menuRight.Add);
                                    command.Parameters.AddWithValue("@Edit", menuRight.Edit);
                                    command.Parameters.AddWithValue("@Delete", menuRight.Delete);

                                    command.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                        }
                    }
                }
                catch (SQLiteException sqlEx)
                {
                    MessageBox.Show($"Ошибка базы данных: {sqlEx.Message}");
                }
                MainWindow main = new MainWindow(userId);
                main.Show();
                this.Hide();
            }
        }
    }
}
