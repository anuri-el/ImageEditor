using ImageEditor.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageEditor.Commands
{
    public class ResizeCollageCommand : IResizeCommand, IUndoableCommand
    {
        private readonly ObservableCollection<LayerModel> _layers;
        private readonly double _scaleX;
        private readonly double _scaleY;
        private readonly Dictionary<LayerModel, LayerMemento> _mementos = new Dictionary<LayerModel, LayerMemento>();

        public string Description => "Зміна розміру колажу";

        public ResizeCollageCommand(ObservableCollection<LayerModel> layers, double scaleX, double scaleY)
        {
            _layers = layers;
            _scaleX = scaleX;
            _scaleY = scaleY;
        }

        public bool CanExecute()
        {
            return _layers != null && _layers.Count > 0 && _scaleX > 0 && _scaleY > 0;
        }

        public void Execute()
        {
            if (!CanExecute()) return;

            try
            {
                // Зберігаємо стан
                foreach (var layer in _layers)
                {
                    _mementos[layer] = new LayerMemento(layer);
                }

                // Resize кожен шар
                foreach (var layer in _layers)
                {
                    if (layer.Image == null) continue;

                    int newWidth = (int)(layer.Image.PixelWidth * _scaleX);
                    int newHeight = (int)(layer.Image.PixelHeight * _scaleY);

                    if (newWidth <= 0 || newHeight <= 0) continue;

                    var resizedImage = ResizeImage(layer.Image, newWidth, newHeight);

                    // Масштабуємо позицію
                    layer.X *= _scaleX;
                    layer.Y *= _scaleY;
                    layer.Image = resizedImage;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при зміні розміру колажу: {ex.Message}");
                Undo();
            }
        }

        public void Undo()
        {
            foreach (var kvp in _mementos)
            {
                kvp.Value.Restore(kvp.Key);
            }
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
