using QRCoder;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace QRRewardPlatform.Services
{
    public class QRCodeGeneratorService
    {
        public byte[] GenerateQRCode(string url, string? text = null)
        {
            using var qrGenerator = new QRCoder.QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(url, QRCoder.QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = qrCode.GetGraphic(10);

            if (string.IsNullOrEmpty(text))
            {
                return qrBytes;
            }

            // Load QR image and add text below
            using var image = Image.Load<Rgba32>(qrBytes);
            
            int textHeight = 60; // Increased padding for clearer text
            int newHeight = image.Height + textHeight;
            
            using var combinedImage = new Image<Rgba32>(image.Width, newHeight);
            
            combinedImage.Mutate(ctx => 
            {
                ctx.Fill(Color.White);
                ctx.DrawImage(image, new Point(0, 0), 1f);
            });

            try 
            {
                Font? font = null;
                // Try to find a common system font
                string[] fontFamilies = { "Arial", "Verdana", "Tahoma", "Segoe UI", "DejaVu Sans" };
                foreach (var familyName in fontFamilies)
                {
                    if (SystemFonts.Collection.TryGet(familyName, out var family))
                    {
                        font = family.CreateFont(28, FontStyle.Bold);
                        break;
                    }
                }

                // Windows path fallback if SystemFonts fails
                if (font == null)
                {
                    var windowsFontPath = @"C:\Windows\Fonts\arialbd.ttf";
                    if (System.IO.File.Exists(windowsFontPath))
                    {
                        var family = new FontCollection().Add(windowsFontPath);
                        font = family.CreateFont(28, FontStyle.Bold);
                    }
                }

                if (font != null)
                {
                    combinedImage.Mutate(ctx => 
                    {
                        var point = new PointF(combinedImage.Width / 2f, image.Height + (textHeight / 2f) - 5);
                        var textOptions = new RichTextOptions(font)
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Origin = point
                        };
                        ctx.DrawText(textOptions, text, Color.Black);
                    });
                }
            }
            catch (Exception ex)
            {
                // Fallback recorded in logs - return the original QR or the white-padded one
                Console.WriteLine($"Font drawing error: {ex.Message}");
            }

            using var ms = new MemoryStream();
            combinedImage.SaveAsPng(ms);
            return ms.ToArray();
        }
    }
}
