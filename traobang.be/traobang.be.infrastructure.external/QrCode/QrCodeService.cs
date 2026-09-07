using Microsoft.Extensions.Logging;
using QRCoder;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using traobang.be.infrastructure.external.QrCode.Dtos;

namespace traobang.be.infrastructure.external.QrCode
{
    public class QrCodeService : IQrCodeService
    {
        private readonly ILogger _logger;
        public QrCodeService(ILogger<QrCodeService> logger)
        {
            _logger = logger;
        }

        public Stream GenQrCodeByText(string text)
        {
            _logger.LogInformation($"{nameof(GenQrCodeByText)}, text = {text}");

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q)
            )
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] qrCodeImage = qrCode.GetGraphic(20);
                Stream stream = new MemoryStream(qrCodeImage);
                return stream;
            }
        }

        public Stream GenerateQrWithText(string qrText, List<QrTextLine> textLines)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            int pixelsPerModule = 20;
            byte[] qrBytes = qrCode.GetGraphic(pixelsPerModule);
            using Image<Rgba32> qrImage = Image.Load<Rgba32>(qrBytes);

            int qrQuietZone = pixelsPerModule * 4; // QRCoder chừa sẵn viền trắng 4 module quanh mã
            int padding = 5;
            int paddingLeft = 40; // lề trái cho khối text đỡ sát mép ảnh
            float lineSpacing = 5;

            var lines = textLines ?? new List<QrTextLine>();

            // đo trước font và kích thước từng dòng vì mỗi dòng có cỡ chữ riêng
            List<Font> fonts = new();
            List<float> lineHeights = new();
            float maxTextWidth = 0;
            float totalTextHeight = 0;
            foreach (var line in lines)
            {
                Font font = SystemFonts.CreateFont("Arial", line.FontSize, FontStyle.Bold);
                fonts.Add(font);

                var size = TextMeasurer.MeasureSize(line.Text, new TextOptions(font));
                if (size.Width > maxTextWidth)
                    maxTextWidth = size.Width;

                // Use font size as fixed line height — reliable across all lines
                float lineHeight = line.FontSize * 1.2f;
                lineHeights.Add(lineHeight);

                // mỗi dòng chiếm lineHeight, cộng thêm lineSpacing để cách dòng tiếp theo
                totalTextHeight += lineHeight + lineSpacing;
            }

            // khối bên trái là text, khối bên phải là mã QR
            int leftWidth = (int)(maxTextWidth + paddingLeft + padding);
            int width = leftWidth + qrImage.Width + padding;

            // ảnh QR căn giữa theo chiều dọc nên lề trên của nó = (height - qrHeight) / 2,
            // khối text bắt đầu ngang phần đen của mã QR, tức là bỏ qua viền trắng của ảnh QR
            float qrTop = Math.Max(padding, qrQuietZone + totalTextHeight + padding - qrImage.Height);
            float paddingTop = qrTop + qrQuietZone;
            int height = (int)(qrTop * 2 + qrImage.Height);

            var finalImage = new Image<Rgba32>(width, height, Color.White);
            finalImage.Mutate(ctx =>
            {
                // khối text bên trái, vẽ từ trên xuống
                float currentY = paddingTop;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].FillRemaining)
                    {
                        // dòng này chiếm hết khoảng trống còn lại, chữ căn giữa theo chiều dọc
                        float remainHeight = height - padding - currentY;
                        float textY = Math.Max(currentY, currentY + (remainHeight - lineHeights[i]) / 2);
                        ctx.DrawText(lines[i].Text, fonts[i], Color.Black, new PointF(paddingLeft, textY));
                        currentY += remainHeight;
                        continue;
                    }

                    ctx.DrawText(lines[i].Text, fonts[i], Color.Black, new PointF(paddingLeft, currentY));
                    currentY += lineHeights[i] + lineSpacing; // lineHeight advances, lineSpacing adds gap
                }

                // khối QR bên phải, căn giữa theo chiều dọc
                int qrX = leftWidth;
                int qrY = (height - qrImage.Height) / 2;
                ctx.DrawImage(qrImage, new Point(qrX, qrY), 1f);
            });

            MemoryStream ms = new MemoryStream();
            finalImage.SaveAsJpeg(ms);
            ms.Position = 0;
            return ms;
        }
    }
}
