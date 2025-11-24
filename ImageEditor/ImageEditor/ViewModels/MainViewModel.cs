using ImageEditor.Commands;
using ImageEditor.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageEditor.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public ObservableCollection<LayerModel> Layers { get; set; }
            = new ObservableCollection<LayerModel>();

        private LayerModel _selectedLayer;
        public LayerModel SelectedLayer
        {
            get => _selectedLayer;
            set
            {
                _selectedLayer = value;
                OnPropertyChanged();
                LayerSelected?.Invoke();
            }
        }

        public event Action LayerSelected;
        public event Action RotationChanged;

        public RelayCommand AddImageCommand { get; }
        public RelayCommand SelectLayerCommand { get; }
        public RelayCommand SaveCommand { get; }
        public ICommand RotateLeftCommand { get; }
        public ICommand RotateRightCommand { get; }

        public MainViewModel()
        {
            AddImageCommand = new RelayCommand(AddImage);
            SelectLayerCommand = new RelayCommand(o => SelectLayer(o));
            SaveCommand = new RelayCommand(SaveCollage);

            RotateLeftCommand = new RelayCommand(_ => Rotate(-90));
            RotateRightCommand = new RelayCommand(_ => Rotate(90));
        }

        private void AddImage()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Image Files|*.png;*.jpg;*.bmp;*.tiff;*.gif";

            if (dialog.ShowDialog() == true)
            {
                var img = new BitmapImage(new System.Uri(dialog.FileName));

                var layer = new LayerModel
                {
                    Image = img,
                    X = 0,
                    Y = 0
                };

                Layers.Add(layer);
                SelectedLayer = layer;
            }
        }

        private void SelectLayer(object obj)
        {
            if (obj is LayerModel layer)
            {
                foreach (var l in Layers)
                    l.IsSelected = false;

                layer.IsSelected = true;
                SelectedLayer = layer;
            }
        }

        private void SaveCollage()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter =
                "PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg|Bitmap (*.bmp)|*.bmp|TIFF (*.tiff)|*.tiff|GIF (*.gif)|*.gif";

            if (dlg.ShowDialog() != true)
                return;

            var canvas = Application.Current.MainWindow.FindName("EditorCanvas") as Canvas;
            if (canvas == null)
            {
                MessageBox.Show("Canvas не знайдено!");
                return;
            }

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = 0, maxY = 0;

            foreach (var layer in Layers)
            {
                if (layer.Image == null) continue;

                double right = layer.X + layer.Image.PixelWidth;
                double bottom = layer.Y + layer.Image.PixelHeight;

                if (layer.X < minX) minX = layer.X;
                if (layer.Y < minY) minY = layer.Y;

                if (right > maxX) maxX = right;
                if (bottom > maxY) maxY = bottom;
            }

            if (minX == double.MaxValue)
                return;

            double finalWidth = maxX - minX;
            double finalHeight = maxY - minY;

            if (finalWidth <= 0 || finalHeight <= 0)
            {
                MessageBox.Show("Шари порожні.");
                return;
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)finalWidth,
                (int)finalHeight,
                96, 96,
                PixelFormats.Pbgra32);

            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                ctx.PushTransform(new TranslateTransform(-minX, -minY));
                ctx.DrawRectangle(new VisualBrush(canvas), null, new Rect(new Point(), new Size(canvas.Width, canvas.Height)));
            }

            rtb.Render(dv);

            BitmapEncoder encoder;

            string ext = System.IO.Path.GetExtension(dlg.FileName).ToLower();

            switch (ext)
            {
                case ".jpg":
                    encoder = new JpegBitmapEncoder();
                    break;
                case ".bmp":
                    encoder = new BmpBitmapEncoder();
                    break;
                case ".tiff":
                    encoder = new TiffBitmapEncoder();
                    break;
                case ".gif":
                    encoder = new GifBitmapEncoder();
                    break;
                default:
                    encoder = new PngBitmapEncoder();
                    break;
            }

            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (FileStream fs = new FileStream(dlg.FileName, FileMode.Create))
                encoder.Save(fs);

            MessageBox.Show("Файл збережено успішно!");
        }

        private void Rotate(int angle)
        {
            if (SelectedLayer != null)
            {
                // Обертаємо тільки вибраний шар
                SelectedLayer.Angle = (SelectedLayer.Angle + angle) % 360;
            }
            else if (Layers.Count > 0)
            {
                // Обертаємо весь колаж як одне ціле
                RotateCollage(angle);
            }

            RotationChanged?.Invoke();
        }

        private void RotateCollage(int angleDelta)
        {
            if (Layers.Count == 0) return;

            // Знаходимо центр колажу
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var layer in Layers)
            {
                if (layer.Image == null) continue;

                double centerX = layer.X + layer.Image.PixelWidth / 2.0;
                double centerY = layer.Y + layer.Image.PixelHeight / 2.0;

                if (centerX < minX) minX = centerX;
                if (centerX > maxX) maxX = centerX;
                if (centerY < minY) minY = centerY;
                if (centerY > maxY) maxY = centerY;
            }

            double collageCenterX = (minX + maxX) / 2.0;
            double collageCenterY = (minY + maxY) / 2.0;

            double angleRad = angleDelta * Math.PI / 180.0;
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);

            // Обертаємо кожен шар відносно центру колажу
            foreach (var layer in Layers)
            {
                if (layer.Image == null) continue;

                // Поточний центр шару
                double layerCenterX = layer.X + layer.Image.PixelWidth / 2.0;
                double layerCenterY = layer.Y + layer.Image.PixelHeight / 2.0;

                // Вектор від центру колажу до центру шару
                double dx = layerCenterX - collageCenterX;
                double dy = layerCenterY - collageCenterY;

                // Обертаємо вектор
                double newDx = dx * cos - dy * sin;
                double newDy = dx * sin + dy * cos;

                // Нові координати центру шару
                double newCenterX = collageCenterX + newDx;
                double newCenterY = collageCenterY + newDy;

                // Перераховуємо позицію верхнього лівого кута
                layer.X = newCenterX - layer.Image.PixelWidth / 2.0;
                layer.Y = newCenterY - layer.Image.PixelHeight / 2.0;

                // Обертаємо сам шар
                layer.Angle = (layer.Angle + angleDelta) % 360;
            }
        }
    }
}
