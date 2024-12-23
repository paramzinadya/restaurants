using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace RestaurantManagement
{
    public class RestaurantAssortmentPage
    {
        public static void GetAssortment(int userId)
        {
            // Получаем права пользователя
            var userPermissions = GetUserPermissions(userId);
            // Создание формы
            Form form = new Form
            {
                Text = "Управление ассортиментом",
                Width = 800,
                Height = 600
            };

            // Выпадающий список для выбора ресторана
            ComboBox restaurantComboBox = new ComboBox
            {
                Left = 10,
                Top = 10,
                Width = 300
            };
            form.Controls.Add(restaurantComboBox);

            // Загрузка ресторанов в выпадающий список
            DataTable restaurants = LoadRestaurants();
            restaurantComboBox.DataSource = restaurants;
            restaurantComboBox.DisplayMember = "Name";
            restaurantComboBox.ValueMember = "Id";
            restaurantComboBox.SelectedIndex = -1;

            // Создание панели для кнопок
            Panel buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Top,
                Padding = new Padding(10)
            };
            form.Controls.Add(buttonPanel);

            // Кнопки на панели
            Button addButton = new Button { Text = "Добавить", Width = 100, Left = 320, Top = 10 };
            Button editButton = new Button { Text = "Редактировать", Width = 100, Left = 430, Top = 10, Enabled = false };
            Button deleteButton = new Button { Text = "Удалить", Width = 100, Left = 540, Top = 10, Enabled = false };
            Button exitButton = new Button { Text = "Выход", Width = 100, Left = 650, Top = 10 };

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
            buttonPanel.Controls.Add(exitButton);

            // Таблица данных
            DataGridView dataGridView = new DataGridView
            {
                Location = new System.Drawing.Point(0, buttonPanel.Height + 10),
                Size = new System.Drawing.Size(form.ClientSize.Width, form.ClientSize.Height - buttonPanel.Height - 50),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                MultiSelect = false
            };
            form.Controls.Add(dataGridView);

            // Обновление данных таблицы при выборе ресторана
            restaurantComboBox.SelectedIndexChanged += (s, e) =>
            {
                if (restaurantComboBox.SelectedValue == null || !int.TryParse(restaurantComboBox.SelectedValue.ToString(), out int restaurantId))
                {
                    dataGridView.DataSource = null;
                    return;
                }

                try
                {
                    DataTable assortment = LoadAssortment(restaurantId);
                    dataGridView.DataSource = assortment;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки ассортимента: {ex.Message}", "Ошибка");
                }
            };

            // Кнопка "Добавить"
            addButton.Click += (s, e) =>
            {
                if (restaurantComboBox.SelectedValue == null || !int.TryParse(restaurantComboBox.SelectedValue.ToString(), out int restaurantId))
                {
                    MessageBox.Show("Выберите ресторан перед добавлением.", "Ошибка");
                    return;
                }

                ShowEditForm(null, 0, null, restaurantId, (name, price, groupId) =>
                {
                    try
                    {
                        AddAssortmentItem(restaurantId, name, price, groupId);
                        dataGridView.DataSource = LoadAssortment(restaurantId);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка добавления: {ex.Message}", "Ошибка");
                    }
                });
            };

            // Кнопка "Редактировать"
            editButton.Click += (s, e) =>
            {
                if (dataGridView.SelectedRows.Count > 0 && restaurantComboBox.SelectedValue != null && int.TryParse(restaurantComboBox.SelectedValue.ToString(), out int restaurantId))
                {
                    var row = dataGridView.SelectedRows[0];
                    int id = Convert.ToInt32(row.Cells["Id"].Value);
                    string name = row.Cells["Название"].Value.ToString();
                    decimal price = Convert.ToDecimal(row.Cells["Цена"].Value);
                    string groupName = row.Cells["Категория"].Value.ToString();

                    ShowEditForm(name, price, groupName, restaurantId, (newName, newPrice, newGroupId) =>
                    {
                        try
                        {
                            EditAssortmentItem(id, newName, newPrice, newGroupId);
                            dataGridView.DataSource = LoadAssortment(restaurantId);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка редактирования: {ex.Message}", "Ошибка");
                        }
                    });
                }
            };

            // Кнопка "Удалить"
            deleteButton.Click += (s, e) =>
            {
                if (dataGridView.SelectedRows.Count > 0 && restaurantComboBox.SelectedValue != null && int.TryParse(restaurantComboBox.SelectedValue.ToString(), out int restaurantId))
                {
                    var row = dataGridView.SelectedRows[0];
                    int id = Convert.ToInt32(row.Cells["Id"].Value);

                    DialogResult result = MessageBox.Show("Вы уверены, что хотите удалить эту позицию?", "Подтверждение", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            DeleteAssortmentItem(id);
                            dataGridView.DataSource = LoadAssortment(restaurantId);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
                        }
                    }
                }
            };

            // Кнопка "Выход"
            exitButton.Click += (s, e) => form.Close();

            // Обновление кнопок при выборе строки
            dataGridView.SelectionChanged += (s, e) =>
            {
                bool rowSelected = dataGridView.SelectedRows.Count > 0;
                editButton.Enabled = rowSelected;
                deleteButton.Enabled = rowSelected;
            };

            form.ShowDialog();
        }

        private static DataTable LoadRestaurants()
        {
            DataTable table = new DataTable();
            string connectionString = "Data Source=restaurants_database.db;Version=3;";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Name FROM Restaurants WHERE Id > 0";
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection))
                {
                    adapter.Fill(table);
                }
            }

            return table;
        }

        private static DataTable LoadAssortment(int restaurantId)
        {
            DataTable table = new DataTable();
            string connectionString = "Data Source=restaurants_database.db;Version=3;";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT a.Id, a.Name AS Название, a.Price AS Цена, g.Name AS Категория FROM Assortment a " +
                               "LEFT JOIN Groups g ON a.Group_id = g.Id WHERE a.Restaurant_id = @RestaurantId";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RestaurantId", restaurantId);
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }
                }
            }

            return table;
        }

        private static void AddAssortmentItem(int restaurantId, string name, decimal price, int groupId)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Assortment (Name, Price, Group_id, Restaurant_id) VALUES (@Name, @Price, @GroupId, @RestaurantId)";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Price", price);
                    command.Parameters.AddWithValue("@GroupId", groupId);
                    command.Parameters.AddWithValue("@RestaurantId", restaurantId);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void EditAssortmentItem(int id, string name, decimal price, int groupId)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE Assortment SET Name = @Name, Price = @Price, Group_id = @GroupId WHERE Id = @Id";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Price", price);
                    command.Parameters.AddWithValue("@GroupId", groupId);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void DeleteAssortmentItem(int id)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "DELETE FROM Assortment WHERE Id = @Id";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void ShowEditForm(string currentName, decimal? currentPrice, string currentGroupName, int restaurantId, Action<string, decimal, int> onSave)
        {
            Form editForm = new Form
            {
                Text = currentName == null ? "Добавить запись" : "Редактировать запись",
                Width = 300,
                Height = 250,
                StartPosition = FormStartPosition.CenterParent
            };

            Label nameLabel = new Label { Text = "Название:", Left = 10, Top = 20, Width = 80 };
            TextBox nameTextBox = new TextBox { Left = 100, Top = 20, Width = 150 };
            if (currentName != null)
                nameTextBox.Text = currentName;

            Label priceLabel = new Label { Text = "Цена:", Left = 10, Top = 60, Width = 80 };
            NumericUpDown priceNumericUpDown = new NumericUpDown
            {
                Left = 100,
                Top = 60,
                Width = 150,
                DecimalPlaces = 2,
                Maximum = 1000000,
                Minimum = 0
            };
            if (currentPrice.HasValue)
                priceNumericUpDown.Value = currentPrice.Value;

            Label groupLabel = new Label { Text = "Категория:", Left = 10, Top = 100, Width = 80 };
            ComboBox groupComboBox = new ComboBox
            {
                Left = 100,
                Top = 100,
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            groupComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            groupComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;

            Button saveButton = new Button { Text = "Сохранить", Left = 100, Top = 140, Width = 80 };

            editForm.Controls.Add(nameLabel);
            editForm.Controls.Add(nameTextBox);
            editForm.Controls.Add(priceLabel);
            editForm.Controls.Add(priceNumericUpDown);
            editForm.Controls.Add(groupLabel);
            editForm.Controls.Add(groupComboBox);
            editForm.Controls.Add(saveButton);

            saveButton.Click += (s, e) =>
            {
                string newName = nameTextBox.Text.Trim();
                decimal newPrice = priceNumericUpDown.Value;
                if (groupComboBox.SelectedValue == null || !int.TryParse(groupComboBox.SelectedValue.ToString(), out int newGroupId))
                {
                    MessageBox.Show("Выберите категорию.", "Ошибка");
                    return;
                }

                if (string.IsNullOrWhiteSpace(newName) || newPrice == 0)
                {
                    MessageBox.Show("Заполните все поля.", "Ошибка");
                    return;
                }

                onSave(newName, newPrice, newGroupId);
                editForm.Close();
            };
            LoadGroups(groupComboBox);
            if (!string.IsNullOrEmpty(currentGroupName))
            {
                int idGroup = GetGroupId(currentGroupName);
                groupComboBox.SelectedValue = idGroup;
            }

            editForm.ShowDialog();
        }

        private static void LoadGroups(ComboBox comboBox)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            DataTable table = new DataTable();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Name FROM Groups";
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection))
                {
                    adapter.Fill(table);
                }
            }
            comboBox.DataSource = table;
            comboBox.DisplayMember = "Name";
            comboBox.ValueMember = "Id";
        }

        private static int GetGroupId(string name)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            int id = 0;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id FROM Groups WHERE Name = @Name";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            id = reader.GetInt32(0);
                        }
                    }
                }
            }
            return id;
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
                    command.Parameters.AddWithValue("@MenuId", 18);

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
