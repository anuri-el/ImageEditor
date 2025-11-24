using ImageEditor.Models;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageEditor.Commands
{
    public class CropCollageCommand : ICropCommand
    {
        private readonly List<LayerModel> _layers;
        private readonly CropArea _cropArea;
        private readonly Dictionary<LayerModel, LayerMemento> _mementos = new Dictionary<LayerModel, LayerMemento>();

        public CropCollageCommand(List<LayerModel> layers, CropArea cropArea)
        {
            _layers = layers;
            _cropArea = cropArea;
        }

        public bool CanExecute()
        {
            return _layers != null && _layers.Count > 0 && _cropArea != null;
        }

        public void Execute()
        {
            if (!CanExecute()) return;

            // Зберігаємо стан всіх шарів
            foreach (var layer in _layers)
            {
                _mementos[layer] = new LayerMemento(layer);
            }

            // Обрізаємо кожен шар відносно crop області
            foreach (var layer in _layers.ToList())
            {
                if (layer.Image == null) continue;

                // Перевіряємо чи перетинається шар з crop областю
                var layerRect = new Rect(layer.X, layer.Y, layer.Image.PixelWidth, layer.Image.PixelHeight);
                var cropRect = new Rect(_cropArea.X, _cropArea.Y, _cropArea.Width, _cropArea.Height);

                if (!layerRect.IntersectsWith(cropRect))
                {
                    // Видаляємо шари, які не перетинаються
                    _layers.Remove(layer);
                    continue;
                }

                // Обчислюємо область перетину
                var intersection = Rect.Intersect(layerRect, cropRect);

                // Координати crop в локальній системі шару
                double localX = intersection.X - layer.X;
                double localY = intersection.Y - layer.Y;

                // Обрізаємо зображення
                try
                {
                    var croppedBitmap = new CroppedBitmap(layer.Image,
                        new Int32Rect(
                            (int)localX,
                            (int)localY,
                            (int)intersection.Width,
                            (int)intersection.Height));

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

                        layer.Image = result;
                        layer.X = intersection.X - _cropArea.X;
                        layer.Y = intersection.Y - _cropArea.Y;
                    }
                }
                catch
                {
                    // Якщо не вдалося обрізати, видаляємо шар
                    _layers.Remove(layer);
                }
            }
        }

        public void Undo()
        {
            foreach (var kvp in _mementos)
            {
                kvp.Value.Restore(kvp.Key);
                if (!_layers.Contains(kvp.Key))
                {
                    _layers.Add(kvp.Key);
                }
            }
        }

        // Memento Pattern для збереження стану шару
        private class LayerMemento
        {
            public BitmapImage Image { get; }
            public double X { get; }
            public double Y { get; }
            public int Angle { get; }

            public LayerMemento(LayerModel layer)
            {
                Image = layer.Image;
                X = layer.X;
                Y = layer.Y;
                Angle = layer.Angle;
            }

            public void Restore(LayerModel layer)
            {
                layer.Image = Image;
                layer.X = X;
                layer.Y = Y;
                layer.Angle = Angle;
            }
        }
    }
}
