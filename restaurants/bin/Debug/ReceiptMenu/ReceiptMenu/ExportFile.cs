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
        public static void ExportRequestToExcel(int receiptId)
        {
            string connectionString = "Data Source=restaurants_database.db;Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Получение основной информации о заявке
                string requestQuery = @"SELECT r.Date, s.Name as Supplier
                                    FROM Receipts r
                                    JOIN Suppliers s ON r.Supplier_id = s.Id
                                    WHERE r.Id = @ReceiptId";

                var command = new SQLiteCommand(requestQuery, connection);
                command.Parameters.AddWithValue("@ReceiptId", receiptId);

                string date = "";
                string restaurant = "";

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        date = reader["Date"].ToString();
                        restaurant = reader["Supplier"].ToString();
                    }
                }

                // Получение деталей заявки
                string detailsQuery = @"SELECT p.Name as Product, rd.Count, mu.Name as MeasurementUnit, rd.Price
                                    FROM Receipts_details rd
                                    JOIN Products p ON rd.Product_id = p.Id
                                    JOIN Measurement_units mu ON rd.Measurement_unit_id = mu.Id
                                    WHERE rd.Receipt_id = @ReceiptId";

                var detailsCommand = new SQLiteCommand(detailsQuery, connection);
                detailsCommand.Parameters.AddWithValue("@ReceiptId", receiptId);

                var details = new System.Collections.Generic.List<(string Product, int Count, string Unit, int Price)>();

                using (var reader = detailsCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string product = reader["Product"].ToString();
                        int count = int.Parse(reader["Count"].ToString());
                        string unit = reader["MeasurementUnit"].ToString();
                        int price = int.Parse(reader["Price"].ToString());
                        details.Add((product, count, unit, price));
                    }
                }

                // Создание Excel файла
                var excelApp = new ExcelApp();
                Workbook workbook = excelApp.Workbooks.Add();
                Worksheet worksheet = (Worksheet)workbook.Sheets[1];

                // Заполнение данных
                worksheet.Cells[1, 1] = "Дата:";
                worksheet.Cells[1, 2] = date;
                worksheet.Cells[2, 1] = "Поставщик:";
                worksheet.Cells[2, 2] = restaurant;

                worksheet.Cells[4, 1] = "Название продукта";
                worksheet.Cells[4, 2] = "Количество";
                worksheet.Cells[4, 3] = "Единица измерения";
                worksheet.Cells[4, 4] = "Закупочная цена";

                int currentRow = 5;
                foreach (var detail in details)
                {
                    worksheet.Cells[currentRow, 1] = detail.Product;
                    worksheet.Cells[currentRow, 2] = detail.Count;
                    worksheet.Cells[currentRow, 3] = detail.Unit;
                    worksheet.Cells[currentRow, 4] = detail.Price;
                    currentRow++;
                }

                // Выбор пути для сохранения файла
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    Title = "Save an Excel File",
                    FileName = "ExportedReceipt.xlsx"
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    workbook.SaveAs(saveFileDialog.FileName);
                    MessageBox.Show("Поставка успешно сохранена в " + saveFileDialog.FileName, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // Очистка ресурсов
                workbook.Close(SaveChanges: false);
                excelApp.Quit();
            }
        }
    }

}