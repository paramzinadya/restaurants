using RequestMenu;
using System;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace RecieptMenu
{
    public class RequestManagementForm
    {
        public static void GetReciept(int userId)
        {
            // Получаем права пользователя
            var userPermissions = GetUserPermissions(userId);
            // Создание формы
            Form form = new Form
            {
                Text = "Управление поставками",
                Width = 800,
                Height = 600
            };

            // Выпадающий список
            ComboBox requestComboBox = new ComboBox
            {
                Left = 10,
                Top = 10,
                Width = 500
            };
            form.Controls.Add(requestComboBox);
            // Кнопки
            Button addButton = new Button { Text = "Добавить поставку", Left = 520, Top = 10, Width = 120 };
            Button exportButton = new Button { Text = "Экспорт", Left = 650, Top = 10, Width = 100, Enabled = false };
            Button deleteButton = new Button { Text = "Удалить поставку", Left = 520, Top = 50, Width = 120, Enabled = false };
            Button exitButton = new Button { Text = "Выход", Left = 650, Top = 50, Width = 100 };
            Button addDetailButton = new Button { Text = "Добавить продукт", Left = 10, Top = 50, Width = 150, Enabled = false };
            Button editCountButton = new Button { Text = "Редактировать продукт", Left = 170, Top = 50, Width = 150, Enabled = false };
            Button deleteDetailButton = new Button { Text = "Удалить продукт", Left = 330, Top = 50, Width = 150, Enabled = false };

            if (userPermissions.Add == 1)
            {
                form.Controls.Add(addButton);
            }

            if (userPermissions.Edit == 1)
            {
                form.Controls.Add(addDetailButton);
                form.Controls.Add(editCountButton);
                form.Controls.Add(deleteDetailButton);
            }

            if (userPermissions.Delete == 1)
            {
                form.Controls.Add(deleteButton);
            }

            form.Controls.Add(exportButton);
            form.Controls.Add(exitButton);

            int receiptId = 0;
            // Таблица
            DataGridView detailsGridView = new DataGridView
            {
                Left = 10,
                Top = 100,
                Width = 760,
                Height = 450,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            form.Controls.Add(detailsGridView);
            // Загрузка данных для выпадающего списка
            LoadReceipts(requestComboBox);
            // Обработчик выбора элемента в выпадающем списке
            requestComboBox.SelectedIndexChanged += (sender, args) =>
            {
                if (requestComboBox.SelectedItem != null)
                {
                    var selectedRequest = (DataRowView)requestComboBox.SelectedItem;
                    receiptId = Convert.ToInt32(selectedRequest["Id"]);
                    LoadReceiptDetails(receiptId, detailsGridView);
                    deleteButton.Enabled = true;
                    exportButton.Enabled = true;
                    addDetailButton.Enabled = true;
                }
            };
            // Обработчик выбора строки в таблице
            detailsGridView.SelectionChanged += (sender, args) =>
            {
                editCountButton.Enabled = detailsGridView.SelectedRows.Count == 1;
                deleteDetailButton.Enabled = detailsGridView.SelectedRows.Count == 1;
            };
            // Кнопка "Создать заяку"
            addButton.Click += (sender, args) =>
            {
                ShowAddReceiptForm();
                LoadReceipts(requestComboBox);
            };

            // Кнопка "Экспорт"
            exportButton.Click += (sender, args) =>
            {
                ExportFile.ExportRequestToExcel(receiptId);
            };

            // Кнопка "Удалить заяку"
            deleteButton.Click += (sender, args) =>
            {
                DialogResult result = MessageBox.Show("Вы уверены, что хотите удалить поставку?", "Подтверждение удаления", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        string connectionString = "Data Source=restaurants_database.db;Version=3;";
                        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();

                            string deleteDetailsQuery = "DELETE FROM Receipts_details WHERE Receipt_id = @Receipt_id";
                            using (SQLiteCommand command = new SQLiteCommand(deleteDetailsQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Receipt_id", receiptId);
                                command.ExecuteNonQuery();
                            }

                            string query = "DELETE FROM Receipts WHERE Id = @Receipt_id";
                            using (SQLiteCommand command = new SQLiteCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@Receipt_id", receiptId);
                                command.ExecuteNonQuery();
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении данных: {ex.Message}", "Ошибка");
                    }
                }
                LoadReceipts(requestComboBox);
            };
            //Кнопка "Добавить продукт"
            addDetailButton.Click += (sender, args) =>
            {
                ShowAddDetailForm(receiptId, detailsGridView);
            };

            // Кнопка "Изменитрь количество"
            editCountButton.Click += (sender, args) =>
            {
                if (detailsGridView.SelectedRows.Count == 1)
                {
                    var selectedRow = detailsGridView.SelectedRows[0];
                    receiptId = Convert.ToInt32(selectedRow.Cells["Id"].Value);
                    string productName = selectedRow.Cells["Продукт"].Value.ToString();
                    int count = Convert.ToInt32(selectedRow.Cells["Количество"].Value);
                    string unitName = selectedRow.Cells["Ед. измерения"].Value.ToString();
                    int price = Convert.ToInt32(selectedRow.Cells["Цена"].Value);
                    ShowEditProductForm(receiptId, productName, count, unitName, price, detailsGridView); // Передаем detailsGridView
                }
            };
            // Кнопка "Удалить продукт"
            deleteDetailButton.Click += (sender, args) =>
            {

                if (detailsGridView.SelectedRows.Count == 1)
                {
                    var selectedRow = detailsGridView.SelectedRows[0];
                    receiptId = Convert.ToInt32(selectedRow.Cells["Id"].Value);
                    string productName = selectedRow.Cells["Продукт"].Value.ToString();
                    DialogResult result = MessageBox.Show("Вы уверены, что хотите удалить продукт из поставки?", "Подтверждение удаления", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            int idtodelete = 0;
                            string connectionString = "Data Source=restaurants_database.db;Version=3;";
                            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                            {
                                connection.Open();

                                string query = "SELECT Id FROM Products WHERE Name = @ProductName";
                                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                                {
                                    command.Parameters.AddWithValue("@ProductName", productName);
                                    using (SQLiteDataReader reader = command.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            idtodelete = reader.GetInt32(0);
                                        }
                                    }
                                }

                                string updateQuery = "DELETE FROM Receipts_details WHERE Receipt_id = @Receipt_id AND Product_id = @Product_id";
                                using (SQLiteCommand command = new SQLiteCommand(updateQuery, connection))
                                {
                                    command.Parameters.AddWithValue("@Receipt_id", receiptId);
                                    command.Parameters.AddWithValue("@Product_id", idtodelete);
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при удалении данных: {ex.Message}", "Ошибка");
                        }
                    }
                    LoadReceiptDetails(receiptId, detailsGridView); // Перезагружаем данные в грид
                }

            };
            // Кнопка "Выход"
            exitButton.Click += (sender, args) => form.Close();
            form.ShowDialog();
        }


        private static void LoadReceipts(ComboBox comboBox)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"SELECT r.Id, s.Name || ' (' || r.Date || ')' AS DisplayText
                                     FROM Receipts r
                                     JOIN Suppliers s ON r.Supplier_id = s.Id";
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        comboBox.DataSource = table;
                        comboBox.DisplayMember = "DisplayText";
                        comboBox.ValueMember = "Id";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки поставок: {ex.Message}", "Ошибка");
                }
            }
        }
        private static void LoadReceiptDetails(int receitpId, DataGridView gridView)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"SELECT rd.Receipt_id AS Id, p.Name AS Продукт, rd.Count AS Количество, mu.Name AS 'Ед. измерения', rd.Price AS Цена
                                     FROM Receipts_details rd
                                     JOIN Products p ON rd.Product_id = p.Id
                                     JOIN Measurement_units mu ON rd.Measurement_unit_id = mu.Id
                                     WHERE rd.Receipt_id = @ReceiptId";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ReceiptId", receitpId);
                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                        {
                            DataTable table = new DataTable();
                            adapter.Fill(table);
                            gridView.DataSource = table;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки деталей поставки: {ex.Message}", "Ошибка");
                }
            }
        }

        private static void ShowAddReceiptForm()
        {
            Form addRequestForm = new Form
            {
                Text = "Добавить поставку",
                Width = 400,
                Height = 400
            };

            // Создаем TableLayoutPanel для управления расположением элементов
            TableLayoutPanel tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(10),
                AutoSize = true,
                ColumnStyles = { new ColumnStyle(SizeType.Percent, 30), new ColumnStyle(SizeType.Percent, 70) } // Равномерное распределение колонок
            };
            addRequestForm.Controls.Add(tableLayout);

            // Создаем и добавляем элементы на форму
            Label supplierLabel = new Label { Text = "Поставщик:", Anchor = AnchorStyles.Left };
            ComboBox restaurantComboBox = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            LoadRSuppliers(restaurantComboBox);

            Label dateLabel = new Label { Text = "Дата:", Anchor = AnchorStyles.Left };
            DateTimePicker datePicker = new DateTimePicker { Anchor = AnchorStyles.Left | AnchorStyles.Right };

            Label productsLabel = new Label { Text = "Продукты:", Anchor = AnchorStyles.Left };
            ListBox productList = new ListBox { Height = 150, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };

            Button addProductButton = new Button { Text = "Добавить продукт", Anchor = AnchorStyles.Left };
            Button saveButton = new Button { Text = "Сохранить", Anchor = AnchorStyles.Right };

            // Размещение элементов в TableLayoutPanel
            tableLayout.Controls.Add(supplierLabel, 0, 0);
            tableLayout.Controls.Add(restaurantComboBox, 1, 0);
            tableLayout.Controls.Add(dateLabel, 0, 1);
            tableLayout.Controls.Add(datePicker, 1, 1);
            tableLayout.Controls.Add(productsLabel, 0, 2);
            tableLayout.Controls.Add(productList, 0, 3);
            tableLayout.SetColumnSpan(productList, 2); // Разворачиваем ListBox на всю ширину

            tableLayout.Controls.Add(addProductButton, 0, 4);
            tableLayout.Controls.Add(saveButton, 1, 4);

            // Обработчики событий
            addProductButton.Click += (sender, args) =>
            {
                ShowAddProductForm(productList);
            };

            saveButton.Click += (sender, args) =>
            {
                if (restaurantComboBox.SelectedItem == null || productList.Items.Count == 0)
                {
                    MessageBox.Show("Заполните все поля и добавьте хотя бы один продукт.", "Ошибка");
                    return;
                }

                string connectionString = "Data Source=restaurants_database.db;Version=3;";
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    try
                    {
                        connection.Open();
                        using (SQLiteTransaction transaction = connection.BeginTransaction())
                        {
                            string insertRequestQuery = "INSERT INTO Receipts (Supplier_id, Date) VALUES (@SupplierId, @Date); SELECT last_insert_rowid();";
                            int receiptId;
                            using (SQLiteCommand command = new SQLiteCommand(insertRequestQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@SupplierId", ((DataRowView)restaurantComboBox.SelectedItem)["Id"]);
                                command.Parameters.AddWithValue("@Date", datePicker.Value.ToString("yyyy-MM-dd"));
                                receiptId = Convert.ToInt32(command.ExecuteScalar());
                            }

                            foreach (string productDetails in productList.Items)
                            {
                                var parts = productDetails.Split('|');
                                int productId = GetProductId((parts[0]).ToString());
                                int count = int.Parse(parts[1]);
                                int unitId = GetUnitId((parts[2]).ToString());
                                int price = int.Parse(parts[3]);

                                string insertDetailQuery = "INSERT INTO Receipts_details (Receipt_id, Product_id, Count, Measurement_unit_id, Price) VALUES (@ReceiptId, @ProductId, @Count, @UnitId, @Price)";
                                using (SQLiteCommand command = new SQLiteCommand(insertDetailQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@ReceiptId", receiptId);
                                    command.Parameters.AddWithValue("@ProductId", productId);
                                    command.Parameters.AddWithValue("@Count", count);
                                    command.Parameters.AddWithValue("@UnitId", unitId);
                                    command.Parameters.AddWithValue("@Price", price);
                                    command.ExecuteNonQuery();
                                }
                            }
                            transaction.Commit();
                        }
                        MessageBox.Show("Поставка успешно создана!", "Успех");
                        addRequestForm.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения поставки: {ex.Message}", "Ошибка");
                    }
                }
            };

            addRequestForm.ShowDialog();
        }


        private static void ShowAddProductForm(ListBox productList)
        {
            Form addProductForm = new Form
            {
                Text = "Добавить продукт",
                Width = 400,
                Height = 300
            };

            // Создаем TableLayoutPanel для управления расположением элементов
            TableLayoutPanel tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(10),
                AutoSize = true,
                ColumnStyles = { new ColumnStyle(SizeType.Percent, 30), new ColumnStyle(SizeType.Percent, 70) } // Равномерное распределение колонок
            };
            addProductForm.Controls.Add(tableLayout);

            // Создаем и добавляем элементы на форму
            Label productLabel = new Label { Text = "Продукт:", Anchor = AnchorStyles.Left };
            ComboBox productComboBox = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            LoadProducts(productComboBox);

            Label countLabel = new Label { Text = "Количество:", Anchor = AnchorStyles.Left };
            NumericUpDown countNumeric = new NumericUpDown { Anchor = AnchorStyles.Left | AnchorStyles.Right, Minimum = 1, Maximum = 1000 };

            Label unitLabel = new Label { Text = "Ед. изм.:", Anchor = AnchorStyles.Left };
            ComboBox unitComboBox = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            LoadMeasurementUnits(unitComboBox);

            Label priceLabel = new Label { Text = "Закупочная цена:", Anchor = AnchorStyles.Left };
            NumericUpDown priceNumeric = new NumericUpDown { Anchor = AnchorStyles.Left | AnchorStyles.Right, Minimum = 1, Maximum = 1000 };

            Button addButton = new Button { Text = "Добавить" };

            // Размещение элементов в TableLayoutPanel
            tableLayout.Controls.Add(productLabel, 0, 0);
            tableLayout.Controls.Add(productComboBox, 1, 0);
            tableLayout.Controls.Add(countLabel, 0, 1);
            tableLayout.Controls.Add(countNumeric, 1, 1);
            tableLayout.Controls.Add(unitLabel, 0, 2);
            tableLayout.Controls.Add(unitComboBox, 1, 2);
            tableLayout.Controls.Add(priceLabel, 0, 3);
            tableLayout.Controls.Add(priceNumeric, 1, 3);
            tableLayout.Controls.Add(addButton, 1, 4);

            // Обработчик события нажатия на кнопку "Добавить"
            addButton.Click += (sender, args) =>
            {
                if (productComboBox.SelectedItem == null || unitComboBox.SelectedItem == null || (int)countNumeric.Value == 0 || (int)priceNumeric.Value == 0)
                {
                    MessageBox.Show("Заполните все поля.", "Ошибка");
                    return;
                }

                // Получаем данные из ComboBox
                string productId = ((DataRowView)productComboBox.SelectedItem)["Id"].ToString();
                string productName = ((DataRowView)productComboBox.SelectedItem)["Name"].ToString();
                string unitId = ((DataRowView)unitComboBox.SelectedItem)["Id"].ToString();
                string unitName = ((DataRowView)unitComboBox.SelectedItem)["Name"].ToString();
                int count = (int)countNumeric.Value;
                int price = (int)priceNumeric.Value;

                // Проверка, есть ли блюдо в списке
                foreach (var item in productList.Items)
                {
                    if (item.ToString().StartsWith(productName + "|"))
                    {
                        MessageBox.Show("Этот продукт уже добавлен.", "Ошибка");
                        return;
                    }
                }

                // Сохраняем данные в формате, удобном для обработки
                productList.Items.Add($"{productName}|{count}|{unitName}|{price}");

                // Отображаем понятный текст для пользователя
                productList.DisplayMember = $"Продукт: {productName}, Количество: {count}, Ед. изм.: {unitName}";
                addProductForm.Close();
            };

            addProductForm.ShowDialog();
        }

        private static void ShowAddDetailForm(int receiptId, DataGridView gridView)
        {
            Form addDetailForm = new Form
            {
                Text = "Добавить продукт",
                Width = 400,
                Height = 300
            };

            // Создаем TableLayoutPanel для управления расположением элементов
            TableLayoutPanel tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(10),
                AutoSize = true,
                ColumnStyles = { new ColumnStyle(SizeType.Percent, 30), new ColumnStyle(SizeType.Percent, 70) } // Равномерное распределение колонок
            };
            addDetailForm.Controls.Add(tableLayout);

            // Создаем и добавляем элементы на форму
            Label productLabel = new Label { Text = "Продукт:", Anchor = AnchorStyles.Left };
            ComboBox productComboBox = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            LoadProducts(productComboBox);

            Label countLabel = new Label { Text = "Количество:", Anchor = AnchorStyles.Left };
            NumericUpDown countNumeric = new NumericUpDown { Anchor = AnchorStyles.Left | AnchorStyles.Right, Minimum = 1, Maximum = 10000 };

            Label unitLabel = new Label { Text = "Ед. изм.:", Anchor = AnchorStyles.Left };
            ComboBox unitComboBox = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            LoadMeasurementUnits(unitComboBox);

            Label priceLabel = new Label { Text = "Закупочная цена:", Anchor = AnchorStyles.Left };
            NumericUpDown priceNumeric = new NumericUpDown { Anchor = AnchorStyles.Left | AnchorStyles.Right, Minimum = 1, Maximum = 1000000 };

            Button addButton = new Button { Text = "Добавить" };

            // Размещение элементов в TableLayoutPanel
            tableLayout.Controls.Add(productLabel, 0, 0);
            tableLayout.Controls.Add(productComboBox, 1, 0);
            tableLayout.Controls.Add(countLabel, 0, 1);
            tableLayout.Controls.Add(countNumeric, 1, 1);
            tableLayout.Controls.Add(unitLabel, 0, 2);
            tableLayout.Controls.Add(unitComboBox, 1, 2);
            tableLayout.Controls.Add(priceLabel, 0, 3);
            tableLayout.Controls.Add(priceNumeric, 1, 3);
            tableLayout.Controls.Add(addButton, 1, 4);

            // Обработчик события нажатия на кнопку "Добавить"
            addButton.Click += (sender, args) =>
            {
                if (productComboBox.SelectedItem == null || unitComboBox.SelectedItem == null || (int)countNumeric.Value == 0 || (int)priceNumeric.Value == 0)
                {
                    MessageBox.Show("Заполните все поля.", "Ошибка");
                    return;
                }

                // Получаем данные из ComboBox
                string productId = ((DataRowView)productComboBox.SelectedItem)["Id"].ToString();
                string productName = ((DataRowView)productComboBox.SelectedItem)["Name"].ToString();
                string unitId = ((DataRowView)unitComboBox.SelectedItem)["Id"].ToString();
                string unitName = ((DataRowView)unitComboBox.SelectedItem)["Name"].ToString();
                int count = (int)countNumeric.Value;
                int price = (int)priceNumeric.Value;
                int pId = Convert.ToInt32(productId);

                // Обновляем запись в базе данных
                string connectionString = "Data Source=restaurants_database.db;Version=3;";

                bool productExists = CheckIfProductExists(connectionString, receiptId, pId);
                if (productExists)
                {
                    MessageBox.Show("Этот продукт уже существует", "Ошибка!");
                }
                else
                {
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        try
                        {
                            connection.Open();

                            // Обновляем данные в таблице
                            string updateQuery = @"INSERT INTO Receipts_details (Receipt_id, Product_id, Count, Measurement_unit_id, Price) VALUES (@ReceiptId, @ProductId, @Count, @UnitId, @Price)";
                            using (SQLiteCommand command = new SQLiteCommand(updateQuery, connection))
                            {
                                command.Parameters.AddWithValue("@ReceiptId", receiptId);
                                command.Parameters.AddWithValue("@ProductId", productId);
                                command.Parameters.AddWithValue("@Count", count);
                                command.Parameters.AddWithValue("@UnitId", unitId);
                                command.Parameters.AddWithValue("@Price", price);
                                command.ExecuteNonQuery();
                            }

                            MessageBox.Show("Продукт успешно добавлен!", "Успех");
                            addDetailForm.Close();
                            LoadReceiptDetails(receiptId, gridView); // Перезагружаем данные в грид
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка обновления продукта: {ex.Message}", "Ошибка");
                        }
                    }
                }
            };

            addDetailForm.ShowDialog();
        }

        private static void ShowEditProductForm(int receiptId, string productName, int count, string unitName, int price, DataGridView gridView)
        {
            Form editProductForm = new Form
            {
                Text = "Изменить количество",
                Width = 300,
                Height = 200
            };

            // Создаем TableLayoutPanel для управления расположением элементов
            TableLayoutPanel tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(10),
                AutoSize = true,
                ColumnStyles = { new ColumnStyle(SizeType.Percent, 30), new ColumnStyle(SizeType.Percent, 70) } // Равномерное распределение колонок
            };
            editProductForm.Controls.Add(tableLayout);

            // Создаем и добавляем элементы на форму
            Label productLabel = new Label { Text = "Продукт:", Anchor = AnchorStyles.Left };
            Label nameLabel = new Label { Text = productName.ToString(), Anchor = AnchorStyles.Left };


            Label countLabel = new Label { Text = "Количество:", Anchor = AnchorStyles.Left };
            TextBox countTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Text = count.ToString() };

            Label unitLabel = new Label { Text = "Единица измерения:", Anchor = AnchorStyles.Left };
            Label unitnameLabel = new Label { Text = unitName.ToString(), Anchor = AnchorStyles.Left };

            Label priceLabel = new Label { Text = "Закупочная цена:", Anchor = AnchorStyles.Left };
            TextBox priceTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Text = price.ToString() };

            Button saveButton = new Button { Text = "Сохранить" };

            // Размещение элементов в TableLayoutPanel
            tableLayout.Controls.Add(productLabel, 0, 0);
            tableLayout.Controls.Add(nameLabel, 1, 0);
            tableLayout.Controls.Add(countLabel, 0, 1);
            tableLayout.Controls.Add(countTextBox, 1, 1);
            tableLayout.Controls.Add(unitLabel, 0, 2);
            tableLayout.Controls.Add(unitnameLabel, 1, 2);
            tableLayout.Controls.Add(priceLabel, 0, 3);
            tableLayout.Controls.Add(priceTextBox, 1, 3);
            tableLayout.Controls.Add(saveButton, 1, 4);

            // Обработчик кнопки сохранения изменений
            saveButton.Click += (sender, args) =>
            {
                if (string.IsNullOrEmpty(countTextBox.Text) || string.IsNullOrEmpty(priceTextBox.Text))
                {
                    MessageBox.Show("Заполните все поля.", "Ошибка");
                    return;
                }

                int updatedCount = int.Parse(countTextBox.Text); // Получаем количество
                int updatedPrice = int.Parse(priceTextBox.Text);
                int old = 0;

                // Обновляем запись в базе данных
                string connectionString = "Data Source=restaurants_database.db;Version=3;";
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    try
                    {
                        connection.Open();

                        // Получаем старый productId для обновления
                        string query = "SELECT Id FROM Products WHERE Name = @ProductName";
                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@ProductName", productName);
                            using (SQLiteDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    old = reader.GetInt32(0);
                                }
                            }
                        }

                        // Обновляем данные в таблице
                        string updateQuery = @"
                    UPDATE Receipts_details
                    SET Count = @Count, Price = @Price
                    WHERE Receipt_id = @ReceiptId AND Product_id = @ProductId;";
                        using (SQLiteCommand command = new SQLiteCommand(updateQuery, connection))
                        {
                            command.Parameters.AddWithValue("@ReceiptId", receiptId);
                            command.Parameters.AddWithValue("@Count", updatedCount);
                            command.Parameters.AddWithValue("@Price", updatedPrice);
                            command.Parameters.AddWithValue("@ProductId", old);
                            command.ExecuteNonQuery();
                        }

                        MessageBox.Show("Успешно обновлено!", "Успех");
                        editProductForm.Close();
                        LoadReceiptDetails(receiptId, gridView); // Перезагружаем данные в грид
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка обновления продукта: {ex.Message}", "Ошибка");
                    }
                }
            };

            editProductForm.ShowDialog();
        }


        private static void LoadRSuppliers(ComboBox comboBox)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT Id, Name FROM Suppliers";
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        comboBox.DataSource = table;
                        comboBox.DisplayMember = "Name";
                        comboBox.ValueMember = "Id";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки ресторанов: {ex.Message}", "Ошибка");
                }
            }
        }

        private static void LoadProducts(ComboBox comboBox)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT Id, Name FROM Products";
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        comboBox.DataSource = table;
                        comboBox.DisplayMember = "Name";
                        comboBox.ValueMember = "Id";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки продуктов: {ex.Message}", "Ошибка");
                }
            }
        }

        private static void LoadMeasurementUnits(ComboBox comboBox)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            DataTable table = new DataTable();
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT Id, Name FROM Measurement_units";
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection))
                    {
                        adapter.Fill(table);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки единиц измерения: {ex.Message}", "Ошибка");
                }
            }
            comboBox.DataSource = table;
            comboBox.DisplayMember = "Name";
            comboBox.ValueMember = "Id";
        }

        private static int GetProductId(string name)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            int id = 0;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id FROM Products WHERE Name = @Name";

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

        private static int GetUnitId(string name)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            int id = 0;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id FROM Measurement_units WHERE Name = @Name";

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

        static bool CheckIfProductExists(string connectionString, int receiptId, int productId)
        {
            string query = "SELECT COUNT(*) FROM Receipts_details WHERE Receipt_id = @ReceiptId AND Product_id = @ProductId";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ReceiptId", receiptId);
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
                    command.Parameters.AddWithValue("@MenuId", 19);

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
        // Класс для удобной работы с элементами в ComboBox
        public class ComboBoxItem
        {
            public int Id { get; set; }
            public string Text { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }
    }
}