using ImageEditor.Models;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageEditor.Commands
{
    public class CropImageCommand : ICropCommand
    {
        private readonly LayerModel _layer;
        private readonly CropArea _cropArea;
        private BitmapImage _originalImage;
        private double _originalX;
        private double _originalY;

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

            // Зберігаємо оригінал для Undo
            _originalImage = _layer.Image;
            _originalX = _layer.X;
            _originalY = _layer.Y;

            try
            {
                // Обчислюємо область crop в координатах шару
                double cropLocalX = _cropArea.X - _layer.X;
                double cropLocalY = _cropArea.Y - _layer.Y;

                // Перевіряємо межі
                cropLocalX = Math.Max(0, cropLocalX);
                cropLocalY = Math.Max(0, cropLocalY);

                double maxWidth = Math.Min(_cropArea.Width, _layer.Image.PixelWidth - cropLocalX);
                double maxHeight = Math.Min(_cropArea.Height, _layer.Image.PixelHeight - cropLocalY);

                if (maxWidth <= 0 || maxHeight <= 0)
                {
                    System.Windows.MessageBox.Show("Неможливо обрізати зображення: область поза межами.");
                    return;
                }

                // Виконуємо crop
                var croppedImage = CropImage(_layer.Image,
                    (int)cropLocalX,
                    (int)cropLocalY,
                    (int)maxWidth,
                    (int)maxHeight);

                _layer.Image = croppedImage;

                // Оновлюємо позицію - зображення залишається на місці crop області
                _layer.X = _cropArea.X;
                _layer.Y = _cropArea.Y;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Помилка при обрізанні: {ex.Message}");
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
            // Створюємо CroppedBitmap
            var croppedBitmap = new CroppedBitmap(source,
                new Int32Rect(x, y, width, height));

            // Конвертуємо в BitmapImage
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
    }
}
