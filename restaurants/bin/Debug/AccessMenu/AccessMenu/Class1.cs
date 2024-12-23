using System;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Windows.Forms;

namespace AccessMenu
{
    public class AccessControlPage
    {
        public static void GetAccess(int user_Id)
        {
            // Получаем права пользователя
            var userPermissions = GetUserPermissions(user_Id);
            Form form = new Form
            {
                Text = "Управление доступом",
                Width = 800,
                Height = 600
            };

            ComboBox userComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = 10,
                Top = 10,
                Width = 300
            };
            form.Controls.Add(userComboBox);

            Button saveButton = new Button
            {
                Text = "Сохранить",
                Left = 320,
                Top = 10,
                Width = 100,
                Enabled= false
            };

            if (userPermissions.Add == 1 || userPermissions.Edit == 1 || userPermissions.Delete == 1)
            {
                form.Controls.Add(saveButton);
            }

            DataTable users = LoadUsers();
            if (users.Rows.Count == 0)
            {
                MessageBox.Show("Пользователи не найдены в базе данных.", "Ошибка");
                return;
            }
            userComboBox.DataSource = users;
            userComboBox.DisplayMember = "Login";
            userComboBox.ValueMember = "Id";

            DataGridView accessGridView = new DataGridView
            {
                Location = new System.Drawing.Point(10, 50),
                Size = new System.Drawing.Size(760, 480),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false
            };
            form.Controls.Add(accessGridView);

            userComboBox.SelectedValueChanged += (s, e) =>
            {
                if (userComboBox.SelectedValue == null || !int.TryParse(userComboBox.SelectedValue.ToString(), out int userId))
                {
                    accessGridView.DataSource = null;
                    return;
                }

                try
                {
                    saveButton.Enabled = true;
                    accessGridView.DataSource = null;
                    accessGridView.Columns.Clear();
                    accessGridView.Rows.Clear();

                    DataTable access = LoadAccessRights(userId);
                    accessGridView.DataSource = access;

                    var columnsToReplace = new[] { "Чтение", "Добавление", "Редактирование", "Удаление" };
                    var replaceColumns = accessGridView.Columns
                        .Cast<DataGridViewColumn>()
                        .Where(c => columnsToReplace.Contains(c.Name))
                        .ToList();

                    foreach (var column in replaceColumns)
                    {
                        var checkBoxColumn = new DataGridViewCheckBoxColumn
                        {
                            DataPropertyName = column.DataPropertyName,
                            HeaderText = column.HeaderText,
                            Name = column.Name,
                            TrueValue = 1,
                            FalseValue = 0,
                            ReadOnly = false
                        };

                        accessGridView.Columns.Remove(column);
                        accessGridView.Columns.Add(checkBoxColumn);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки прав доступа: {ex.Message}", "Ошибка");
                }
            };

            saveButton.Click += (s, e) =>
            {
                try
                {
                    accessGridView.EndEdit(); // Завершение редактирования в гриде

                    if (userComboBox.SelectedValue != null && int.TryParse(userComboBox.SelectedValue.ToString(), out int userId))
                    {
                        var dataSource = accessGridView.DataSource as DataTable;
                        if (dataSource == null)
                        {
                            MessageBox.Show("Нет данных для сохранения.", "Ошибка");
                            return;
                        }

                        int affectedRows = UpdateAccessRights(userId, dataSource);
                        MessageBox.Show(affectedRows > 0
                            ? "Изменения успешно сохранены."
                            : "Нет изменений для сохранения.", "Результат");
                    }
                    else
                    {
                        MessageBox.Show("Выберите пользователя из списка.", "Ошибка");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
                }
            };


            form.ShowDialog();
        }

        private static DataTable LoadUsers()
        {
            DataTable table = new DataTable();
            string connectionString = "Data Source=restaurants_database.db;Version=3;";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Login FROM Users";
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection))
                {
                    adapter.Fill(table);
                }
            }

            return table;
        }

        private static DataTable LoadAccessRights(int userId)
        {
            DataTable table = new DataTable();
            string connectionString = "Data Source=restaurants_database.db;Version=3;";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT
                                    MenuItems.Id AS Id,
                                    MenuItems.Name AS Название,
                                    IFNULL(Read, 0) AS Чтение,
                                    IFNULL([Add], 0) AS Добавление,
                                    IFNULL(Edit, 0) AS Редактирование,
                                    IFNULL([Delete], 0) AS Удаление
                                FROM MenuItems
                                LEFT JOIN AccessList ON MenuItems.Id = AccessList.MenuId AND AccessList.UserId = @UserId";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }
                }
            }
            return table;
        }

        private static int UpdateAccessRights(int userId, DataTable accessData)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            int affectedRows = 0;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (SQLiteTransaction transaction = connection.BeginTransaction())
                {
                    foreach (DataRow row in accessData.Rows)
                    {
                        
                        // Проверяем только измененные строки
                        if (row.RowState == DataRowState.Modified)
                        {
                            string query = @"UPDATE AccessList
                                     SET Read = @Read, [Add] = @Add, Edit = @Edit, [Delete] = @Delete
                                     WHERE MenuId = @MenuId AND UserId = @UserId";

                            using (SQLiteCommand command = new SQLiteCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@MenuId", row["Id"]);
                                command.Parameters.AddWithValue("@UserId", userId);
                                command.Parameters.AddWithValue("@Read", row["Чтение"]);
                                command.Parameters.AddWithValue("@Add", row["Добавление"]);
                                command.Parameters.AddWithValue("@Edit", row["Редактирование"]);
                                command.Parameters.AddWithValue("@Delete", row["Удаление"]);

                                affectedRows += command.ExecuteNonQuery();
                            }
                        }
                    }

                    transaction.Commit();
                }
            }

            return affectedRows;
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
                    command.Parameters.AddWithValue("@MenuId", 5);

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
