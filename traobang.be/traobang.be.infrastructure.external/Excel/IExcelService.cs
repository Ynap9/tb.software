using Microsoft.AspNetCore.Http;

namespace traobang.be.infrastructure.external.Excel
{
    public interface IExcelService
    {
        public List<List<string>> ReadExcelFile(IFormFile file, string sheetName);

        /// <summary>
        /// Ghi dữ liệu ra file excel, dòng đầu là tiêu đề cột
        /// </summary>
        /// <param name="headers">tiêu đề các cột</param>
        /// <param name="rows">dữ liệu, mỗi phần tử là một dòng</param>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        public byte[] WriteExcelFile(List<string> headers, List<List<string>> rows, string sheetName);
    }
}
