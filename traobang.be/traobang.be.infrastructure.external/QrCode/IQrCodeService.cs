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
        /// Sinh ảnh mã QR ở giữa, khối text căn giữa ở trên và ở dưới mã QR
        /// </summary>
        /// <param name="qrText"></param>
        /// <param name="textAbove">nội dung text phía trên mã QR, xuống dòng bằng \n</param>
        /// <param name="textBelow">nội dung text phía dưới mã QR, xuống dòng bằng \n</param>
        /// <returns></returns>
        public Stream GenerateQrWithText(string qrText, string textAbove, string textBelow);

        /// <summary>
        /// Sinh ảnh 2 khối: khối text bên trái, khối mã QR bên phải
        /// </summary>
        /// <param name="qrText"></param>
        /// <param name="textLines">các dòng text của khối bên trái, vẽ từ trên xuống</param>
        /// <returns></returns>
        public Stream GenerateQrWithText(string qrText, List<QrTextLine> textLines);
    }
}
