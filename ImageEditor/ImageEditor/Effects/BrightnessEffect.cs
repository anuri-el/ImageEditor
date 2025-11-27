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
    public class BrightnessEffect : IImageEffect
    {
        private readonly int _adjustment;

        public string Name { get; }
        public string Description { get; }

        public BrightnessEffect(int adjustment)
        {
            _adjustment = adjustment;
            Name = adjustment > 0 ? "Brighten" : "Darken";
            Description = adjustment > 0 ? "Світліший" : "Темніший";
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
                    pixels[i] = Clamp(pixels[i] + _adjustment);       // B
                    pixels[i + 1] = Clamp(pixels[i + 1] + _adjustment); // G
                    pixels[i + 2] = Clamp(pixels[i + 2] + _adjustment); // R
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
                MessageBox.Show($"Помилка застосування Brightness: {ex.Message}");
                return source;
            }
        }

        private byte Clamp(int value)
        {
            return (byte)Math.Max(0, Math.Min(255, value));
        }
    }
}
