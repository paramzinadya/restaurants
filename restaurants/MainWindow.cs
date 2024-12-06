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
        private MenuStrip menuStrip;

        public MainWindow(int userId)
        {
            InitializeComponent();
            InitializeMenuStrip();
            LoadMenu();
            _userId = userId;
        }

        // Метод для инициализации MenuStrip
        private void InitializeMenuStrip()
        {
            menuStrip = new MenuStrip
            {
                Dock = DockStyle.Top
            };
            this.Controls.Add(menuStrip); // Добавляем MenuStrip на форму
        }

        // Метод для загрузки меню из базы данных
        private void LoadMenu()
        {
            try
            {
                var dataTable = new DataTable();
                using (var connection = new SQLiteConnection(GetConnectionString()))
                {
                    connection.Open();

                    // Запрос для получения всех пунктов меню
                    string query = @"
                        SELECT Id, ParentId, Name, DLL, Key, [Order]
                        FROM MenuItems
                        ORDER BY 
                            CASE WHEN ParentId = 0 THEN [Order] ELSE 9999 END, -- Основные модули по Order
                            ParentId, 
                            Id"; // Подмодули по их порядку добавления
                    var command = new SQLiteCommand(query, connection);
                    var adapter = new SQLiteDataAdapter(command);
                    adapter.Fill(dataTable);
                }

                // Проверка: загружены ли данные
                if (dataTable.Rows.Count == 0)
                {
                    MessageBox.Show("Данные меню не найдены в таблице MenuItems.", "Информация");
                    return;
                }

                // Построение меню
                BuildMenu(menuStrip, dataTable);
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
            // Проверяем, содержит ли таблица необходимые столбцы
            if (!dataTable.Columns.Contains("Id") ||
                !dataTable.Columns.Contains("ParentId") ||
                !dataTable.Columns.Contains("Name"))
            {
                MessageBox.Show("Таблица не содержит необходимых столбцов ('Id', 'ParentId', 'Name').", "Ошибка");
                return;
            }

            // Преобразуем данные из таблицы в список объектов MenuItem
            var menuItems = dataTable.AsEnumerable().Select(row => new MenuItem
            {
                Id = Convert.ToInt32(row["Id"]),
                ParentId = Convert.ToInt32(row["ParentId"]),
                Name = Convert.ToString(row["Name"]),
                DLL = row["DLL"] == DBNull.Value ? null : Convert.ToString(row["DLL"]),
                Key = row["Key"] == DBNull.Value ? null : Convert.ToString(row["Key"]),
                Order = Convert.ToInt32(row["Order"])
            }).ToList();

            // Ищем основные пункты меню (где ParentId = 0) и сортируем их по Order
            var mainMenuItems = menuItems
                .Where(item => item.ParentId == 0)
                .OrderBy(item => item.Order)
                .ToList();

            if (mainMenuItems.Count == 0)
            {
                MessageBox.Show("Основные пункты меню не найдены.", "Информация");
                return;
            }

            // Создаём основное меню
            foreach (var mainMenuItem in mainMenuItems)
            {
                // Создаём основной пункт меню
                var mainMenu = new ToolStripMenuItem(mainMenuItem.Name);

                // Добавляем подменю (где ParentId == mainMenuItem.Id)
                BuildSubMenu(mainMenu.DropDownItems, mainMenuItem.Id, menuItems);

                // Добавляем основной пункт в MenuStrip
                menuStrip.Items.Add(mainMenu);
            }
        }

        // Метод для добавления подменю
        private void BuildSubMenu(ToolStripItemCollection parentCollection, int parentId, List<MenuItem> menuItems)
        {
            // Находим подмодули с ParentId, равным текущему ID
            var subMenuItems = menuItems
                .Where(item => item.ParentId == parentId)
                .OrderBy(item => item.Order)
                .ToList();

            foreach (var subMenuItem in subMenuItems)
            {
                // Создаём подменю
                var subMenu = new ToolStripMenuItem(subMenuItem.Name)
                {
                    Tag = subMenuItem // Сохраняем данные о пункте меню в теге
                };

                // Добавляем обработчик события клика по подменю
                subMenu.Click += (sender, e) => HandleMenuItemClick(subMenuItem);

                // Добавляем подменю в выпадающий список
                parentCollection.Add(subMenu);
            }
        }

        // Обработчик кликов по пунктам меню
        private void HandleMenuItemClick(MenuItem menuItem)
        {
            if (!string.IsNullOrEmpty(menuItem.DLL))
            {
                try
                {
                    // Загружаем DLL и отображаем форму
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
            public int ParentId { get; set; }
            public string Name { get; set; }
            public string DLL { get; set; }
            public string Key { get; set; }
            public int Order { get; set; }
        }
    }
}
