using System;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Windows.Forms;

namespace RestaurantMenu
{
    public class RestaurantControlPage
    {
        public static void GetRestaurants(int userId)
        {
            // Получаем права пользователя
            var userPermissions = GetUserPermissions(userId);

            // Создание формы
            Form form = new Form
            {
                Text = "Управление ресторанами",
                Width = 800,
                Height = 600
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
                ShowEditForm(null, (data) =>
                {
                    try
                    {
                        string connectionString = "Data Source=restaurants_database.db;Version=3;";
                        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            string insertQuery = @"INSERT INTO Restaurants (Name, Street_id, House_number, Phone_number, 
                                                       Last_name_director, First_name_director, Patronymic_director)
                                                   VALUES (@Name, @StreetId, @HouseNumber, @PhoneNumber, @LastName, 
                                                           @FirstName, @Patronymic)";
                            using (SQLiteCommand command = new SQLiteCommand(insertQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Name", data.Name);
                                command.Parameters.AddWithValue("@StreetId", data.StreetId);
                                command.Parameters.AddWithValue("@HouseNumber", data.HouseNumber);
                                command.Parameters.AddWithValue("@PhoneNumber", data.PhoneNumber);
                                command.Parameters.AddWithValue("@LastName", data.LastName);
                                command.Parameters.AddWithValue("@FirstName", data.FirstName);
                                command.Parameters.AddWithValue("@Patronymic", data.Patronymic);
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
                int currentId = Convert.ToInt32(selectedRow.Cells["Id"].Value);

                ShowEditForm(GetRestaurantDataById(currentId), (data) =>
                {
                    try
                    {
                        string connectionString = "Data Source=restaurants_database.db;Version=3;";
                        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            string updateQuery = @"UPDATE Restaurants 
                                                   SET Name = @Name, Street_id = @StreetId, House_number = @HouseNumber, 
                                                       Phone_number = @PhoneNumber, Last_name_director = @LastName, 
                                                       First_name_director = @FirstName, Patronymic_director = @Patronymic
                                                   WHERE Id = @Id";
                            using (SQLiteCommand command = new SQLiteCommand(updateQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Id", currentId);
                                command.Parameters.AddWithValue("@Name", data.Name);
                                command.Parameters.AddWithValue("@StreetId", data.StreetId);
                                command.Parameters.AddWithValue("@HouseNumber", data.HouseNumber);
                                command.Parameters.AddWithValue("@PhoneNumber", data.PhoneNumber);
                                command.Parameters.AddWithValue("@LastName", data.LastName);
                                command.Parameters.AddWithValue("@FirstName", data.FirstName);
                                command.Parameters.AddWithValue("@Patronymic", data.Patronymic);
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
                int restaurantId = Convert.ToInt32(selectedRow.Cells["Id"].Value);

                DialogResult result = MessageBox.Show($"Вы уверены, что хотите удалить ресторан?", "Подтверждение удаления", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        string connectionString = "Data Source=restaurants_database.db;Version=3;";
                        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            string update1Query = "UPDATE Requests SET Restaurant_id = 0 WHERE Restaurant_id = @Id";
                            using (SQLiteCommand command = new SQLiteCommand(update1Query, connection))
                            {
                                command.Parameters.AddWithValue("@Id", restaurantId);
                                command.ExecuteNonQuery();
                            }
                            string update2Query = "UPDATE Revenue SET Restaurant_id = 0 WHERE Restaurant_id = @Id";
                            using (SQLiteCommand command = new SQLiteCommand(update2Query, connection))
                            {
                                command.Parameters.AddWithValue("@Id", restaurantId);
                                command.ExecuteNonQuery();
                            }
                            string delete1Query = "DELETE FROM Assortment WHERE Restaurant_id = @Id";
                            using (SQLiteCommand command = new SQLiteCommand(delete1Query, connection))
                            {
                                command.Parameters.AddWithValue("@Id", restaurantId);
                                command.ExecuteNonQuery();
                            }
                            string delete2Query = "DELETE FROM Restaurants WHERE Id = @Id";
                            using (SQLiteCommand command = new SQLiteCommand(delete2Query, connection))
                            {
                                command.Parameters.AddWithValue("@Id", restaurantId);
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

        private static RestaurantData GetRestaurantDataById(int id)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            RestaurantData data = null;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT r.Id, r.Name, r.Street_id, r.House_number, r.Phone_number, 
                                        r.Last_name_director, r.First_name_director, r.Patronymic_director 
                                 FROM Restaurants r
                                 WHERE r.Id = @Id";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            data = new RestaurantData
                            {
                                Name = reader.GetString(1),
                                StreetId = reader.GetInt32(2),
                                HouseNumber = reader.GetString(3),
                                PhoneNumber = reader.GetString(4),
                                LastName = reader.GetString(5),
                                FirstName = reader.GetString(6),
                                Patronymic = reader.GetString(7)
                            };
                        }
                    }
                }
            }

            return data;
        }

        private static void ShowEditForm(RestaurantData data, Action<RestaurantData> onSave)
        {
            // Создание формы
            Form editForm = new Form
            {
                Text = data == null ? "Добавить ресторан" : "Редактировать ресторан",
                Width = 400,
                Height = 400
            };

            // Поля для ввода данных
            TextBox nameTextBox = new TextBox { Left = 150, Top = 20, Width = 200 };
            ComboBox streetComboBox = new ComboBox
            {
                Left = 150,
                Top = 60,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            streetComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            streetComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;

            TextBox houseNumberTextBox = new TextBox { Left = 150, Top = 100, Width = 200 };
            TextBox phoneNumberTextBox = new TextBox { Left = 150, Top = 140, Width = 200 };
            TextBox lastNameTextBox = new TextBox { Left = 150, Top = 180, Width = 200 };
            TextBox firstNameTextBox = new TextBox { Left = 150, Top = 220, Width = 200 };
            TextBox patronymicTextBox = new TextBox { Left = 150, Top = 260, Width = 200 };

            // Метки
            Label[] labels = {
            new Label { Text = "Название", Left = 20, Top = 20, Width = 100 },
            new Label { Text = "Улица", Left = 20, Top = 60, Width = 100 },
            new Label { Text = "Дом", Left = 20, Top = 100, Width = 100 },
            new Label { Text = "Телефон", Left = 20, Top = 140, Width = 100 },
            new Label { Text = "Фамилия директора", Left = 20, Top = 180, Width = 120 },
            new Label { Text = "Имя директора", Left = 20, Top = 220, Width = 120 },
            new Label { Text = "Отчество директора", Left = 20, Top = 260, Width = 120 }
            };

            // Добавление меток и текстовых полей
            editForm.Controls.AddRange(labels);
            editForm.Controls.Add(nameTextBox);
            editForm.Controls.Add(streetComboBox);
            editForm.Controls.Add(houseNumberTextBox);
            editForm.Controls.Add(phoneNumberTextBox);
            editForm.Controls.Add(lastNameTextBox);
            editForm.Controls.Add(firstNameTextBox);
            editForm.Controls.Add(patronymicTextBox);

            // Кнопки
            Button saveButton = new Button { Text = "Сохранить", Left = 150, Top = 320, Width = 100 };
            Button cancelButton = new Button { Text = "Отмена", Left = 260, Top = 320, Width = 100 };

            saveButton.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(nameTextBox.Text) || string.IsNullOrWhiteSpace(houseNumberTextBox.Text) || string.IsNullOrWhiteSpace(phoneNumberTextBox.Text) || string.IsNullOrWhiteSpace(lastNameTextBox.Text) || string.IsNullOrWhiteSpace(firstNameTextBox.Text) || string.IsNullOrWhiteSpace(patronymicTextBox.Text) || streetComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Заполните все поля.", "Ошибка");
                    return;
                }

                var result = new RestaurantData
                {
                    Name = nameTextBox.Text,
                    StreetId = Convert.ToInt32(streetComboBox.SelectedValue),
                    HouseNumber = houseNumberTextBox.Text,
                    PhoneNumber = phoneNumberTextBox.Text,
                    LastName = lastNameTextBox.Text,
                    FirstName = firstNameTextBox.Text,
                    Patronymic = patronymicTextBox.Text
                };

                onSave(result); // Передаем данные в вызывающий код
                editForm.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                editForm.Close();
            };

            editForm.Controls.Add(saveButton);
            editForm.Controls.Add(cancelButton);

            // Заполнение данными для редактирования (если есть)
            if (data != null)
            {
                nameTextBox.Text = data.Name;
                houseNumberTextBox.Text = data.HouseNumber;
                phoneNumberTextBox.Text = data.PhoneNumber;
                lastNameTextBox.Text = data.LastName;
                firstNameTextBox.Text = data.FirstName;
                patronymicTextBox.Text = data.Patronymic;
            }

            // Загрузка данных для ComboBox (улицы)
            LoadStreets(streetComboBox);

            // Установка текущего значения улицы для редактирования
            if (data != null)
            {
                streetComboBox.SelectedValue = data.StreetId;
            }

            editForm.ShowDialog();
        }

        private static void LoadStreets(ComboBox comboBox)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            DataTable streetsTable = new DataTable();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Name FROM Streets";
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection))
                {
                    adapter.Fill(streetsTable);
                }
            }

            comboBox.DataSource = streetsTable;
            comboBox.DisplayMember = "Name";
            comboBox.ValueMember = "Id";

            // Установка пустого значения при открытии
            comboBox.SelectedIndex = -1;
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
                    string query = @"
                SELECT 
                    r.Id, 
                    r.Name AS Название, 
                    s.Name AS Улица, 
                    r.House_number AS 'Номер дома', 
                    r.Phone_number AS Телефон, 
                    r.Last_name_director AS Фамилия, 
                    r.First_name_director AS Имя, 
                    r.Patronymic_director AS Отчество
                FROM Restaurants r
                JOIN Streets s ON r.Street_id = s.Id
                WHERE r.Id > 0";

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
                    command.Parameters.AddWithValue("@MenuId", 15);

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

    public class RestaurantData
    {
        public string Name { get; set; }
        public int StreetId { get; set; }
        public string HouseNumber { get; set; }
        public string PhoneNumber { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Patronymic { get; set; }
    }
}
