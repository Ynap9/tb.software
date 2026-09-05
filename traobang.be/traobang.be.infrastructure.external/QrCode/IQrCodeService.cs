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
        /// Sinh mã QR với text bên trên và bên dưới
        /// </summary>
        /// <param name="qrText"></param>
        /// <param name="textAbove"></param>
        /// <param name="textBelow"></param>
        /// <returns></returns>
        public Stream GenerateQrWithText(string qrText, string textAbove, string textBelow);
    }
}
