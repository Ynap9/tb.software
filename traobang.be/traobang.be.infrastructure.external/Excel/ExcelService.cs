using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using traobang.be.shared.HttpRequest.AppException;
using traobang.be.shared.HttpRequest.Error;

namespace traobang.be.infrastructure.external.Excel
{
    public class ExcelService : IExcelService
    {
        public List<List<string>> ReadExcelFile(IFormFile file, string sheetName)
        {
            var result = new List<List<string>>();
            using var stream = new MemoryStream();
            file.CopyTo(stream);
            stream.Position = 0;
            using var workbook = new XLWorkbook(stream);
            IXLWorksheet worksheet;
            if (workbook.Worksheets.TryGetWorksheet(sheetName, out worksheet) == false)
            {
                if (workbook.Worksheets.Any())
                {
                    worksheet = workbook.Worksheets.First();
                }
                else
                {
                    throw new UserFriendlyException(ErrorCodes.ImportExcelSheetErrorNotFound,
                        string.Format(ErrorMessages.GetMessage(ErrorCodes.ImportExcelSheetErrorNotFound), sheetName));
                }
            }
            var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            for (int rowNumber = 1; rowNumber <= lastRowUsed; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);
                var rowData = new List<string>();
                var lastColumnUsed = row.LastCellUsed()?.Address.ColumnNumber ?? 0;
                for (int col = 1; col <= Math.Max(lastColumnUsed, 20); col++)
                {
                    var cell = row.Cell(col);
                    var cellValue = string.Empty;
                    if (!cell.IsEmpty())
                    {
                        cellValue = cell.GetFormattedString();
                    }
                    rowData.Add(cellValue);
                }
                result.Add(rowData);
            }
            return result;
        }

        public byte[] WriteExcelFile(List<string> headers, List<List<string>> rows, string sheetName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            for (int col = 0; col < headers.Count; col++)
            {
                var cell = worksheet.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
            }

            for (int row = 0; row < rows.Count; row++)
            {
                for (int col = 0; col < rows[row].Count; col++)
                {
                    worksheet.Cell(row + 2, col + 1).Value = rows[row][col];
                }
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
