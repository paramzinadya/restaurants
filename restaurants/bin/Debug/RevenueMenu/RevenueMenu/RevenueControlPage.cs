using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml.Linq;

namespace RevenueMenu
{
    public class RevenueControlPage
    {
        public static void GetRevenue(int userId)
        {
            // Получаем права пользователя
            var userPermissions = GetUserPermissions(userId);
            // Создание формы
            Form form = new Form
            {
                Text = "Управление выручкой",
                Width = 800,
                Height = 600
            };

            // Выпадающий список
            ComboBox revenueComboBox = new ComboBox
            {
                Left = 10,
                Top = 10,
                Width = 500
            };
            form.Controls.Add(revenueComboBox);
            // Кнопки
            Button addButton = new Button { Text = "Добавить отчет", Left = 520, Top = 10, Width = 120 };
            Button exportButton = new Button { Text = "Экспорт", Left = 650, Top = 10, Width = 100, Enabled = false };
            Button deleteButton = new Button { Text = "Удалить отчет", Left = 520, Top = 50, Width = 120, Enabled = false };
            Button exitButton = new Button { Text = "Выход", Left = 650, Top = 50, Width = 100 };
            Button addDetailButton = new Button { Text = "Добавить блюдо", Left = 10, Top = 50, Width = 110, Enabled = false };
            Button editCountButton = new Button { Text = "Редактировать", Left = 125, Top = 50, Width = 110, Enabled = false };
            Button deleteDetailButton = new Button { Text = "Удалить блюдо", Left = 240, Top = 50, Width = 110, Enabled = false };
            Button groupsReport = new Button { Text = "Выручка по категориям", Left = 355, Top = 50, Width = 140, Enabled = false };

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

            form.Controls.Add(groupsReport);
            form.Controls.Add(exportButton);
            form.Controls.Add(exitButton);

            int reportId = 0;
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
            LoadRevenue(revenueComboBox);
            // Обработчик выбора элемента в выпадающем списке
            revenueComboBox.SelectedIndexChanged += (sender, args) =>
            {
                if (revenueComboBox.SelectedItem != null)
                {
                    var selectedRequest = (DataRowView)revenueComboBox.SelectedItem;
                    reportId = Convert.ToInt32(selectedRequest["Id"]);
                    LoadRevenueDetails(reportId, detailsGridView);
                    deleteButton.Enabled = true;
                    exportButton.Enabled = true;
                    addDetailButton.Enabled = true;
                    groupsReport.Enabled = true;
                }
            };
            // Обработчик выбора строки в таблице
            detailsGridView.SelectionChanged += (sender, args) =>
            {
                editCountButton.Enabled = detailsGridView.SelectedRows.Count == 1;
                deleteDetailButton.Enabled = detailsGridView.SelectedRows.Count == 1;
            };
            // Кнопка "Создать отчет"
            addButton.Click += (sender, args) =>
            {
                ShowAddReportForm();
                LoadRevenue(revenueComboBox);
            };

            groupsReport.Click += (sender, args) =>
            {
                ShowGroupsReport(reportId);
            };

            // Кнопка "Экспорт"
            exportButton.Click += (sender, args) =>
            {
                ExportFile.ExportReport(reportId);
            };

            // Кнопка "Удалить заяку"
            deleteButton.Click += (sender, args) =>
            {
                DialogResult result = MessageBox.Show("Вы уверены, что хотите удалить отчет о выручке?", "Подтверждение удаления", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        string connectionString = "Data Source=restaurants_database.db;Version=3;";
                        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();

                            string deleteDetailsQuery = "DELETE FROM Revenue_details WHERE Report_id = @ReportId";
                            using (SQLiteCommand command = new SQLiteCommand(deleteDetailsQuery, connection))
                            {
                                command.Parameters.AddWithValue("@ReportId", reportId);
                                command.ExecuteNonQuery();
                            }

                            string query = "DELETE FROM Revenue WHERE Id = @RevenueId";
                            using (SQLiteCommand command = new SQLiteCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@RevenueId", reportId);
                                command.ExecuteNonQuery();
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении данных: {ex.Message}", "Ошибка");
                    }
                }
                LoadRevenue(revenueComboBox);
            };
            
            //Кнопка "Добавить продукт"
            addDetailButton.Click += (sender, args) =>
            {
                ShowAddDetailForm(reportId, detailsGridView);
            };
            // Кнопка "Изменитрь количество"
            editCountButton.Click += (sender, args) =>
            {
                if (detailsGridView.SelectedRows.Count == 1)
                {
                    var selectedRow = detailsGridView.SelectedRows[0];
                    //reportId = Convert.ToInt32(selectedRow.Cells["Report_id"].Value);
                    string dishtName = selectedRow.Cells["Блюдо"].Value.ToString();
                    int count = Convert.ToInt32(selectedRow.Cells["Количество"].Value);
                    string groupName = selectedRow.Cells["Категория"].Value.ToString();
                    ShowEditDishForm(reportId, dishtName, count, groupName, detailsGridView); // Передаем detailsGridView
                }
            };
            
            // Кнопка "Удалить продукт"
            deleteDetailButton.Click += (sender, args) =>
            {

                if (detailsGridView.SelectedRows.Count == 1)
                {
                    var selectedRow = detailsGridView.SelectedRows[0];
                    //reportId = Convert.ToInt32(selectedRow.Cells["Report_id"].Value);
                    string dishName = selectedRow.Cells["Блюдо"].Value.ToString();
                    DialogResult result = MessageBox.Show("Вы уверены, что хотите удалить блюдо из отчета?", "Подтверждение удаления", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        int idtodelete = getDishId(dishName);
                        string connectionString = "Data Source=restaurants_database.db;Version=3;";
                        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                        {
                            try
                            {
                                connection.Open();
                                string updateQuery = "DELETE FROM Revenue_details WHERE Report_id = @ReportId AND Dish_id = @DishId";
                                using (SQLiteCommand command = new SQLiteCommand(updateQuery, connection))
                                {
                                    command.Parameters.AddWithValue("@ReportId", reportId);
                                    command.Parameters.AddWithValue("@DishId", idtodelete);
                                    command.ExecuteNonQuery();
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Ошибка при удалении данных: {ex.Message}", "Ошибка");
                            }
                        }
                    }
                    LoadRevenueDetails(reportId, detailsGridView); // Перезагружаем данные в грид
                }

            };
            // Кнопка "Выход"
            exitButton.Click += (sender, args) => form.Close();
            form.ShowDialog();
        }


        private static void LoadRevenue(ComboBox comboBox)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"SELECT r.Id, res.Name || ' (' || r.Date || ')' AS DisplayText
                                     FROM Revenue r
                                     JOIN Restaurants res ON r.Restaurant_id = res.Id";
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
                    MessageBox.Show($"Ошибка загрузки отчетов: {ex.Message}", "Ошибка");
                }
            }
        }
        private static void LoadRevenueDetails(int reportId, DataGridView gridView)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"SELECT 
                                        rd.Report_id AS Id, 
                                        a.Name AS Блюдо,
                                        g.Name AS Категория,
                                        rd.Count AS Количество,
                                        a.price AS Цена,
                                        (rd.Count * a.price) AS Итого
                                    FROM 
                                        Revenue_details rd
                                    JOIN 
                                        Assortment a ON rd.Dish_id = a.Id
                                    JOIN 
                                        Groups g ON a.Group_id = g.Id
                                    WHERE rd.Report_id = @ReportId";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ReportId", reportId);
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
                    MessageBox.Show($"Ошибка загрузки деталей отчетов: {ex.Message}", "Ошибка");
                }
            }
        }

        private static void ShowAddReportForm()
        {
            Form addRequestForm = new Form
            {
                Text = "Добавить отчет",
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
            Label restaurantLabel = new Label { Text = "Ресторан:", Anchor = AnchorStyles.Left };
            ComboBox restaurantComboBox = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            LoadRestaurant(restaurantComboBox);

            Label dateLabel = new Label { Text = "Дата:", Anchor = AnchorStyles.Left };
            DateTimePicker datePicker = new DateTimePicker { Anchor = AnchorStyles.Left | AnchorStyles.Right };

            Label dishLabel = new Label { Text = "Блюда:", Anchor = AnchorStyles.Left };
            ListBox dishList = new ListBox { Height = 150, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };

            Button addProductButton = new Button { Text = "Добавить блюдо", Anchor = AnchorStyles.Left };
            Button saveButton = new Button { Text = "Сохранить", Anchor = AnchorStyles.Right };

            // Размещение элементов в TableLayoutPanel
            tableLayout.Controls.Add(restaurantLabel, 0, 0);
            tableLayout.Controls.Add(restaurantComboBox, 1, 0);
            tableLayout.Controls.Add(dateLabel, 0, 1);
            tableLayout.Controls.Add(datePicker, 1, 1);
            tableLayout.Controls.Add(dishLabel, 0, 2);
            tableLayout.Controls.Add(dishList, 0, 3);
            tableLayout.SetColumnSpan(dishList, 2); // Разворачиваем ListBox на всю ширину

            tableLayout.Controls.Add(addProductButton, 0, 4);
            tableLayout.Controls.Add(saveButton, 1, 4);

            // Обработчики событий
            addProductButton.Click += (sender, args) =>
            {
                try
                {
                    string restaurantId = ((DataRowView)restaurantComboBox.SelectedItem)["Id"].ToString();
                    ShowAddDishForm(Convert.ToInt32(restaurantId), dishList);
                    if (dishList != null) restaurantComboBox.Enabled = false;
                }
                catch
                {
                    MessageBox.Show("Выберите ресторан", "Внимание");
                }
            };

            saveButton.Click += (sender, args) =>
            {
                if (restaurantComboBox.SelectedItem == null || dishList.Items.Count == 0)
                {
                    MessageBox.Show("Заполните все поля и добавьте хотя бы одно блюдо.", "Ошибка");
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
                            string insertRequestQuery = "INSERT INTO Revenue (Restaurant_id, Date) VALUES (@RestaurantId, @Date); SELECT last_insert_rowid();";
                            int reportId;
                            using (SQLiteCommand command = new SQLiteCommand(insertRequestQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@RestaurantId", ((DataRowView)restaurantComboBox.SelectedItem)["Id"]);
                                command.Parameters.AddWithValue("@Date", datePicker.Value.ToString("yyyy-MM-dd"));
                                reportId = Convert.ToInt32(command.ExecuteScalar());
                            }

                            foreach (string productDetails in dishList.Items)
                            {
                                var parts = productDetails.Split(':');
                                int dishId = getDishId(parts[0]);
                                int count = int.Parse(parts[1]);

                                string insertDetailQuery = "INSERT INTO Revenue_details (Report_id, Dish_id, Count) VALUES (@ReportId, @DishId, @Count)";
                                using (SQLiteCommand command = new SQLiteCommand(insertDetailQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@ReportId", reportId);
                                    command.Parameters.AddWithValue("@DishId", dishId);
                                    command.Parameters.AddWithValue("@Count", count);
                                    command.ExecuteNonQuery();
                                }
                            }
                            transaction.Commit();
                        }
                        MessageBox.Show("Отчет по выпучке успешно создан!", "Успех");
                        addRequestForm.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения отчета: {ex.Message}", "Ошибка");
                    }
                }
            };

            addRequestForm.ShowDialog();
        }

        private static void ShowAddDishForm(int restaurantId, ListBox productList)
        {
            Form addProductForm = new Form
            {
                Text = "Добавить блюдо",
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
            Label dishLabel = new Label { Text = "Блюдо:", Anchor = AnchorStyles.Left };
            ComboBox dishComboBox = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            LoadAssortment(restaurantId, dishComboBox);

            Label countLabel = new Label { Text = "Количество:", Anchor = AnchorStyles.Left };
            NumericUpDown countNumeric = new NumericUpDown { Anchor = AnchorStyles.Left | AnchorStyles.Right, Minimum = 1, Maximum = 1000 };

            Button addButton = new Button { Text = "Добавить" };

            // Размещение элементов в TableLayoutPanel
            tableLayout.Controls.Add(dishLabel, 0, 0);
            tableLayout.Controls.Add(dishComboBox, 1, 0);
            tableLayout.Controls.Add(countLabel, 0, 1);
            tableLayout.Controls.Add(countNumeric, 1, 1);
            tableLayout.Controls.Add(addButton, 1, 2);

            // Обработчик события нажатия на кнопку "Добавить"
            addButton.Click += (sender, args) =>
            {
                if (dishComboBox.SelectedItem == null || (int)countNumeric.Value == 0)
                {
                    MessageBox.Show("Заполните все поля.", "Ошибка");
                    return;
                }

                string dishName = ((DataRowView)dishComboBox.SelectedItem)["Name"].ToString();
                int count = (int)countNumeric.Value;

                // Проверка, есть ли блюдо в списке
                foreach (var item in productList.Items)
                {
                    if (item.ToString().StartsWith(dishName + ":"))
                    {
                        MessageBox.Show("Это блюдо уже добавлено.", "Ошибка");
                        return;
                    }
                }

                // Сохраняем данные в формате, удобном для обработки
                productList.Items.Add($"{dishName}:{count}");
                addProductForm.Close();
            };

            addProductForm.ShowDialog();
        }

        
        private static void ShowAddDetailForm(int reportId, DataGridView gridView)
        {
            Form addDetailForm = new Form
            {
                Text = "Добавить блюдо",
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
            Label dishLabel = new Label { Text = "Блюдо:", Anchor = AnchorStyles.Left };
            ComboBox dishComboBox = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };

            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            int restaurantId = 0;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Restaurant_Id FROM Revenue WHERE Id = @ReportId";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ReportId", reportId);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            restaurantId = reader.GetInt32(0);
                        }
                    }
                }
            }

            LoadAssortment(restaurantId, dishComboBox);

            Label countLabel = new Label { Text = "Количество:", Anchor = AnchorStyles.Left };
            NumericUpDown countNumeric = new NumericUpDown { Anchor = AnchorStyles.Left | AnchorStyles.Right, Minimum = 1, Maximum = 1000 };

            Button addButton = new Button { Text = "Добавить" };

            // Размещение элементов в TableLayoutPanel
            tableLayout.Controls.Add(dishLabel, 0, 0);
            tableLayout.Controls.Add(dishComboBox, 1, 0);
            tableLayout.Controls.Add(countLabel, 0, 1);
            tableLayout.Controls.Add(countNumeric, 1, 1);
            tableLayout.Controls.Add(addButton, 1, 2);

            // Обработчик события нажатия на кнопку "Добавить"
            addButton.Click += (sender, args) =>
            {
                if (dishComboBox.SelectedItem == null || (int)countNumeric.Value == 0)
                {
                    MessageBox.Show("Заполните все поля.", "Ошибка");
                    return;
                }

                string dishName = ((DataRowView)dishComboBox.SelectedItem)["Name"].ToString();
                int count = (int)countNumeric.Value;
                int dishId = getDishId(dishName);

                //int pId = Convert.ToInt32(productId);
                // Обновляем запись в базе данных
                

                bool productExists = CheckIfDishExists(connectionString, reportId, dishId);
                if (productExists)
                {
                    MessageBox.Show("Это блюдо уже добавлено", "Ошибка!");
                }
                else
                {
                    // Обновляем запись в базе данных
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        try
                        {
                            connection.Open();

                            // Обновляем данные в таблице
                            string updateQuery = @"INSERT INTO Revenue_details (Report_id, Dish_id, Count) VALUES (@ReportId, @DishId, @Count)";
                            using (SQLiteCommand command = new SQLiteCommand(updateQuery, connection))
                            {
                                command.Parameters.AddWithValue("@ReportId", reportId);
                                command.Parameters.AddWithValue("@DishId", dishId);
                                command.Parameters.AddWithValue("@Count", count);
                                command.ExecuteNonQuery();
                            }

                            MessageBox.Show("Продукт успешно добавлен!", "Успех");
                            addDetailForm.Close();
                            LoadRevenueDetails(reportId, gridView); // Перезагружаем данные в грид
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
        
        private static void ShowEditDishForm(int reportId, string dishName, int count, string groupName, DataGridView gridView)
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
            Label productLabel = new Label { Text = "Блюдо:", Anchor = AnchorStyles.Left };
            Label nameLabel = new Label { Text = dishName.ToString(), Anchor = AnchorStyles.Left };


            Label countLabel = new Label { Text = "Количество:", Anchor = AnchorStyles.Left };
            TextBox countTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Text = count.ToString() };

            Label unitLabel = new Label { Text = "Категория продукции:", Anchor = AnchorStyles.Left };
            Label unitnameLabel = new Label { Text = groupName.ToString(), Anchor = AnchorStyles.Left };

            Button saveButton = new Button { Text = "Сохранить" };

            // Размещение элементов в TableLayoutPanel
            tableLayout.Controls.Add(productLabel, 0, 0);
            tableLayout.Controls.Add(nameLabel, 1, 0);
            tableLayout.Controls.Add(countLabel, 0, 1);
            tableLayout.Controls.Add(countTextBox, 1, 1);
            tableLayout.Controls.Add(unitLabel, 0, 2);
            tableLayout.Controls.Add(unitnameLabel, 1, 2);
            tableLayout.Controls.Add(saveButton, 1, 3);

            // Обработчик кнопки сохранения изменений
            saveButton.Click += (sender, args) =>
            {
                if (string.IsNullOrEmpty(countTextBox.Text))
                {
                    MessageBox.Show("Заполните все поля.", "Ошибка");
                    return;
                }

                int updatedCount = int.Parse(countTextBox.Text); // Получаем количество
                int old = getDishId(dishName);

                // Обновляем запись в базе данных
                string connectionString = "Data Source=restaurants_database.db;Version=3;";
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    try
                    {
                        connection.Open();
                        // Обновляем данные в таблице
                        string updateQuery = @"
                    UPDATE Revenue_details
                    SET Count = @Count
                    WHERE Report_id = @ReportId AND Dish_id = @DishId;";
                        using (SQLiteCommand command = new SQLiteCommand(updateQuery, connection))
                        {
                            command.Parameters.AddWithValue("@ReportId", reportId);
                            command.Parameters.AddWithValue("@Count", updatedCount);
                            command.Parameters.AddWithValue("@DishId", old);
                            command.ExecuteNonQuery();
                        }

                        MessageBox.Show("Успешно обновлено!", "Успех");
                        editProductForm.Close();
                        LoadRevenueDetails(reportId, gridView); // Перезагружаем данные в грид
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка обновления продукта: {ex.Message}", "Ошибка");
                    }
                }
            };

            editProductForm.ShowDialog();
        } 

        private static void ShowGroupsReport(int reportId)
        {
            // Создание формы
            Form form = new Form
            {
                Text = "Отчет по выручке по категориям меню",
                Width = 440,
                Height = 300
            };
            // Кнопки
            Button exportButton = new Button { Text = "Экспорт", Left = 200, Top = 10, Width = 100 };
            Button exitButton = new Button { Text = "Выход", Left = 310, Top = 10, Width = 100 };

            DataGridView dataGridView = new DataGridView
            {
                Left = 10,
                Top = 50,
                Width = 400,
                Height = 200,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            form.Controls.Add(dataGridView);

            form.Controls.Add(exportButton);
            form.Controls.Add(exitButton);

            LoadGroupsReport(reportId, dataGridView);
            //Кнопка "Экспорт"
            exportButton.Click += (sender, args) =>
            {
                ExportFile.ExportGroupsReport(reportId);
            };
            // Кнопка "Выход"
            exitButton.Click += (sender, args) => form.Close();
            form.ShowDialog();
        }

        private static void LoadGroupsReport(int reportId, DataGridView gridView)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"SELECT 
                                        g.Name AS Категория,
                                        SUM(rd.Count * a.price) AS Итого
                                    FROM 
                                        Revenue_details rd
                                    JOIN 
                                        Assortment a ON rd.Dish_id = a.Id
                                    JOIN 
                                        Groups g ON a.Group_id = g.Id
                                    WHERE 
                                        rd.Report_id = @ReportId
                                    GROUP BY 
                                        g.Name;";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ReportId", reportId);
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

        private static void LoadRestaurant(ComboBox comboBox)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT Id, Name FROM Restaurants";
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

        private static void LoadAssortment(int restaurantId, ComboBox comboBox)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT Id, Name FROM Assortment WHERE Restaurant_id = @RestaurantId AND IsDeleted = 0";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RestaurantId", restaurantId);

                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                        {
                            DataTable table = new DataTable();
                            adapter.Fill(table);
                            comboBox.DataSource = table;
                            comboBox.DisplayMember = "Name";
                            comboBox.ValueMember = "Id";
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки продуктов: {ex.Message}", "Ошибка");
                }
            }
        }

        private static int getDishId(string name)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";
            int id=0;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id FROM Assortment WHERE Name = @Name";

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

        static bool CheckIfDishExists(string connectionString, int reportId, int dishId)
        {
            string query = "SELECT COUNT(*) FROM Revenue_details WHERE Report_id = @ReportId AND Dish_id = @DishId";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ReportId", reportId);
                    command.Parameters.AddWithValue("@DishId", dishId);

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
                    command.Parameters.AddWithValue("@MenuId", 20);

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