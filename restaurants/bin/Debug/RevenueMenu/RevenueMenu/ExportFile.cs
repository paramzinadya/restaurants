using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using Microsoft.Office.Interop.Excel;
using System.Windows.Forms;

namespace RevenueMenu
{
    public static class ExportFile
    {
        [STAThread]
        public static void ExportReport(int reportId)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Получение основной информации
                string mainInfoQuery = @"
                    SELECT r.Id, res.Name AS Restaurant_Name, r.Date 
                    FROM Revenue r 
                    JOIN Restaurants res ON r.Restaurant_id = res.Id
                    WHERE r.Id = @ReportId";

                string reportDate = "";
                string restaurantName = "";

                using (var command = new SQLiteCommand(mainInfoQuery, connection))
                {
                    command.Parameters.AddWithValue("@ReportId", reportId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            reportDate = reader["Date"].ToString();
                            restaurantName = reader["Restaurant_Name"].ToString();
                        }
                    }
                }

                // Получение деталей отчета
                string detailsQuery = @"
                    SELECT 
                        rd.Report_id, 
                        a.Name AS Dish_Name,
                        g.Name AS Group_Name,
                        rd.Count,
                        a.price,
                        (rd.Count * a.price) AS Total_Cost
                    FROM 
                        Revenue_details rd
                    JOIN 
                        Assortment a ON rd.Dish_id = a.Id
                    JOIN 
                        Groups g ON a.Group_id = g.Id
                    WHERE 
                        rd.Report_id = @ReportId";

                var details = new List<(string DishName, string GroupName, int Count, decimal Price, decimal TotalCost)>();

                using (var command = new SQLiteCommand(detailsQuery, connection))
                {
                    command.Parameters.AddWithValue("@ReportId", reportId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dishName = reader["Dish_Name"].ToString();
                            string groupName = reader["Group_Name"].ToString();
                            int count = Convert.ToInt32(reader["Count"]);
                            decimal price = Convert.ToDecimal(reader["price"]);
                            decimal totalCost = Convert.ToDecimal(reader["Total_Cost"]);

                            details.Add((dishName, groupName, count, price, totalCost));
                        }
                    }
                }

                // Создание Excel файла
                var excelApp = new Microsoft.Office.Interop.Excel.Application();
                Workbook workbook = excelApp.Workbooks.Add();
                Worksheet worksheet = (Worksheet)workbook.Sheets[1];

                // Основная информация
                worksheet.Cells[1, 1] = "Дата отчета:";
                worksheet.Cells[1, 2] = reportDate;
                worksheet.Cells[2, 1] = "Название ресторана:";
                worksheet.Cells[2, 2] = restaurantName;

                // Заголовки таблицы
                worksheet.Cells[4, 1] = "Название блюда";
                worksheet.Cells[4, 2] = "Группа";
                worksheet.Cells[4, 3] = "Количество";
                worksheet.Cells[4, 4] = "Цена";
                worksheet.Cells[4, 5] = "Итоговая стоимость";

                // Заполнение данных
                int currentRow = 5;
                foreach (var detail in details)
                {
                    worksheet.Cells[currentRow, 1] = detail.DishName;
                    worksheet.Cells[currentRow, 2] = detail.GroupName;
                    worksheet.Cells[currentRow, 3] = detail.Count;
                    worksheet.Cells[currentRow, 4] = detail.Price;
                    worksheet.Cells[currentRow, 5] = detail.TotalCost;
                    currentRow++;
                }
                DateTime today = DateTime.Today;
                // Сохранение файла
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    Title = "Сохранить отчет о выручке",
                    FileName = $"Report.{today.ToString("d")}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    workbook.SaveAs(saveFileDialog.FileName);
                    MessageBox.Show($"Отчет успешно сохранен: {saveFileDialog.FileName}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // Очистка ресурсов
                workbook.Close(false);
                excelApp.Quit();
            }
        }

        [STAThread]
        public static void ExportGroupsReport(int reportId)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Получение основной информации
                string mainInfoQuery = @"
                    SELECT r.Id, res.Name AS Restaurant_Name, r.Date 
                    FROM Revenue r 
                    JOIN Restaurants res ON r.Restaurant_id = res.Id
                    WHERE r.Id = @ReportId";

                string reportDate = "";
                string restaurantName = "";

                using (var command = new SQLiteCommand(mainInfoQuery, connection))
                {
                    command.Parameters.AddWithValue("@ReportId", reportId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            reportDate = reader["Date"].ToString();
                            restaurantName = reader["Restaurant_Name"].ToString();
                        }
                    }
                }

                // Получение суммарной выручки по группам
                string groupRevenueQuery = @"
                    SELECT 
                        g.Name AS Group_Name,
                        SUM(rd.Count * a.price) AS Total_Revenue
                    FROM 
                        Revenue_details rd
                    JOIN 
                        Assortment a ON rd.Dish_id = a.Id
                    JOIN 
                        Groups g ON a.Group_id = g.Id
                    WHERE 
                        rd.Report_id = @ReportId
                    GROUP BY 
                        g.Name";

                var groupRevenues = new List<(string GroupName, decimal TotalRevenue)>();

                using (var command = new SQLiteCommand(groupRevenueQuery, connection))
                {
                    command.Parameters.AddWithValue("@ReportId", reportId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string groupName = reader["Group_Name"].ToString();
                            decimal totalRevenue = Convert.ToDecimal(reader["Total_Revenue"]);

                            groupRevenues.Add((groupName, totalRevenue));
                        }
                    }
                }

                // Создание Excel файла
                var excelApp = new Microsoft.Office.Interop.Excel.Application();
                Workbook workbook = excelApp.Workbooks.Add();
                Worksheet worksheet = (Worksheet)workbook.Sheets[1];

                // Основная информация
                worksheet.Cells[1, 1] = "Дата отчета:";
                worksheet.Cells[1, 2] = reportDate;
                worksheet.Cells[2, 1] = "Название ресторана:";
                worksheet.Cells[2, 2] = restaurantName;

                // Заголовки таблицы
                worksheet.Cells[4, 1] = "Группа";
                worksheet.Cells[4, 2] = "Суммарная выручка";

                // Заполнение данных
                int currentRow = 5;
                foreach (var groupRevenue in groupRevenues)
                {
                    worksheet.Cells[currentRow, 1] = groupRevenue.GroupName;
                    worksheet.Cells[currentRow, 2] = groupRevenue.TotalRevenue;
                    currentRow++;
                }
                DateTime today = DateTime.Today;
                // Сохранение файла
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    Title = "Сохранить отчет по группам",
                    FileName = $"GroupReport.{today.ToString("d")}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    workbook.SaveAs(saveFileDialog.FileName);
                    MessageBox.Show($"Отчет успешно сохранен: {saveFileDialog.FileName}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // Очистка ресурсов
                workbook.Close(false);
                excelApp.Quit();
            }
        }
    }
}
