using System;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace PasswordChange
{
    public class PasswordChangeForm
    {
        public static void ChangePassword(int userId)
        {
            // Создание формы
            Form form = new Form
            {
                Text = "Смена пароля",
                Width = 350,
                Height = 300,
                StartPosition = FormStartPosition.CenterScreen
            };

            // Метки и поля ввода
            Label lblOldPassword = new Label { Text = "Старый пароль", Left = 10, Top = 20, Width = 120 };
            TextBox txtOldPassword = new TextBox { Left = 140, Top = 20, Width = 150, UseSystemPasswordChar = true };
            Button btnToggleOldPassword = new Button { Text = "👁️", Left = 300, Top = 20, Width = 30, Height = 22 };

            Label lblNewPassword = new Label { Text = "Новый пароль", Left = 10, Top = 60, Width = 120 };
            TextBox txtNewPassword = new TextBox { Left = 140, Top = 60, Width = 150, UseSystemPasswordChar = true };
            Button btnToggleNewPassword = new Button { Text = "👁️", Left = 300, Top = 60, Width = 30, Height = 22 };

            Label lblRepeatPassword = new Label { Text = "Повторите пароль", Left = 10, Top = 100, Width = 120 };
            TextBox txtRepeatPassword = new TextBox { Left = 140, Top = 100, Width = 150, UseSystemPasswordChar = true };
            Button btnToggleRepeatPassword = new Button { Text = "👁️", Left = 300, Top = 100, Width = 30, Height = 22 };

            // Кнопка сохранения
            Button btnSave = new Button { Text = "Сохранить", Left = 140, Top = 150, Width = 150 };

            form.Controls.Add(lblOldPassword);
            form.Controls.Add(txtOldPassword);
            form.Controls.Add(btnToggleOldPassword);
            form.Controls.Add(lblNewPassword);
            form.Controls.Add(txtNewPassword);
            form.Controls.Add(btnToggleNewPassword);
            form.Controls.Add(lblRepeatPassword);
            form.Controls.Add(txtRepeatPassword);
            form.Controls.Add(btnToggleRepeatPassword);
            form.Controls.Add(btnSave);

            // Обработчики кнопок показа/скрытия пароля
            btnToggleOldPassword.Click += (sender, args) =>
            {
                txtOldPassword.UseSystemPasswordChar = !txtOldPassword.UseSystemPasswordChar;
            };
            btnToggleNewPassword.Click += (sender, args) =>
            {
                txtNewPassword.UseSystemPasswordChar = !txtNewPassword.UseSystemPasswordChar;
            };
            btnToggleRepeatPassword.Click += (sender, args) =>
            {
                txtRepeatPassword.UseSystemPasswordChar = !txtRepeatPassword.UseSystemPasswordChar;
            };

            // Обработчик кнопки сохранения
            btnSave.Click += (sender, args) =>
            {
                string oldPassword = txtOldPassword.Text;
                string newPassword = txtNewPassword.Text;
                string repeatPassword = txtRepeatPassword.Text;

                if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(repeatPassword))
                {
                    MessageBox.Show("Все поля должны быть заполнены.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (newPassword != repeatPassword)
                {
                    MessageBox.Show("Новый пароль и подтверждение не совпадают.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    string connectionString = "Data Source=restaurants_database.db;Version=3;";
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();

                        // Проверка старого пароля
                        string checkPasswordQuery = "SELECT Password FROM Users WHERE Id = @UserId";
                        using (SQLiteCommand command = new SQLiteCommand(checkPasswordQuery, connection))
                        {
                            command.Parameters.AddWithValue("@UserId", userId);
                            string currentPasswordHash = command.ExecuteScalar()?.ToString();

                            if (currentPasswordHash != GetMd5Hash(oldPassword))
                            {
                                MessageBox.Show("Старый пароль неверен.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }

                        // Обновление пароля
                        string updatePasswordQuery = "UPDATE Users SET Password = @NewPassword WHERE Id = @UserId";
                        using (SQLiteCommand command = new SQLiteCommand(updatePasswordQuery, connection))
                        {
                            command.Parameters.AddWithValue("@NewPassword", GetMd5Hash(newPassword));
                            command.Parameters.AddWithValue("@UserId", userId);
                            command.ExecuteNonQuery();
                        }

                        MessageBox.Show("Пароль успешно изменён.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        form.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка изменения пароля: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            form.ShowDialog();
        }

        private static string GetMd5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}