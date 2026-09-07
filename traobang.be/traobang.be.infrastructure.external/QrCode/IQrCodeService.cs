using traobang.be.infrastructure.external.QrCode.Dtos;

namespace traobang.be.infrastructure.external.QrCode
{
    public interface IQrCodeService
    {
        /// <summary>
        /// Sinh mã QR cho nội dung text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Stream GenQrCodeByText(string text);

        /// <summary>
        /// Sinh ảnh 2 khối: khối text bên trái, khối mã QR bên phải
        /// </summary>
        /// <param name="qrText"></param>
        /// <param name="textLines">các dòng text của khối bên trái, vẽ từ trên xuống</param>
        /// <returns></returns>
        public Stream GenerateQrWithText(string qrText, List<QrTextLine> textLines);
    }
}
