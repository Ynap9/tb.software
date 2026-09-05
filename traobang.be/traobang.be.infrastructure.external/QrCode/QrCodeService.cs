using Microsoft.Extensions.Logging;
using QRCoder;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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

        public Stream GenerateQrWithText(string qrText, string textAbove, string textBelow)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            byte[] qrBytes = qrCode.GetGraphic(20);
            using Image<Rgba32> qrImage = Image.Load<Rgba32>(qrBytes);

            int padding = 5;
            float lineSpacing = 5;
            float fontSize = 64;
            Font font = SystemFonts.CreateFont("Arial", fontSize, FontStyle.Bold);

            var aboveLines = string.IsNullOrEmpty(textAbove) ? Array.Empty<string>() : textAbove.Split('\n');
            var belowLines = string.IsNullOrEmpty(textBelow) ? Array.Empty<string>() : textBelow.Split('\n');

            // Use font size as fixed line height — reliable across all lines
            float lineHeight = fontSize * 1.2f;

            float maxTextWidth = 0;
            List<float> aboveLineWidths = new();
            List<float> belowLineWidths = new();
            foreach (var line in aboveLines)
            {
                var size = TextMeasurer.MeasureSize(line, new TextOptions(font));
                aboveLineWidths.Add(size.Width);
                if (size.Width > maxTextWidth)
                    maxTextWidth = size.Width;
            }
            foreach (var line in belowLines)
            {
                var size = TextMeasurer.MeasureSize(line, new TextOptions(font));
                belowLineWidths.Add(size.Width);
                if (size.Width > maxTextWidth)
                    maxTextWidth = size.Width;
            }

            // mỗi dòng chiếm lineHeight, cộng thêm lineSpacing để cách dòng tiếp theo
            float totalAboveHeight = aboveLines.Length * (lineHeight + lineSpacing);
            float totalBelowHeight = belowLines.Length * (lineHeight + lineSpacing);

            int width = (int)Math.Max(qrImage.Width, maxTextWidth + padding * 2);
            int height = (int)(totalAboveHeight + qrImage.Height + totalBelowHeight + padding * 3);

            var finalImage = new Image<Rgba32>(width, height, Color.White);
            finalImage.Mutate(ctx =>
            {
                float currentY = padding;

                // text phía trên QR
                for (int i = 0; i < aboveLines.Length; i++)
                {
                    float textX = (width - aboveLineWidths[i]) / 2;
                    ctx.DrawText(aboveLines[i], font, Color.Black, new PointF(textX, currentY));
                    currentY += lineHeight + lineSpacing; // lineHeight advances, lineSpacing adds gap
                }

                int qrX = (width - qrImage.Width) / 2;
                int qrY = (int)currentY;
                ctx.DrawImage(qrImage, new Point(qrX, qrY), 1f);

                // ô trống hình vuông ở giữa QR, cạnh bằng 1/3 cạnh ảnh QR
                float holeSize = qrImage.Width / 3f;
                ctx.Fill(
                    Color.White,
                    new RectangularPolygon(
                        qrX + (qrImage.Width - holeSize) / 2f,
                        qrY + (qrImage.Height - holeSize) / 2f,
                        holeSize,
                        holeSize
                    )
                );

                currentY += qrImage.Height + padding;

                // text phía dưới QR
                for (int i = 0; i < belowLines.Length; i++)
                {
                    float textX = (width - belowLineWidths[i]) / 2;
                    ctx.DrawText(belowLines[i], font, Color.Black, new PointF(textX, currentY));
                    currentY += lineHeight + lineSpacing;
                }
            });

            MemoryStream ms = new MemoryStream();
            finalImage.SaveAsJpeg(ms);
            ms.Position = 0;
            return ms;
        }
    }
}
