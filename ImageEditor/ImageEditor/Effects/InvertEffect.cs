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
    public class InvertEffect : IImageEffect
    {
        public string Name => "Invert";
        public string Description => "Інверсія кольорів";

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
                    pixels[i] = (byte)(255 - pixels[i]);       // B
                    pixels[i + 1] = (byte)(255 - pixels[i + 1]); // G
                    pixels[i + 2] = (byte)(255 - pixels[i + 2]); // R
                    // Alpha залишається без змін
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
                MessageBox.Show($"Помилка застосування Invert: {ex.Message}");
                return source;
            }
        }
    }
}
