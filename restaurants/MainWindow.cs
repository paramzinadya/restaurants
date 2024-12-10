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
            InitializeTabControl();  // Инициализация TabControl
            _userId = userId;
            LoadMenu();
        }

        // Инициализация TabControl
        private void InitializeTabControl()
        {
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Name = "tabControl"
            };
            this.Controls.Add(tabControl);  // Добавляем TabControl на форму
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
                var accessList = new List<AccessRights>();

                using (var connection = new SQLiteConnection(GetConnectionString()))
                {
                    connection.Open();

                    // Запрос для получения всех пунктов меню
                    string queryMenu = @"
                SELECT Id, ParentId, Name, DLL, Key, [Order]
                FROM MenuItems
                ORDER BY 
                    CASE WHEN ParentId = 0 THEN [Order] ELSE 9999 END, -- Основные модули по Order
                    ParentId, 
                    Id"; // Подмодули по их порядку добавления

                    var commandMenu = new SQLiteCommand(queryMenu, connection);
                    var adapterMenu = new SQLiteDataAdapter(commandMenu);
                    adapterMenu.Fill(dataTable);

                    // Запрос для получения прав доступа пользователя
                    string queryAccess = @"
                SELECT MenuId, Read
                FROM AccessList
                WHERE UserId = @UserId";
                    var commandAccess = new SQLiteCommand(queryAccess, connection);
                    commandAccess.Parameters.AddWithValue("@UserId", _userId);
                    var adapterAccess = new SQLiteDataAdapter(commandAccess);
                    var accessDataTable = new DataTable();
                    adapterAccess.Fill(accessDataTable);

                    // Преобразуем данные из AccessList в список прав пользователя
                    accessList = accessDataTable.AsEnumerable().Select(row => new AccessRights
                    {
                        MenuId = Convert.ToInt32(row["MenuId"]),
                        Read = Convert.ToInt32(row["Read"])
                    }).ToList();
                }

                // Проверка: загружены ли данные
                if (dataTable.Rows.Count == 0)
                {
                    MessageBox.Show("Данные меню не найдены в таблице MenuItems.", "Информация");
                    return;
                }

                // Построение меню с учетом прав пользователя
                BuildMenu(menuStrip, dataTable, accessList);
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
        private void BuildMenu(MenuStrip menuStrip, DataTable dataTable, List<AccessRights> accessList)
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
                // Проверяем, есть ли у пользователя права на чтение этого пункта меню
                var access = accessList.FirstOrDefault(a => a.MenuId == mainMenuItem.Id);
                if (access == null || access.Read == 0)
                    continue; // Если прав нет, пропускаем этот пункт меню

                // Создаём основной пункт меню
                var mainMenu = new ToolStripMenuItem(mainMenuItem.Name)
                {
                    Tag = mainMenuItem // Сохраняем данные о пункте меню в теге
                };

                // Подключаем обработчик клика
                mainMenu.Click += (sender, e) =>
                {
                    HandleMenuItemClick(mainMenuItem);
                };

                // Добавляем подменю (где ParentId == mainMenuItem.Id)
                BuildSubMenu(mainMenu.DropDownItems, mainMenuItem.Id, menuItems, accessList);

                // Добавляем основной пункт в MenuStrip
                menuStrip.Items.Add(mainMenu);
            }
        }

        // Метод для добавления подменю
        private void BuildSubMenu(ToolStripItemCollection parentCollection, int parentId, List<MenuItem> menuItems, List<AccessRights> accessList)
        {
            // Находим подмодули с ParentId, равным текущему ID
            var subMenuItems = menuItems
                .Where(item => item.ParentId == parentId)
                .OrderBy(item => item.Order)
                .ToList();

            foreach (var subMenuItem in subMenuItems)
            {
                // Проверяем, есть ли у пользователя права на чтение этого подменю
                var access = accessList.FirstOrDefault(a => a.MenuId == subMenuItem.Id);
                if (access == null || access.Read == 0)
                    continue; // Если прав нет, пропускаем это подменю

                // Создаём подменю
                var subMenu = new ToolStripMenuItem(subMenuItem.Name)
                {
                    Tag = subMenuItem // Сохраняем данные о пункте меню в теге
                };

                // Подключаем обработчик клика
                subMenu.Click += (sender, e) =>
                {
                    HandleMenuItemClick(subMenuItem);
                };

                // Если это подменю также имеет свои подменю, рекурсивно добавляем их
                BuildSubMenu(subMenu.DropDownItems, subMenuItem.Id, menuItems, accessList);

                // Добавляем подменю в выпадающий список
                parentCollection.Add(subMenu);
            }
        }

        private void AddTabPage(string tabTitle, Assembly assembly, MethodInfo methodInfo)
        {
            // Получаем TabControl
            var tabControl = this.Controls.OfType<TabControl>().FirstOrDefault();
            if (tabControl == null)
                return;

            // Создаем новую вкладку
            TabPage tabPage = new TabPage(tabTitle);

            // Создаем DataGridView для отображения данных
            var dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill
            };

            // Вызываем метод из DLL для получения данных (например, StreetControlPage.GetData)
            var result = methodInfo.Invoke(null, null);  // Предположим, что метод возвращает DataTable

            // Если метод возвращает DataTable, отображаем его в DataGridView
            if (result is DataTable table)
            {
                dataGridView.DataSource = table;
            }

            // Добавляем DataGridView в вкладку
            tabPage.Controls.Add(dataGridView);

            // Добавляем вкладку в TabControl
            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage; // Активируем эту вкладку
        }



        // Обработчик кликов по пунктам меню
        private void HandleMenuItemClick(MenuItem menuItem)
        {
            // Проверяем, является ли пункт меню контейнером (имеет ли подпункты)
            var menuStripItems = menuStrip.Items.Cast<ToolStripMenuItem>();
            var parentItem = menuStripItems.FirstOrDefault(item => item.Tag == menuItem);

            if (parentItem != null && parentItem.DropDownItems.Count > 0)
            {
                // Если у пункта меню есть подпункты, ничего не делаем
                return;
            }

            if (!string.IsNullOrEmpty(menuItem.DLL))
            {
                try
                {
                    Console.WriteLine($"Попытка загрузить DLL: {menuItem.DLL}");

                    // Загружаем сборку из файла DLL
                    var assembly = Assembly.LoadFrom(menuItem.DLL);
                    Console.WriteLine($"DLL {menuItem.DLL} успешно загружена.");

                    // Ищем тип с нужной функцией
                    var type = assembly.GetTypes()
                                       .FirstOrDefault(t => t.GetMethod(menuItem.Key) != null);

                    if (type == null)
                    {
                        Console.WriteLine($"Тип с методом {menuItem.Key} не найден в DLL.");
                        MessageBox.Show($"Тип с методом {menuItem.Key} не найден в DLL {menuItem.DLL}.", "Ошибка");
                        return;
                    }

                    // Получаем метод по имени (в поле Key хранится имя метода)
                    var methodInfo = type.GetMethod(menuItem.Key);

                    if (methodInfo != null)
                    {
                        Console.WriteLine($"Метод {menuItem.Key} найден, вызываем.");

                        // Проверяем, является ли метод статическим
                        if (methodInfo.IsStatic)
                        {
                            // Передаем id пользователя в качестве аргумента методу
                            var parameters = new object[] { _userId }; // передаем _userId как аргумент

                            // Вызываем статический метод с параметром
                            methodInfo.Invoke(null, parameters);
                            Console.WriteLine("Метод успешно вызван.");
                        }
                        else
                        {
                            Console.WriteLine($"Метод {menuItem.Key} не является статическим, не могу вызвать.");
                            MessageBox.Show($"Метод {menuItem.Key} не является статическим в DLL {menuItem.DLL}.", "Ошибка");
                        }

                        // Теперь добавляем вкладку в TabControl с данными из DLL
                        AddTabPage(menuItem.Name, assembly, methodInfo);
                    }
                    else
                    {
                        Console.WriteLine($"Метод {menuItem.Key} не найден.");
                        MessageBox.Show($"Метод {menuItem.Key} не найден в DLL {menuItem.DLL}.", "Ошибка");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки DLL: {ex.Message}");
                    MessageBox.Show($"Ошибка загрузки DLL: {ex.Message}", "Ошибка");
                }
            }
            else
            {
                // Только показываем сообщение, если у пункта меню нет дочерних элементов
                MessageBox.Show($"Для пункта меню '{menuItem.Name}' действие не задано.", "Информация");
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

        // Вспомогательный класс для хранения прав доступа
        private class AccessRights
        {
            public int MenuId { get; set; }
            public int Read { get; set; }
        }
    }
}
