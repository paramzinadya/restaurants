using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace StreetMenu
{
    public class StreetControlPage
    {
        public static void GetData(int userId)
        {
            // Получаем права пользователя
            var userPermissions = GetUserPermissions(userId);

            // Создание формы
            Form form = new Form
            {
                Text = "Управление улицами",
                Width = 600,
                Height = 400
            };

            // Создание панели для кнопок
            Panel buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Top
            };
            form.Controls.Add(buttonPanel);

            // Кнопки на панели
            Button addButton = new Button { Text = "Добавить", Width = 100, Left = 10, Top = 10 };
            Button editButton = new Button { Text = "Редактировать", Width = 100, Left = 120, Top = 10, Enabled = false };
            Button deleteButton = new Button { Text = "Удалить", Width = 100, Left = 230, Top = 10, Enabled = false };
            Button exitButton = new Button { Text = "Выход", Width = 100, Left = 450, Top = 10 };

            // Проверка прав и добавление кнопок
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

            // Кнопка "Выход" доступна всегда
            buttonPanel.Controls.Add(exitButton);

            // Таблица данных
            DataGridView dataGridView = new DataGridView
            {
                Location = new System.Drawing.Point(0, buttonPanel.Height),
                Size = new System.Drawing.Size(form.ClientSize.Width, form.ClientSize.Height - buttonPanel.Height),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                MultiSelect = false // Позволяем выбрать только одну строку
            };
            form.Controls.Add(dataGridView);

            // Загрузка данных в таблицу
            DataTable table = LoadData();
            dataGridView.DataSource = table;

            // Кнопка "Добавить"
            addButton.Click += (addSender, addArgs) =>
            {
                ShowEditForm(null, (name) =>
                {
                    try
                    {
                        string connectionString = "Data Source=restaurants_database.db;Version=3;";
                        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            string insertQuery = "INSERT INTO Streets (Name) VALUES (@Name)";
                            using (SQLiteCommand command = new SQLiteCommand(insertQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Name", name);
                                command.ExecuteNonQuery();
                            }
                        }

                        // Обновляем таблицу
                        table = LoadData();
                        dataGridView.DataSource = table;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка добавления данных: {ex.Message}", "Ошибка");
                    }
                });
            };

            // Кнопка "Редактировать"
            editButton.Click += (editSender, editArgs) =>
            {
                if (dataGridView.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Выберите строку для редактирования.", "Ошибка");
                    return;
                }

                DataGridViewRow selectedRow = dataGridView.SelectedRows[0];
                string currentId = selectedRow.Cells["Id"].Value.ToString();
                string currentName = selectedRow.Cells["Название"].Value.ToString();

                ShowEditForm(currentName, (newName) =>
                {
                    try
                    {
                        string connectionString = "Data Source=restaurants_database.db;Version=3;";
                        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            string updateQuery = "UPDATE Streets SET Name = @Name WHERE Id = @Id";
                            using (SQLiteCommand command = new SQLiteCommand(updateQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Id", currentId);
                                command.Parameters.AddWithValue("@Name", newName);
                                command.ExecuteNonQuery();
                            }
                        }

                        // Обновляем таблицу
                        table = LoadData();
                        dataGridView.DataSource = table;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка редактирования данных: {ex.Message}", "Ошибка");
                    }
                });
            };

            // Кнопка "Удалить"
            deleteButton.Click += (deleteSender, deleteArgs) =>
            {
                if (dataGridView.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Выберите строку для удаления.", "Информация");
                    return;
                }

                DataGridViewRow selectedRow = dataGridView.SelectedRows[0];
                string streetName = selectedRow.Cells["Название"].Value?.ToString();
                string streetId = selectedRow.Cells["Id"].Value?.ToString();

                DialogResult result = MessageBox.Show($"Вы уверены, что хотите удалить '{streetName}'?", "Подтверждение удаления", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        string connectionString = "Data Source=restaurants_database.db;Version=3;";
                        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            string update1Query = "UPDATE Suppliers SET Street_id = 0 WHERE Street_id = @Id";
                            using (SQLiteCommand command = new SQLiteCommand(update1Query, connection))
                            {
                                command.Parameters.AddWithValue("@Id", streetId);
                                command.ExecuteNonQuery();
                            }
                            string update2Query = "UPDATE Restaurants SET Street_id = 0 WHERE Street_id = @Id";
                            using (SQLiteCommand command = new SQLiteCommand(update2Query, connection))
                            {
                                command.Parameters.AddWithValue("@Id", streetId);
                                command.ExecuteNonQuery();
                            }
                            string deleteQuery = "DELETE FROM Streets WHERE Id = @Id";
                            using (SQLiteCommand command = new SQLiteCommand(deleteQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Id", streetId);
                                command.ExecuteNonQuery();
                            }
                        }

                        // Обновляем таблицу
                        table = LoadData();
                        dataGridView.DataSource = table;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении данных: {ex.Message}", "Ошибка");
                    }
                }
            };

            // Включение кнопок "Редактировать" и "Удалить" при выборе строки
            dataGridView.SelectionChanged += (s, e) =>
            {
                bool rowSelected = dataGridView.SelectedRows.Count > 0;
                editButton.Enabled = rowSelected;
                deleteButton.Enabled = rowSelected;
            };

            // Кнопка "Выход"
            exitButton.Click += (exitSender, exitArgs) =>
            {
                form.Close();
            };

            form.ShowDialog();
        }

        private static void ShowEditForm(string currentName, Action<string> onSave)
        {
            Form editForm = new Form
            {
                Text = currentName == null ? "Добавить запись" : "Редактировать запись",
                Width = 300,
                Height = 150,
                StartPosition = FormStartPosition.CenterParent
            };

            Label nameLabel = new Label { Text = "Название:", Left = 10, Top = 20, Width = 80 };
            TextBox nameTextBox = new TextBox { Left = 100, Top = 20, Width = 150 };
            if (currentName != null)
                nameTextBox.Text = currentName;

            Button saveButton = new Button { Text = "Сохранить", Left = 100, Top = 60, Width = 80 };

            editForm.Controls.Add(nameLabel);
            editForm.Controls.Add(nameTextBox);
            editForm.Controls.Add(saveButton);

            saveButton.Click += (s, e) =>
            {
                string newName = nameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(newName))
                {
                    MessageBox.Show("Поле 'Название' не может быть пустым.", "Ошибка");
                    return;
                }

                onSave(newName);
                editForm.Close();
            };

            editForm.ShowDialog();
        }

        private static DataTable LoadData()
        {
            DataTable table = new DataTable();
            string connectionString = "Data Source=restaurants_database.db;Version=3;";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT Id, Name AS Название FROM Streets WHERE Id > 0";

                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection))
                    {
                        adapter.Fill(table);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка");
                }
            }

            return table;
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
                    command.Parameters.AddWithValue("@MenuId", 7);

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
