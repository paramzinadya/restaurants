using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace restaurants
{
    public partial class MainWindow : Form
    {
        private int _userId;

        public MainWindow(int userId)
        {
            InitializeComponent();
            _userId = userId; // Сохраняем ID пользователя
            LoadMenu();
        }

        // Метод для загрузки меню
        private void LoadMenu()
        {
            try
            {
                // Подключение к базе данных и получение данных о меню
                var dataTable = new DataTable();
                using (var connection = new SQLiteConnection(GetConnectionString()))
                {
                    connection.Open();

                    // Запрос для получения данных о пунктах меню с учётом прав пользователя
                    string query = @"
                        SELECT m.Id, m.ParentId, m.Name, m.DLL, m.Key, m.Order
                        FROM MenuItems m
                        INNER JOIN AccessList a ON m.Id = a.MenuItemId
                        WHERE a.UserId = @userId
                        ORDER BY m.Order";
                    var command = new SQLiteCommand(query, connection);
                    command.Parameters.AddWithValue("@userId", _userId);
                    var adapter = new SQLiteDataAdapter(command);
                    adapter.Fill(dataTable);
                }

                // Построение меню
                BuildMenu(menuStrip1, dataTable);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки меню: {ex.Message}", "Ошибка");
            }
        }

        // Строка подключения к базе данных
        private string GetConnectionString()
        {
            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string databaseRelativePath = "restaurants_database.db";
            return $"Data Source={System.IO.Path.Combine(projectDirectory, databaseRelativePath)};Version=3;";
        }

        // Метод для построения меню
        private void BuildMenu(MenuStrip menuStrip, DataTable dataTable)
        {
            if (!dataTable.Columns.Contains("Id") ||
                !dataTable.Columns.Contains("ParentId") ||
                !dataTable.Columns.Contains("Name"))
            {
                MessageBox.Show("Таблица не содержит необходимых столбцов ('Id', 'ParentId', 'Name').", "Ошибка");
                return;
            }

            var menuItems = dataTable.AsEnumerable().Select(row => new MenuItem
            {
                Id = row.Field<int>("Id"),
                ParentId = row.Field<int?>("ParentId"),
                Name = row.Field<string>("Name"),
                DLL = dataTable.Columns.Contains("DLL") ? row.Field<string>("DLL") : null,
                Key = dataTable.Columns.Contains("Key") ? row.Field<string>("Key") : null,
                Order = dataTable.Columns.Contains("Order") ? row.Field<int>("Order") : 0
            }).ToList();

            BuildMenuHierarchy(menuStrip.Items, null, menuItems);
        }

        private void BuildMenuHierarchy(ToolStripItemCollection parentCollection, int? parentId, List<MenuItem> menuItems)
        {
            var items = menuItems.Where(item => item.ParentId == parentId).OrderBy(item => item.Order);

            foreach (var menuItem in items)
            {
                var newMenuItem = new ToolStripMenuItem(menuItem.Name);
                newMenuItem.Click += (sender, e) => HandleMenuItemClick(menuItem);
                parentCollection.Add(newMenuItem);
                BuildMenuHierarchy(newMenuItem.DropDownItems, menuItem.Id, menuItems);
            }
        }

        private void HandleMenuItemClick(MenuItem menuItem)
        {
            if (!string.IsNullOrEmpty(menuItem.DLL))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(menuItem.DLL);
                    var formType = assembly.GetType(menuItem.Key);
                    if (formType != null)
                    {
                        var form = (Form)Activator.CreateInstance(formType);
                        form.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show($"Не удалось найти форму с ключом {menuItem.Key} в DLL {menuItem.DLL}.", "Ошибка");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке DLL: {ex.Message}", "Ошибка");
                }
            }
            else
            {
                MessageBox.Show($"Вы выбрали пункт меню '{menuItem.Name}', но действие не задано.", "Информация");
            }
        }

        // Вспомогательный класс для хранения данных о пункте меню
        private class MenuItem
        {
            public int Id { get; set; }
            public int? ParentId { get; set; }
            public string Name { get; set; }
            public string DLL { get; set; }
            public string Key { get; set; }
            public int Order { get; set; }
        }
    }
}
