using System;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Windows.Forms;

namespace RestaurantMenu
{
    public class SupplierControlPage
    {
        public static void GetSuppliers(int userId)
        {
            // Получаем права пользователя
            var userPermissions = GetUserPermissions(userId);

            // Создание формы
            Form form = new Form
            {
                Text = "Управление поставщиками",
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
                            string insertQuery = @"INSERT INTO Suppliers (Name, Street_id, House_number, Last_name_director, 
                                                       First_name_director, Patronymic_director, Phone_number_director, 
                                                       Bank_id, Payment_account, Inn)
                                                   VALUES (@Name, @StreetId, @HouseNumber, @LastName, @FirstName, 
                                                           @Patronymic, @PhoneNumber, @BankId, @PaymentAccount, @Inn)";
                            using (SQLiteCommand command = new SQLiteCommand(insertQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Name", data.Name);
                                command.Parameters.AddWithValue("@StreetId", data.StreetId);
                                command.Parameters.AddWithValue("@HouseNumber", data.HouseNumber);
                                command.Parameters.AddWithValue("@LastName", data.LastName);
                                command.Parameters.AddWithValue("@FirstName", data.FirstName);
                                command.Parameters.AddWithValue("@Patronymic", data.Patronymic);
                                command.Parameters.AddWithValue("@PhoneNumber", data.PhoneNumber);
                                command.Parameters.AddWithValue("@BankId", data.BankId);
                                command.Parameters.AddWithValue("@PaymentAccount", data.PaymentAccount);
                                command.Parameters.AddWithValue("@Inn", data.Inn);
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

                ShowEditForm(GetSupplierDataById(currentId), (data) =>
                {
                    try
                    {
                        string connectionString = "Data Source=restaurants_database.db;Version=3;";
                        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            string updateQuery = @"UPDATE Suppliers 
                                                   SET Name = @Name, Street_id = @StreetId, House_number = @HouseNumber, 
                                                       Last_name_director = @LastName, First_name_director = @FirstName, 
                                                       Patronymic_director = @Patronymic, Phone_number_director = @PhoneNumber,
                                                       Bank_id = @BankId, Payment_account = @PaymentAccount, Inn = @Inn
                                                   WHERE Id = @Id";
                            using (SQLiteCommand command = new SQLiteCommand(updateQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Id", currentId);
                                command.Parameters.AddWithValue("@Name", data.Name);
                                command.Parameters.AddWithValue("@StreetId", data.StreetId);
                                command.Parameters.AddWithValue("@HouseNumber", data.HouseNumber);
                                command.Parameters.AddWithValue("@LastName", data.LastName);
                                command.Parameters.AddWithValue("@FirstName", data.FirstName);
                                command.Parameters.AddWithValue("@Patronymic", data.Patronymic);
                                command.Parameters.AddWithValue("@PhoneNumber", data.PhoneNumber);
                                command.Parameters.AddWithValue("@BankId", data.BankId);
                                command.Parameters.AddWithValue("@PaymentAccount", data.PaymentAccount);
                                command.Parameters.AddWithValue("@Inn", data.Inn);
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
                int supplierId = Convert.ToInt32(selectedRow.Cells["Id"].Value);

                DialogResult result = MessageBox.Show($"Вы уверены, что хотите удалить поставщика?", "Подтверждение удаления", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        string connectionString = "Data Source=restaurants_database.db;Version=3;";
                        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            string update1Query = "UPDATE Receipts SET Supplier_id = 0 WHERE Supplier_id = @Id";
                            using (SQLiteCommand command = new SQLiteCommand(update1Query, connection))
                            {
                                command.Parameters.AddWithValue("@Id", supplierId);
                                command.ExecuteNonQuery();
                            }
                            string update2Query = "UPDATE Stocks SET Supplier_id = 0 WHERE Supplier_id = @Id";
                            using (SQLiteCommand command = new SQLiteCommand(update2Query, connection))
                            {
                                command.Parameters.AddWithValue("@Id", supplierId);
                                command.ExecuteNonQuery();
                            }
                            string deleteQuery = "DELETE FROM Suppliers WHERE Id = @Id";
                            using (SQLiteCommand command = new SQLiteCommand(deleteQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Id", supplierId);
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

        private static SupplierData GetSupplierDataById(int id)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            SupplierData data = null;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT s.Id, s.Name, s.Street_id, s.House_number, s.Last_name_director, s.First_name_director, 
                                        s.Patronymic_director, s.Phone_number_director, s.Bank_id, s.Payment_account, s.Inn 
                                 FROM Suppliers s
                                 WHERE s.Id = @Id";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            data = new SupplierData
                            {
                                Name = reader.GetString(1),
                                StreetId = reader.GetInt32(2),
                                HouseNumber = reader.GetString(3),
                                LastName = reader.GetString(4),
                                FirstName = reader.GetString(5),
                                Patronymic = reader.GetString(6),
                                PhoneNumber = reader.GetString(7),
                                BankId = reader.GetInt32(8),
                                PaymentAccount = reader.GetString(9),
                                Inn = reader.GetString(10)
                            };
                        }
                    }
                }
            }

            return data;
        }

        private static void ShowEditForm(SupplierData data, Action<SupplierData> onSave)
        {
            // Создание формы
            Form editForm = new Form
            {
                Text = data == null ? "Добавить поставщика" : "Редактировать поставщика",
                Width = 400,
                Height = 500
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

            ComboBox bankComboBox = new ComboBox
            {
                Left = 150,
                Top = 300,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            bankComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            bankComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;

            TextBox houseNumberTextBox = new TextBox { Left = 150, Top = 100, Width = 200 };
            TextBox phoneNumberTextBox = new TextBox { Left = 150, Top = 140, Width = 200 };
            TextBox lastNameTextBox = new TextBox { Left = 150, Top = 180, Width = 200 };
            TextBox firstNameTextBox = new TextBox { Left = 150, Top = 220, Width = 200 };
            TextBox patronymicTextBox = new TextBox { Left = 150, Top = 260, Width = 200 };
            TextBox paymentAccountTextBox = new TextBox { Left = 150, Top = 340, Width = 200 };
            TextBox innTextBox = new TextBox { Left = 150, Top = 380, Width = 200 };

            // Метки
            Label[] labels = {
                new Label { Text = "Название", Left = 20, Top = 20, Width = 100 },
                new Label { Text = "Улица", Left = 20, Top = 60, Width = 100 },
                new Label { Text = "Дом", Left = 20, Top = 100, Width = 100 },
                new Label { Text = "Телефон", Left = 20, Top = 140, Width = 100 },
                new Label { Text = "Фамилия директора", Left = 20, Top = 180, Width = 120 },
                new Label { Text = "Имя директора", Left = 20, Top = 220, Width = 120 },
                new Label { Text = "Отчество директора", Left = 20, Top = 260, Width = 120 },
                new Label { Text = "Банк", Left = 20, Top = 300, Width = 100 },
                new Label { Text = "ИНН", Left = 20, Top = 340, Width = 100 },
                new Label { Text = "Расчетный счет", Left = 20, Top = 380, Width = 100 }
            };

            foreach (var label in labels)
            {
                editForm.Controls.Add(label);
            }

            editForm.Controls.Add(nameTextBox);
            editForm.Controls.Add(streetComboBox);
            editForm.Controls.Add(bankComboBox);
            editForm.Controls.Add(houseNumberTextBox);
            editForm.Controls.Add(phoneNumberTextBox);
            editForm.Controls.Add(lastNameTextBox);
            editForm.Controls.Add(firstNameTextBox);
            editForm.Controls.Add(patronymicTextBox);
            editForm.Controls.Add(paymentAccountTextBox);
            editForm.Controls.Add(innTextBox);

            // Загрузка данных в ComboBox
            LoadStreets(streetComboBox);
            LoadBanks(bankComboBox);

            // Если есть данные, заполняем их в поля
            if (data != null)
            {
                nameTextBox.Text = data.Name;
                streetComboBox.SelectedValue = data.StreetId;
                houseNumberTextBox.Text = data.HouseNumber;
                phoneNumberTextBox.Text = data.PhoneNumber;
                lastNameTextBox.Text = data.LastName;
                firstNameTextBox.Text = data.FirstName;
                patronymicTextBox.Text = data.Patronymic;
                bankComboBox.SelectedValue = data.BankId;
                paymentAccountTextBox.Text = data.PaymentAccount;
                innTextBox.Text = data.Inn;
            }

            // Кнопка сохранения
            Button saveButton = new Button { Text = "Сохранить", Left = 150, Top = 420, Width = 100 };
            saveButton.Click += (sender, args) =>
            {
                if (string.IsNullOrEmpty(nameTextBox.Text) || string.IsNullOrEmpty(houseNumberTextBox.Text) || string.IsNullOrEmpty(phoneNumberTextBox.Text) || string.IsNullOrEmpty(lastNameTextBox.Text) || string.IsNullOrEmpty(firstNameTextBox.Text) || string.IsNullOrEmpty(patronymicTextBox.Text) || string.IsNullOrEmpty(paymentAccountTextBox.Text) || string.IsNullOrEmpty(innTextBox.Text) || streetComboBox.SelectedValue == null || bankComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Заполните все поля.", "Ошибка");
                    return;
                }
                else
                {
                    // Собираем данные из полей
                    SupplierData newData = new SupplierData
                    {
                        Name = nameTextBox.Text,
                        StreetId = Convert.ToInt32(streetComboBox.SelectedValue),
                        HouseNumber = houseNumberTextBox.Text,
                        PhoneNumber = phoneNumberTextBox.Text,
                        LastName = lastNameTextBox.Text,
                        FirstName = firstNameTextBox.Text,
                        Patronymic = patronymicTextBox.Text,
                        BankId = Convert.ToInt32(bankComboBox.SelectedValue),
                        PaymentAccount = paymentAccountTextBox.Text,
                        Inn = innTextBox.Text
                    };

                    // Сохраняем данные
                    onSave(newData);
                }
                editForm.Close();
            };

            // Кнопка отмены
            Button cancelButton = new Button { Text = "Отмена", Left = 270, Top = 420, Width = 100 };
            cancelButton.Click += (sender, args) => { editForm.Close(); };

            editForm.Controls.Add(saveButton);
            editForm.Controls.Add(cancelButton);

            // Показ формы
            editForm.ShowDialog();
        }

        // Функция загрузки улиц
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

        // Функция загрузки банков
        private static void LoadBanks(ComboBox comboBox)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            DataTable banksTable = new DataTable();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Name FROM Banks";
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection))
                {
                    adapter.Fill(banksTable);
                }
            }

            comboBox.DataSource = banksTable;
            comboBox.DisplayMember = "Name";
            comboBox.ValueMember = "Id";

            // Установка пустого значения при открытии
            comboBox.SelectedIndex = -1;
        }

        private static DataTable LoadData()
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            DataTable dataTable = new DataTable();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT 
                        Suppliers.Id,
                        Suppliers.Name AS Название,
                        Streets.Name AS Улица,
                     Suppliers.House_number AS 'Номер дома',
                        Suppliers.Last_name_director AS 'Фамилия директора',
                        Suppliers.First_name_director AS Имя,
                        Suppliers.Patronymic_director AS Отчество,
                        Suppliers.Phone_number_director AS Телефон,
                        Banks.Name AS Банк,
                        Suppliers.Payment_account AS 'Расчетный счет',
                        Suppliers.Inn AS ИНН
                    FROM Suppliers
                    JOIN Streets ON Suppliers.Street_id = Streets.Id
                    JOIN Banks ON Suppliers.Bank_id = Banks.Id
                    WHERE Suppliers.Id > 0;
                 ";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }

            return dataTable;
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
                    command.Parameters.AddWithValue("@MenuId", 16);

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

    // Класс для данных поставщика
    public class SupplierData
    {
        public string Name { get; set; }
        public int StreetId { get; set; }
        public string HouseNumber { get; set; }
        public string PhoneNumber { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Patronymic { get; set; }
        public int BankId { get; set; }
        public string PaymentAccount { get; set; }
        public string Inn { get; set; }
    }
}
