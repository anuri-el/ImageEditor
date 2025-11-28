using ImageEditor.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageEditor.Commands
{
    public class CropCollageCommand : ICropCommand, IUndoableCommand
    {
        private readonly ObservableCollection<LayerModel> _layers;
        private readonly CropArea _cropArea;
        private readonly Dictionary<LayerModel, LayerMemento> _mementos = new Dictionary<LayerModel, LayerMemento>();
        private readonly List<LayerModel> _removedLayers = new List<LayerModel>();

        public string Description => "Обрізання колажу";

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
                var layersToProcess = _layers.ToList();
                foreach (var layer in layersToProcess)
                {
                    _mementos[layer] = new LayerMemento(layer);
                }

                var cropRect = new Rect(_cropArea.X, _cropArea.Y, _cropArea.Width, _cropArea.Height);

                foreach (var layer in layersToProcess)
                {
                    if (layer.Image == null) continue;

                    var layerRect = new Rect(layer.X, layer.Y, layer.Image.PixelWidth, layer.Image.PixelHeight);

                    if (!layerRect.IntersectsWith(cropRect))
                    {
                        _layers.Remove(layer);
                        _removedLayers.Add(layer);
                        continue;
                    }

                    var intersection = Rect.Intersect(layerRect, cropRect);

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
                        var croppedImage = CropImage(layer.Image, localX, localY, localWidth, localHeight);

                        double newX = intersection.X - _cropArea.X;
                        double newY = intersection.Y - _cropArea.Y;

                        layer.X = newX;
                        layer.Y = newY;
                        layer.Image = croppedImage;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Помилка обрізання шару: {ex.Message}");
                        _layers.Remove(layer);
                        _removedLayers.Add(layer);
                    }
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при обрізанні колажу: {ex.Message}");
                Undo();
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

            _removedLayers.Clear();
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
