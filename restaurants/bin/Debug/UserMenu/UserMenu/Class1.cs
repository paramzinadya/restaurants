using System;
using System.Data;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace RestaurantNetwork
{
    public class UserManagementPage
    {
        public static void GetUsers(int userI)
        {
            var userPermissions = GetUserPermissions(userI);
            Form form = new Form
            {
                Text = "Управление пользователями",
                Width = 800,
                Height = 600
            };

            Panel buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Top
            };
            form.Controls.Add(buttonPanel);

            Button addButton = new Button { Text = "Добавить", Width = 100, Left = 10, Top = 10 };
            Button editButton = new Button { Text = "Редактировать", Width = 100, Left = 120, Top = 10, Enabled = false };
            Button deleteButton = new Button { Text = "Удалить", Width = 100, Left = 230, Top = 10, Enabled = false };
            Button exitButton = new Button { Text = "Выход", Width = 100, Left = 340, Top = 10 };

            if (userPermissions.Add == 1)
            {
                buttonPanel.Controls.Add(addButton);
            }

            if (userPermissions.Edit == 1)
            {
                buttonPanel.Controls.Add(editButton);
            }

            if (userPermissions.Delete == 1)
            {
                buttonPanel.Controls.Add(deleteButton);
            }

            buttonPanel.Controls.Add(exitButton);

            DataGridView dataGridView = new DataGridView
            {
                Location = new System.Drawing.Point(0, buttonPanel.Height),
                Size = new System.Drawing.Size(form.ClientSize.Width, form.ClientSize.Height - buttonPanel.Height),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                MultiSelect = false
            };
            form.Controls.Add(dataGridView);

            DataTable table = LoadUsers();
            dataGridView.DataSource = table;

            addButton.Click += (s, e) =>
            {
                ShowAddForm();
                RefreshTable(dataGridView);
            };

            editButton.Click += (s, e) =>
            {
                if (dataGridView.SelectedRows.Count == 0) return;

                DataGridViewRow row = dataGridView.SelectedRows[0];
                int userId = Convert.ToInt32(row.Cells["Id"].Value);
                string currentLogin = row.Cells["Логин"].Value.ToString();

                ShowEditForm(userId, currentLogin);
                RefreshTable(dataGridView);
            };

            deleteButton.Click += (s, e) =>
            {
                if (dataGridView.SelectedRows.Count == 0) return;

                DataGridViewRow row = dataGridView.SelectedRows[0];
                int userId = Convert.ToInt32(row.Cells["Id"].Value);

                DialogResult result = MessageBox.Show("Вы уверены, что хотите удалить пользователя?", "Подтверждение удаления", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        string connectionString = "Data Source=restaurants_database.db;Version=3;";

                        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            string deleteAccessListQuery = "DELETE FROM AccessList WHERE UserId = @UserId";

                            using (SQLiteCommand command = new SQLiteCommand(deleteAccessListQuery, connection))
                            {
                                command.Parameters.AddWithValue("@UserId", userId);
                                command.ExecuteNonQuery(); // Важно: исполняем команду
                            }

                            string deleteUserQuery = "DELETE FROM Users WHERE Id = @Id";
                            using (SQLiteCommand command = new SQLiteCommand(deleteUserQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Id", userId);
                                command.ExecuteNonQuery(); // Важно: исполняем команду
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления пользователя: {ex.Message}", "Ошибка");
                    }
                    RefreshTable(dataGridView);
                }
            };

            dataGridView.SelectionChanged += (s, e) =>
            {
                bool rowSelected = dataGridView.SelectedRows.Count > 0;
                editButton.Enabled = rowSelected;
                deleteButton.Enabled = rowSelected;
            };

            exitButton.Click += (s, e) => form.Close();

            form.ShowDialog();
        }

        private static void ShowAddForm()
        {
            Form editForm = new Form
            {
                Text = "Добавить пользователя",
                Width = 300,
                Height = 200,
                StartPosition = FormStartPosition.CenterParent
            };

            Label loginLabel = new Label { Text = "Логин:", Left = 10, Top = 20, Width = 80 };
            TextBox loginTextBox = new TextBox { Left = 100, Top = 20, Width = 150 };

            Label passwordLabel = new Label { Text = "Пароль:", Left = 10, Top = 60, Width = 80 };
            TextBox passwordTextBox = new TextBox { Left = 100, Top = 60, Width = 150, UseSystemPasswordChar = true };

            Button saveButton = new Button { Text = "Сохранить", Left = 100, Top = 100, Width = 80 };

            editForm.Controls.Add(loginLabel);
            editForm.Controls.Add(loginTextBox);
            editForm.Controls.Add(passwordLabel);
            editForm.Controls.Add(passwordTextBox);
            editForm.Controls.Add(saveButton);

            saveButton.Click += (s, e) =>
            {
                string newLogin = loginTextBox.Text.Trim();
                string newPassword = passwordTextBox.Text;
                if (string.IsNullOrWhiteSpace(newLogin))
                {
                    MessageBox.Show("Логин не может быть пустым", "Ошибка");
                    return;
                }
                string hashedPassword = HashPassword(newPassword);

                string connectionString = "Data Source=restaurants_database.db;Version=3;";

                if (CheckIfUserExists(connectionString, newLogin))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует!");
                }
                else
                {
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        string query = "INSERT INTO Users (Login, Password) VALUES (@Login, @Password); SELECT last_insert_rowid();";
                        int newUserId;
                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Login", newLogin);
                            command.Parameters.AddWithValue("@Password", hashedPassword);
                            newUserId = Convert.ToInt32(command.ExecuteScalar());
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
                                    command.Parameters.AddWithValue("@UserId", newUserId);
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

                
                editForm.Close();
            };

            editForm.ShowDialog();
        }
        private static void ShowEditForm(int userId, string login)
        {
            Form editForm = new Form
            {
                Text = "Редактировать пользователя",
                Width = 300,
                Height = 200,
                StartPosition = FormStartPosition.CenterParent
            };

            Label loginLabel = new Label { Text = "Логин:", Left = 10, Top = 20, Width = 80 };
            TextBox loginTextBox = new TextBox { Left = 100, Top = 20, Width = 150 };
            loginTextBox.Text = login;

            Button saveButton = new Button { Text = "Сохранить", Left = 100, Top = 100, Width = 80 };

            editForm.Controls.Add(loginLabel);
            editForm.Controls.Add(loginTextBox);
            editForm.Controls.Add(saveButton);

            saveButton.Click += (s, e) =>
            {
                string newLogin = loginTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(newLogin))
                {
                    MessageBox.Show("Логин не может быть пустым", "Ошибка");
                    return;
                }
                string connectionString = "Data Source=restaurants_database.db;Version=3;";
                if (CheckIfUserExists(connectionString, login))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует!");
                }
                else
                {
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        string query = "UPDATE Users SET Login = @Login WHERE Id = @Id";

                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Id", userId);
                            command.Parameters.AddWithValue("@Login", newLogin);
                            command.ExecuteNonQuery();
                        }
                    }
                }
                editForm.Close();
            };

            editForm.ShowDialog();
        }

        private static DataTable LoadUsers()
        {
            DataTable table = new DataTable();
            ExecuteQuery("SELECT Id, Login AS Логин FROM Users", (adapter) => adapter.Fill(table));
            return table;
        }

        private static void RefreshTable(DataGridView gridView)
        {
            gridView.DataSource = LoadUsers();
        }

        private static void ExecuteQuery(string query, Action<SQLiteDataAdapter> action)
        {
            using (SQLiteConnection connection = new SQLiteConnection("Data Source=restaurants_database.db;Version=3;"))
            {
                connection.Open();
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection))
                {
                    action(adapter);
                }
            }
        }

        private static string HashPassword(string password)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
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

        private static (int Add, int Edit, int Delete) GetUserPermissions(int userId)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            int add = 0, edit = 0, delete = 0;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT [Add], Edit, [Delete] FROM AccessList WHERE UserId = @UserId AND MenuId = @MenuId";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@MenuId", 23);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            add = reader.GetInt32(0);
                            edit = reader.GetInt32(1);
                            delete = reader.GetInt32(2);
                        }
                    }
                }
            }

            return (add, edit, delete);
        }
    }
}
