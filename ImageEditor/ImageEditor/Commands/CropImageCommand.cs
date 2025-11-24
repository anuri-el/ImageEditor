using ImageEditor.Models;
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

            // Виконуємо crop
            var croppedImage = CropImage(_layer.Image, _cropArea);
            _layer.Image = croppedImage;

            // Оновлюємо позицію
            _layer.X += _cropArea.X;
            _layer.Y += _cropArea.Y;
        }

        public void Undo()
        {
            if (_originalImage == null) return;

            _layer.Image = _originalImage;
            _layer.X = _originalX;
            _layer.Y = _originalY;
        }

        private BitmapImage CropImage(BitmapImage source, CropArea area)
        {
            var croppedBitmap = new CroppedBitmap(source,
                new System.Windows.Int32Rect(
                    (int)area.X,
                    (int)area.Y,
                    (int)area.Width,
                    (int)area.Height));

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));

            using (var stream = new System.IO.MemoryStream())
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
