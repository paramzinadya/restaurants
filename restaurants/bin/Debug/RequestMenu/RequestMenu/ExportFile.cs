using System;
using System.Data.SQLite; // Если вы используете SQLite
using ExcelApp = Microsoft.Office.Interop.Excel.Application;
using System.Windows.Forms; // Для сохранения файла через диалог
using Microsoft.Office.Interop.Excel;

namespace RequestMenu
{
    public static class ExportFile
    {
        [STAThread] // Требуется для использования FileDialog
        public static void ExportRequestToExcel(int requestId)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Получение основной информации о заявке
                string requestQuery = @"SELECT r.Date, res.Name as Restaurant
                                    FROM Requests r
                                    JOIN Restaurants res ON r.Restaurant_id = res.Id
                                    WHERE r.Id = @RequestId";

                var command = new SQLiteCommand(requestQuery, connection);
                command.Parameters.AddWithValue("@RequestId", requestId);

                string date = "";
                string restaurant = "";

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        date = reader["Date"].ToString();
                        restaurant = reader["Restaurant"].ToString();
                    }
                }

                // Получение деталей заявки
                string detailsQuery = @"SELECT p.Name as Product, rd.Count, mu.Name as MeasurementUnit
                                    FROM Requests_details rd
                                    JOIN Products p ON rd.Product_id = p.Id
                                    JOIN Measurement_units mu ON rd.Measurement_unit_id = mu.Id
                                    WHERE rd.Request_id = @RequestId";

                var detailsCommand = new SQLiteCommand(detailsQuery, connection);
                detailsCommand.Parameters.AddWithValue("@RequestId", requestId);

                var details = new System.Collections.Generic.List<(string Product, int Count, string Unit)>();

                using (var reader = detailsCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string product = reader["Product"].ToString();
                        int count = int.Parse(reader["Count"].ToString());
                        string unit = reader["MeasurementUnit"].ToString();
                        details.Add((product, count, unit));
                    }
                }

                // Создание Excel файла
                var excelApp = new ExcelApp();
                Workbook workbook = excelApp.Workbooks.Add();
                Worksheet worksheet = workbook.Sheets[1];

                // Заполнение данных
                worksheet.Cells[1, 1] = "Дата:";
                worksheet.Cells[1, 2] = date;
                worksheet.Cells[2, 1] = "Ресторан:";
                worksheet.Cells[2, 2] = restaurant;

                worksheet.Cells[4, 1] = "Название продукта";
                worksheet.Cells[4, 2] = "Количество";
                worksheet.Cells[4, 3] = "Единица измерения";

                int currentRow = 5;
                foreach (var detail in details)
                {
                    worksheet.Cells[currentRow, 1] = detail.Product;
                    worksheet.Cells[currentRow, 2] = detail.Count;
                    worksheet.Cells[currentRow, 3] = detail.Unit;
                    currentRow++;
                }

                // Выбор пути для сохранения файла
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    Title = "Save an Excel File",
                    FileName = "ExportedRequest.xlsx"
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    workbook.SaveAs(saveFileDialog.FileName);
                    MessageBox.Show("Заявка успешно сохранена в " + saveFileDialog.FileName, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // Очистка ресурсов
                workbook.Close(SaveChanges: false);
                excelApp.Quit();
            }
        }
    }

}