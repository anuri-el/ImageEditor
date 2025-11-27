using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace ImageEditor.Effects
{
    public class BlurEffect : IImageEffect
    {
        private readonly double _radius;

        public string Name => "Blur";
        public string Description { get; }

        public BlurEffect(double radius = 5)
        {
            _radius = radius;
            Description = $"Розмиття ({radius}px)";
        }

        public BitmapImage Apply(BitmapImage source)
        {
            if (source == null) return null;

            try
            {
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    var blurEffect = new System.Windows.Media.Effects.BlurEffect
                    {
                        Radius = _radius,
                        KernelType = KernelType.Gaussian
                    };

                    drawingContext.PushEffect(new BlurBitmapEffect { Radius = _radius }, null);
                    drawingContext.DrawImage(source, new Rect(0, 0, source.PixelWidth, source.PixelHeight));
                    drawingContext.Pop();
                }

                var renderTargetBitmap = new RenderTargetBitmap(
                    source.PixelWidth, source.PixelHeight, 96, 96, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(drawingVisual);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

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
                MessageBox.Show($"Помилка застосування Blur: {ex.Message}");
                return source;
            }
        }
    }
}
