using ImageEditor.Models;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageEditor.Commands
{
    public class ResizeImageCommand : IResizeCommand, IUndoableCommand
    {
        private readonly LayerModel _layer;
        private readonly double _newWidth;
        private readonly double _newHeight;
        private BitmapImage _originalImage;
        private double _originalX;
        private double _originalY;

        public string Description => "Зміна розміру зображення";

        public ResizeImageCommand(LayerModel layer, double newWidth, double newHeight)
        {
            _layer = layer;
            _newWidth = newWidth;
            _newHeight = newHeight;
        }

        public bool CanExecute()
        {
            return _layer != null && _layer.Image != null && _newWidth > 0 && _newHeight > 0;
        }

        public void Execute()
        {
            if (!CanExecute()) return;

            // Зберігаємо оригінал
            _originalImage = _layer.Image;
            _originalX = _layer.X;
            _originalY = _layer.Y;

            try
            {
                // Resize зображення
                var resizedImage = ResizeImage(_layer.Image, (int)_newWidth, (int)_newHeight);
                // Оновлюємо зображення (позиція залишається незмінною)
                _layer.Image = resizedImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при зміні розміру: {ex.Message}");
                Undo();
            }
        }

        public void Undo()
        {
            if (_originalImage == null) return;

            _layer.Image = _originalImage;
            _layer.X = _originalX;
            _layer.Y = _originalY;
        }

        private BitmapImage ResizeImage(BitmapImage source, int width, int height)
        {
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawImage(source, new Rect(0, 0, width, height));
            }

            var renderTargetBitmap = new RenderTargetBitmap(
                width, height, 96, 96, PixelFormats.Pbgra32);
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
    }
}
