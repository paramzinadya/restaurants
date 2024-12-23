using System;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace StockManagement
{
    public class StockControlPage
    {
        public static void GetStocks(int userId)
        {
            // Получаем права пользователя
            var userPermissions = GetUserPermissions(userId);

            // Создание формы
            Form form = new Form
            {
                Text = "Управление запасами",
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
                MultiSelect = false
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
                            string insertQuery = @"INSERT INTO Stocks (Product_id, Measurement_unit_id, Price, Remains, Supplier_id) 
                                                   VALUES (@ProductId, @MeasurementUnitId, @Price, @Remains, @SupplierId)";
                            using (SQLiteCommand command = new SQLiteCommand(insertQuery, connection))
                            {
                                command.Parameters.AddWithValue("@ProductId", data.ProductId);
                                command.Parameters.AddWithValue("@MeasurementUnitId", data.MeasurementUnitId);
                                command.Parameters.AddWithValue("@Price", data.Price);
                                command.Parameters.AddWithValue("@Remains", data.Remains);
                                command.Parameters.AddWithValue("@SupplierId", data.SupplierId);
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
                int productId = Convert.ToInt32(selectedRow.Cells["ID Продукта"].Value);


                ShowEditForm(GetStockDataById(productId), (data) =>
                {
                    try
                    {
                        string connectionString = "Data Source=restaurants_database.db;Version=3;";
                        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            string updateQuery = @"UPDATE Stocks 
                                                   SET Measurement_unit_id = @MeasurementUnitId, Price = @Price, 
                                                       Remains = @Remains, Supplier_id = @SupplierId 
                                                   WHERE Product_id = @ProductId";
                            using (SQLiteCommand command = new SQLiteCommand(updateQuery, connection))
                            {
                                command.Parameters.AddWithValue("@ProductId", productId);
                                command.Parameters.AddWithValue("@MeasurementUnitId", data.MeasurementUnitId);
                                command.Parameters.AddWithValue("@Price", data.Price);
                                command.Parameters.AddWithValue("@Remains", data.Remains);
                                command.Parameters.AddWithValue("@SupplierId", data.SupplierId);
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
                int productId = Convert.ToInt32(selectedRow.Cells["ID Продукта"].Value);

                DialogResult result = MessageBox.Show($"Вы уверены, что хотите удалить запись?", "Подтверждение удаления", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        string connectionString = "Data Source=restaurants_database.db;Version=3;";
                        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            string deleteQuery = "DELETE FROM Stocks WHERE Product_id = @ProductId";
                            using (SQLiteCommand command = new SQLiteCommand(deleteQuery, connection))
                            {
                                command.Parameters.AddWithValue("@ProductId", productId);
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

        private static StockData GetStockDataById(int productId)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            StockData data = null;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT Product_id, Measurement_unit_id, Price, Remains, Supplier_id 
                                 FROM Stocks
                                 WHERE Product_id = @ProductId";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", productId);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            data = new StockData
                            {
                                ProductId = reader.GetInt32(0),
                                MeasurementUnitId = reader.GetInt32(1),
                                Price = reader.GetDouble(2),
                                Remains = reader.GetInt32(3),
                                SupplierId = reader.GetInt32(4)
                            };
                        }
                    }
                }
            }
            return data;
        }

        private static void ShowEditForm(StockData data, Action<StockData> onSave)
        {
            bool dataIsNull = data == null;
            Form editForm = new Form
            {
                Text = data == null ? "Добавить запись" : "Редактировать запись",
                Width = 400,
                Height = 400,
                StartPosition = FormStartPosition.CenterParent
            };

            Label productLabel = new Label { Text = "Продукт", Top = 20, Left = 20, Width = 120 };
            ComboBox productComboBox = new ComboBox { Top = 20, Left = 150, Width = 200, DropDownStyle = ComboBoxStyle.DropDown };
            productComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            productComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;

            Label unitLabel = new Label { Text = "Ед. измерения", Top = 60, Left = 20, Width = 120 };
            ComboBox unitComboBox = new ComboBox { Top = 60, Left = 150, Width = 200, DropDownStyle = ComboBoxStyle.DropDown };
            unitComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            unitComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;

            Label priceLabel = new Label { Text = "Цена", Top = 100, Left = 20, Width = 120 };
            TextBox priceTextBox = new TextBox { Top = 100, Left = 150, Width = 200 };

            Label remainsLabel = new Label { Text = "Остаток", Top = 140, Left = 20, Width = 120 };
            TextBox remainsTextBox = new TextBox { Top = 140, Left = 150, Width = 200 };

            Label supplierLabel = new Label { Text = "Поставщик", Top = 180, Left = 20, Width = 120 };
            ComboBox supplierComboBox = new ComboBox { Top = 180, Left = 150, Width = 200, DropDownStyle = ComboBoxStyle.DropDown };
            supplierComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            supplierComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;

            Button saveButton = new Button { Text = "Сохранить", Top = 240, Left = 150, Width = 100 };
            Button cancelButton = new Button { Text = "Отмена", Top = 240, Left = 260, Width = 100 };

            

            saveButton.Click += (s, e) =>
            {
                if (!double.TryParse(priceTextBox.Text, out double price))
                {
                    MessageBox.Show("Введите корректную цену.", "Ошибка");
                    return;
                }

                if (!int.TryParse(remainsTextBox.Text, out int remains))
                {
                    MessageBox.Show("Введите корректное количество остатков.", "Ошибка");
                    return;
                }

                var newData = new StockData
                {
                    ProductId = Convert.ToInt32(productComboBox.SelectedValue),
                    MeasurementUnitId = Convert.ToInt32(unitComboBox.SelectedValue),
                    Price = price,
                    Remains = remains,
                    SupplierId = Convert.ToInt32(supplierComboBox.SelectedValue)
                };

                string connectionString = "Data Source=restaurants_database.db;Version=3;";

                bool productExists = CheckIfProductExists(connectionString, Convert.ToInt32(productComboBox.SelectedValue));
                if (productExists && dataIsNull)
                {
                    MessageBox.Show("Этот продукт уже существует", "Ошибка!");
                }
                else
                {
                    onSave(newData);
                }
                editForm.Close();
            };

            cancelButton.Click += (s, e) => editForm.Close();

            editForm.Controls.Add(productLabel);
            editForm.Controls.Add(productComboBox);
            editForm.Controls.Add(unitLabel);
            editForm.Controls.Add(unitComboBox);
            editForm.Controls.Add(priceLabel);
            editForm.Controls.Add(priceTextBox);
            editForm.Controls.Add(remainsLabel);
            editForm.Controls.Add(remainsTextBox);
            editForm.Controls.Add(supplierLabel);
            editForm.Controls.Add(supplierComboBox);
            editForm.Controls.Add(saveButton);
            editForm.Controls.Add(cancelButton);

            if (data != null)
            {
                priceTextBox.Text = data.Price.ToString("F2");
                remainsTextBox.Text = data.Remains.ToString();
            }

            // Загрузка данных для ComboBox
            LoadProducts(productComboBox);
            LoadMeasurementUnits(unitComboBox);
            LoadSuppliers(supplierComboBox);

            // Настройка SelectedValue после загрузки данных
            if (data != null)
            {
                productComboBox.SelectedValue = data.ProductId;
                unitComboBox.SelectedValue = data.MeasurementUnitId;
                supplierComboBox.SelectedValue = data.SupplierId;

                productComboBox.Enabled = false; // Запрещаем менять продукт при редактировании
            }

            editForm.ShowDialog();
        }


        private static void LoadProducts(ComboBox comboBox)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Name FROM Products";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    DataTable productsTable = new DataTable();
                    productsTable.Load(reader);

                    comboBox.DataSource = productsTable;
                    comboBox.DisplayMember = "Name";
                    comboBox.ValueMember = "Id";
                }
            }
        }

        private static void LoadMeasurementUnits(ComboBox comboBox)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Name FROM Measurement_Units";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    DataTable unitsTable = new DataTable();
                    unitsTable.Load(reader);

                    comboBox.DataSource = unitsTable;
                    comboBox.DisplayMember = "Name";
                    comboBox.ValueMember = "Id";
                }
            }
        }

        private static void LoadSuppliers(ComboBox comboBox)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            DataTable suppliersTable = new DataTable();
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Name FROM Suppliers";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    suppliersTable.Load(reader);
                }
            }
            comboBox.DataSource = suppliersTable;
            comboBox.DisplayMember = "Name";
            comboBox.ValueMember = "Id";
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
                            s.Product_id AS 'ID Продукта',
                            p.Name AS Продукт,
                            mu.Name AS 'Ед. измерения',
                            s.Price AS Цена,
                            s.Remains AS Остаток,
                            sup.Name AS Поставщик
                        FROM Stocks s
                        JOIN Products p ON s.Product_id = p.Id
                        JOIN Measurement_Units mu ON s.Measurement_unit_id = mu.Id
                        JOIN Suppliers sup ON s.Supplier_id = sup.Id";

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

        static bool CheckIfProductExists(string connectionString, int productId)
        {
            string query = "SELECT COUNT(*) FROM Stocks WHERE Product_id = @ProductId";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", productId);

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
                    command.Parameters.AddWithValue("@MenuId", 17);

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

    public class StockData
    {
        public int ProductId { get; set; }
        public int MeasurementUnitId { get; set; }
        public double Price { get; set; }
        public int Remains { get; set; }
        public int SupplierId { get; set; }
    }
}
