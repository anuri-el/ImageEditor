using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageEditor.Effects
{
    public class ContrastEffect : IImageEffect
    {
        private readonly double _factor;

        public string Name => "Contrast";
        public string Description { get; }

        public ContrastEffect(double factor)
        {
            _factor = factor;
            Description = factor > 1.0 ? "Високий контраст" : "Низький контраст";
        }

        public BitmapImage Apply(BitmapImage source)
        {
            if (source == null) return null;

            try
            {
                int width = source.PixelWidth;
                int height = source.PixelHeight;
                int stride = width * 4;
                byte[] pixels = new byte[height * stride];

                source.CopyPixels(pixels, stride, 0);

                for (int i = 0; i < pixels.Length; i += 4)
                {
                    pixels[i] = ApplyContrast(pixels[i]);       // B
                    pixels[i + 1] = ApplyContrast(pixels[i + 1]); // G
                    pixels[i + 2] = ApplyContrast(pixels[i + 2]); // R
                }

                var bitmap = BitmapSource.Create(
                    width, height, 96, 96,
                    PixelFormats.Bgra32, null, pixels, stride);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Position = 0;

                    var result = new BitmapImage();
                    result.BeginInit();
                    result.CacheOption = BitmapCacheOption.OnLoad;
                    result.StreamSource = stream;
                    result.EndInit();
                    result.Freeze();

                    return result;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка застосування Contrast: {ex.Message}");
                return source;
            }
        }

        private byte ApplyContrast(byte value)
        {
            double normalized = value / 255.0;
            double contrasted = ((normalized - 0.5) * _factor) + 0.5;
            return (byte)Math.Max(0, Math.Min(255, contrasted * 255));
        }
    }
}
