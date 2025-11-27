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
    public class GrayscaleEffect : IImageEffect
    {
        public string Name => "Grayscale";
        public string Description => "Чорно-білий ефект";

        public BitmapImage Apply(BitmapImage source)
        {
            if (source == null) return null;

            try
            {
                var formatConvertedBitmap = new FormatConvertedBitmap();
                formatConvertedBitmap.BeginInit();
                formatConvertedBitmap.Source = source;
                formatConvertedBitmap.DestinationFormat = PixelFormats.Gray8;
                formatConvertedBitmap.EndInit();

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(formatConvertedBitmap));

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
                MessageBox.Show($"Помилка застосування Grayscale: {ex.Message}");
                return source;
            }
        }
    }
}
