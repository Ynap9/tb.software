namespace traobang.be.infrastructure.external.QrCode.Dtos
{
    public class QrTextLine
    {
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Cỡ chữ của dòng này
        /// </summary>
        public float FontSize { get; set; }

        /// <summary>
        /// Dòng chiếm hết khoảng trống còn lại theo chiều dọc, chữ căn giữa khoảng trống đó
        /// </summary>
        public bool FillRemaining { get; set; }
    }
}
