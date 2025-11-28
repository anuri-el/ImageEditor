using ImageEditor.Models;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageEditor.Commands
{
    public class CropImageCommand : ICropCommand, IUndoableCommand
    {
        private readonly LayerModel _layer;
        private readonly CropArea _cropArea;
        private BitmapImage _originalImage;
        private double _originalX;
        private double _originalY;

        public string Description => "Обрізання зображення";

        public CropImageCommand(LayerModel layer, CropArea cropArea)
        {
            _layer = layer;
            _cropArea = cropArea;
        }

        public bool CanExecute()
        {
            return _layer != null && _layer.Image != null && _cropArea != null;
        }

        public void Execute()
        {
            if (!CanExecute()) return;

            _originalImage = _layer.Image;
            _originalX = _layer.X;
            _originalY = _layer.Y;

            try
            {
                double cropLocalX = _cropArea.X - _layer.X;
                double cropLocalY = _cropArea.Y - _layer.Y;

                cropLocalX = Math.Max(0, cropLocalX);
                cropLocalY = Math.Max(0, cropLocalY);

                double maxWidth = Math.Min(_cropArea.Width, _layer.Image.PixelWidth - cropLocalX);
                double maxHeight = Math.Min(_cropArea.Height, _layer.Image.PixelHeight - cropLocalY);

                if (maxWidth <= 0 || maxHeight <= 0)
                {
                    MessageBox.Show("Неможливо обрізати зображення: область поза межами.");
                    return;
                }

                var croppedImage = CropImage(_layer.Image,
                    (int)cropLocalX,
                    (int)cropLocalY,
                    (int)maxWidth,
                    (int)maxHeight);

                _layer.X = _cropArea.X;
                _layer.Y = _cropArea.Y;
                _layer.Image = croppedImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при обрізанні: {ex.Message}");
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

        private BitmapImage CropImage(BitmapImage source, int x, int y, int width, int height)
        {
            try
            {
                var croppedBitmap = new CroppedBitmap(source,
                    new Int32Rect(x, y, width, height));

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));

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
                throw new Exception($"Помилка створення cropped image: {ex.Message}", ex);
            }
        }
    }
}
