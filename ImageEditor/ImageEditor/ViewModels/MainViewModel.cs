using ImageEditor.Commands;
using ImageEditor.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
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

                if (_selectedLayer != null)
                {
                    SliderAngle = GetCurrentSliderAngle(_selectedLayer.Angle);
                }
                else
                {
                    SliderAngle = 0;
                }
            }
        }

        private double _sliderAngle;
        public double SliderAngle
        {
            get => _sliderAngle;
            set
            {
                if (Math.Abs(_sliderAngle - value) < 0.01) return;

                double delta = value - _sliderAngle;
                _sliderAngle = value;
                OnPropertyChanged();

                ApplySliderRotation(delta);
            }
        }

        private double _canvasWidth = 1500;
        public double CanvasWidth
        {
            get => _canvasWidth;
            set
            {
                _canvasWidth = value;
                OnPropertyChanged();
            }
        }

        private double _canvasHeight = 1000;
        public double CanvasHeight
        {
            get => _canvasHeight;
            set
            {
                _canvasHeight = value;
                OnPropertyChanged();
            }
        }

        // Crop properties
        private bool _isCropMode;
        public bool IsCropMode
        {
            get => _isCropMode;
            set
            {
                if (_isCropMode == value) return;
                _isCropMode = value;
                OnPropertyChanged();

                if (!value)
                {
                    CancelCrop();
                }
            }
        }

        private CropArea _cropArea;
        public CropArea CropArea
        {
            get => _cropArea;
            set
            {
                _cropArea = value;
                OnPropertyChanged();
            }
        }

        private CropRatio _selectedCropRatio;
        public CropRatio SelectedCropRatio
        {
            get => _selectedCropRatio;
            set
            {
                _selectedCropRatio = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCustomRatio));
            }
        }

        public ObservableCollection<CropRatio> CropRatios { get; set; }

        public bool IsCustomRatio => SelectedCropRatio?.Name == "Custom";

        private double _customRatioWidth = 1;
        public double CustomRatioWidth
        {
            get => _customRatioWidth;
            set
            {
                _customRatioWidth = value;
                OnPropertyChanged();
            }
        }

        private double _customRatioHeight = 1;
        public double CustomRatioHeight
        {
            get => _customRatioHeight;
            set
            {
                _customRatioHeight = value;
                OnPropertyChanged();
            }
        }

        public event Action LayerSelected;
        public event Action RotationChanged;
        public event Action CropModeChanged;

        public RelayCommand AddImageCommand { get; }
        public RelayCommand SelectLayerCommand { get; }
        public RelayCommand SaveCommand { get; }
        public ICommand RotateLeftCommand { get; }
        public ICommand RotateRightCommand { get; }
        public RelayCommand StartCropCommand { get; }
        public RelayCommand ApplyCropCommand { get; }
        public RelayCommand CancelCropCommand { get; }

        private ICropCommand _currentCropCommand;

        public MainViewModel()
        {
            AddImageCommand = new RelayCommand(AddImage);
            SelectLayerCommand = new RelayCommand(o => SelectLayer(o));
            SaveCommand = new RelayCommand(SaveCollage);

            RotateLeftCommand = new RelayCommand(_ => Rotate(-90));
            RotateRightCommand = new RelayCommand(_ => Rotate(90));

            StartCropCommand = new RelayCommand(StartCrop);
            ApplyCropCommand = new RelayCommand(ApplyCrop, CanApplyCrop);
            CancelCropCommand = new RelayCommand(CancelCrop);

            CropRatios = new ObservableCollection<CropRatio>(CropRatio.GetPredefinedRatios());
            SelectedCropRatio = CropRatios[0]; // Freeform by default
        }

        private void AddImage()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Image Files|*.png;*.jpg;*.bmp;*.tiff;*.gif";

            if (dialog.ShowDialog() == true)
            {
                var img = new BitmapImage(new System.Uri(dialog.FileName));

                double scale = CalculateScaleToFit(img.PixelWidth, img.PixelHeight);

                int newWidth = (int)(img.PixelWidth * scale);
                int newHeight = (int)(img.PixelHeight * scale);

                BitmapImage scaledImg = img;

                if (scale < 1.0)
                {
                    scaledImg = new BitmapImage();
                    scaledImg.BeginInit();
                    scaledImg.UriSource = new System.Uri(dialog.FileName);
                    scaledImg.DecodePixelWidth = newWidth;
                    scaledImg.DecodePixelHeight = newHeight;
                    scaledImg.CacheOption = BitmapCacheOption.OnLoad;
                    scaledImg.EndInit();
                    scaledImg.Freeze();
                }

                var layer = new LayerModel
                {
                    Image = scaledImg,
                    X = (CanvasWidth - scaledImg.PixelWidth) / 2,
                    Y = (CanvasHeight - scaledImg.PixelHeight) / 2,
                    OriginalWidth = img.PixelWidth,
                    OriginalHeight = img.PixelHeight
                };

                Layers.Add(layer);
                SelectedLayer = layer;
            }
        }

        private double CalculateScaleToFit(double width, double height)
        {
            double maxWidth = CanvasWidth * 0.9;
            double maxHeight = CanvasHeight * 0.9;

            double scaleX = maxWidth / width;
            double scaleY = maxHeight / height;

            double scale = Math.Min(scaleX, scaleY);

            return Math.Min(scale, 1.0);
        }

        public void UpdateCanvasSize(double availableWidth, double availableHeight)
        {
            CanvasWidth = Math.Max(800, availableWidth);
            CanvasHeight = Math.Max(600, availableHeight);
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

            var canvas = Application.Current.MainWindow.FindName("EditorCanvas") as System.Windows.Controls.Canvas;
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
                ctx.DrawRectangle(new VisualBrush(canvas), null, new Rect(new Point(), new Size(canvas.ActualWidth, canvas.ActualHeight)));
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
            if (IsCropMode)
            {
                IsCropMode = false;
            }

            if (SelectedLayer != null)
            {
                SelectedLayer.Angle = (SelectedLayer.Angle + angle) % 360;
                SliderAngle = GetCurrentSliderAngle(SelectedLayer.Angle);
            }
            else if (Layers.Count > 0)
            {
                RotateCollage(angle);
                SliderAngle = 0;
            }

            RotationChanged?.Invoke();
        }

        private void RotateCollage(int angleDelta)
        {
            if (Layers.Count == 0) return;

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

            foreach (var layer in Layers)
            {
                if (layer.Image == null) continue;

                double layerCenterX = layer.X + layer.Image.PixelWidth / 2.0;
                double layerCenterY = layer.Y + layer.Image.PixelHeight / 2.0;

                double dx = layerCenterX - collageCenterX;
                double dy = layerCenterY - collageCenterY;

                double newDx = dx * cos - dy * sin;
                double newDy = dx * sin + dy * cos;

                double newCenterX = collageCenterX + newDx;
                double newCenterY = collageCenterY + newDy;

                layer.X = newCenterX - layer.Image.PixelWidth / 2.0;
                layer.Y = newCenterY - layer.Image.PixelHeight / 2.0;

                layer.Angle = (layer.Angle + angleDelta) % 360;
            }
        }

        private void ApplySliderRotation(double delta)
        {
            if (IsCropMode)
            {
                IsCropMode = false;
            }

            if (SelectedLayer != null)
            {
                SelectedLayer.Angle = NormalizeAngle(SelectedLayer.Angle + delta);
            }
            else if (Layers.Count > 0)
            {
                RotateCollage((int)Math.Round(delta));
            }

            RotationChanged?.Invoke();
        }

        private double GetCurrentSliderAngle(int angle)
        {
            int normalized = ((angle % 360) + 360) % 360;
            int baseAngle = (int)(Math.Round(normalized / 90.0) * 90) % 360;
            int diff = normalized - baseAngle;

            if (diff > 180) diff -= 360;
            if (diff < -180) diff += 360;

            return Math.Max(-45, Math.Min(45, diff));
        }

        private int NormalizeAngle(double angle)
        {
            int result = ((int)Math.Round(angle) % 360);
            if (result < 0) result += 360;
            return result;
        }

        // Crop methods
        private void StartCrop()
        {
            IsCropMode = true;

            // Ініціалізуємо crop область
            if (SelectedLayer != null && SelectedLayer.Image != null)
            {
                // Crop для вибраного шару
                CropArea = new CropArea
                {
                    X = SelectedLayer.X + SelectedLayer.Image.PixelWidth * 0.1,
                    Y = SelectedLayer.Y + SelectedLayer.Image.PixelHeight * 0.1,
                    Width = SelectedLayer.Image.PixelWidth * 0.8,
                    Height = SelectedLayer.Image.PixelHeight * 0.8
                };
            }
            else if (Layers.Count > 0)
            {
                // Crop для всього колажу
                double minX = Layers.Min(l => l.X);
                double minY = Layers.Min(l => l.Y);
                double maxX = Layers.Max(l => l.X + l.Image.PixelWidth);
                double maxY = Layers.Max(l => l.Y + l.Image.PixelHeight);

                double width = maxX - minX;
                double height = maxY - minY;

                CropArea = new CropArea
                {
                    X = minX + width * 0.1,
                    Y = minY + height * 0.1,
                    Width = width * 0.8,
                    Height = height * 0.8
                };
            }

            CropModeChanged?.Invoke();
        }

        private bool CanApplyCrop()
        {
            return IsCropMode && CropArea != null;
        }

        private void ApplyCrop()
        {
            if (!CanApplyCrop()) return;

            // Застосовуємо custom ratio якщо вибрано
            if (IsCustomRatio && CustomRatioWidth > 0 && CustomRatioHeight > 0)
            {
                SelectedCropRatio = new CropRatio
                {
                    Name = "Custom",
                    Width = CustomRatioWidth,
                    Height = CustomRatioHeight
                };
            }

            if (SelectedLayer != null)
            {
                // Crop одного шару
                _currentCropCommand = new CropImageCommand(SelectedLayer, CropArea);
            }
            else
            {
                // Crop всього колажу
                _currentCropCommand = new CropCollageCommand(Layers.ToList(), CropArea);
            }

            if (_currentCropCommand.CanExecute())
            {
                _currentCropCommand.Execute();
            }

            IsCropMode = false;
            CropModeChanged?.Invoke();
            RotationChanged?.Invoke();
        }

        private void CancelCrop()
        {
            IsCropMode = false;
            CropArea = null;
            CropModeChanged?.Invoke();
        }

        public void UpdateCropArea(double x, double y, double width, double height)
        {
            if (CropArea == null) return;

            CropArea.X = x;
            CropArea.Y = y;
            CropArea.Width = width;
            CropArea.Height = height;
        }

        public CropRatio GetEffectiveRatio()
        {
            if (IsCustomRatio)
            {
                return new CropRatio
                {
                    Width = CustomRatioWidth,
                    Height = CustomRatioHeight,
                    IsFreeform = false
                };
            }

            return SelectedCropRatio;
        }
    }
}
