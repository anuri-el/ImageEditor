using ImageEditor.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageEditor.Commands
{
    public class CropCollageCommand : ICropCommand
    {
        private readonly ObservableCollection<LayerModel> _layers;
        private readonly CropArea _cropArea;
        private readonly Dictionary<LayerModel, LayerMemento> _mementos = new Dictionary<LayerModel, LayerMemento>();
        private readonly List<LayerModel> _removedLayers = new List<LayerModel>();

        public CropCollageCommand(ObservableCollection<LayerModel> layers, CropArea cropArea)
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

            try
            {
                // Зберігаємо стан всіх шарів
                foreach (var layer in _layers.ToList())
                {
                    _mementos[layer] = new LayerMemento(layer);
                }

                var cropRect = new Rect(_cropArea.X, _cropArea.Y, _cropArea.Width, _cropArea.Height);

                // Обрізаємо кожен шар
                foreach (var layer in _layers.ToList())
                {
                    if (layer.Image == null) continue;

                    var layerRect = new Rect(layer.X, layer.Y, layer.Image.PixelWidth, layer.Image.PixelHeight);

                    if (!layerRect.IntersectsWith(cropRect))
                    {
                        // Видаляємо шари, які не перетинаються
                        _layers.Remove(layer);
                        _removedLayers.Add(layer);
                        continue;
                    }

                    // Обчислюємо область перетину
                    var intersection = Rect.Intersect(layerRect, cropRect);

                    // Координати crop в локальній системі шару
                    int localX = (int)Math.Max(0, intersection.X - layer.X);
                    int localY = (int)Math.Max(0, intersection.Y - layer.Y);
                    int localWidth = (int)Math.Min(intersection.Width, layer.Image.PixelWidth - localX);
                    int localHeight = (int)Math.Min(intersection.Height, layer.Image.PixelHeight - localY);

                    if (localWidth <= 0 || localHeight <= 0)
                    {
                        _layers.Remove(layer);
                        _removedLayers.Add(layer);
                        continue;
                    }

                    try
                    {
                        // Обрізаємо зображення
                        var croppedBitmap = new CroppedBitmap(layer.Image,
                            new Int32Rect(localX, localY, localWidth, localHeight));

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

                            layer.Image = result;

                            // Нова позиція відносно crop області
                            layer.X = intersection.X - _cropArea.X;
                            layer.Y = intersection.Y - _cropArea.Y;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Помилка обрізання шару: {ex.Message}");
                        _layers.Remove(layer);
                        _removedLayers.Add(layer);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Помилка при обрізанні колажу: {ex.Message}");
                Undo();
            }
        }

        public void Undo()
        {
            // Відновлюємо всі шари
            foreach (var kvp in _mementos)
            {
                kvp.Value.Restore(kvp.Key);
                if (!_layers.Contains(kvp.Key))
                {
                    _layers.Add(kvp.Key);
                }
            }

            // Очищаємо список видалених
            _removedLayers.Clear();
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
