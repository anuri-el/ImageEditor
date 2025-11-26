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

        private bool _isDraggingLayer;
        public bool IsDraggingLayer
        {
            get => _isDraggingLayer;
            set
            {
                _isDraggingLayer = value;
                OnPropertyChanged();
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

        private bool _isResizeMode;
        public bool IsResizeMode
        {
            get => _isResizeMode;
            set
            {
                if (_isResizeMode == value) return;

                // Спочатку оновлюємо значення
                _isResizeMode = value;
                OnPropertyChanged();

                // Потім скасовуємо, якщо потрібно
                if (!value)
                {
                    CancelResize();
                }

                // Вимикаємо crop mode при включенні resize
                if (value && IsCropMode)
                {
                    IsCropMode = false;
                }
            }
        }

        private ResizeArea _resizeArea;
        public ResizeArea ResizeArea
        {
            get => _resizeArea;
            set
            {
                _resizeArea = value;
                OnPropertyChanged();
            }
        }

        public event Action LayerSelected;
        public event Action RotationChanged;
        public event Action CropModeChanged;
        public event Action ResizeModeChanged;

        public RelayCommand AddImageCommand { get; }
        public RelayCommand SelectLayerCommand { get; }
        public RelayCommand SaveCommand { get; }
        public ICommand RotateLeftCommand { get; }
        public ICommand RotateRightCommand { get; }
        public RelayCommand StartCropCommand { get; }
        public RelayCommand ApplyCropCommand { get; }
        public RelayCommand CancelCropCommand { get; }
        public RelayCommand StartResizeCommand { get; }
        public RelayCommand ApplyResizeCommand { get; }
        public RelayCommand CancelResizeCommand { get; }

        private ICropCommand _currentCropCommand;
        private IResizeCommand _currentResizeCommand;
        private IMoveCommand _currentMoveCommand;

        private Stack<IMoveCommand> _moveHistory = new Stack<IMoveCommand>();

        private ILayerOrderCommand _currentLayerOrderCommand;
        private Stack<ILayerOrderCommand> _layerOrderHistory = new Stack<ILayerOrderCommand>();

        public RelayCommand MoveLayerUpCommand { get; }
        public RelayCommand MoveLayerDownCommand { get; }
        public RelayCommand BringLayerToFrontCommand { get; }
        public RelayCommand SendLayerToBackCommand { get; }
        public RelayCommand DeleteLayerCommand { get; }

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

            StartResizeCommand = new RelayCommand(StartResize);
            ApplyResizeCommand = new RelayCommand(ApplyResize, CanApplyResize);
            CancelResizeCommand = new RelayCommand(CancelResize);

            MoveLayerUpCommand = new RelayCommand(MoveLayerUp, CanMoveLayerUp);
            MoveLayerDownCommand = new RelayCommand(MoveLayerDown, CanMoveLayerDown);
            BringLayerToFrontCommand = new RelayCommand(BringLayerToFront, CanBringLayerToFront);
            SendLayerToBackCommand = new RelayCommand(SendLayerToBack, CanSendLayerToBack);
            DeleteLayerCommand = new RelayCommand(DeleteLayer, CanDeleteLayer);
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

            if (Layers.Count == 0)
            {
                MessageBox.Show("Немає зображень для збереження.");
                return;
            }

            try
            {
                // Знаходимо bounds всіх зображень з урахуванням обертання
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;

                foreach (var layer in Layers)
                {
                    if (layer.Image == null) continue;

                    // Отримуємо bounds повернутого зображення
                    double w = layer.Image.PixelWidth;
                    double h = layer.Image.PixelHeight;
                    double angle = layer.Angle;
                    double rad = Math.PI * angle / 180.0;

                    double w2 = Math.Abs(w * Math.Cos(rad)) + Math.Abs(h * Math.Sin(rad));
                    double h2 = Math.Abs(w * Math.Sin(rad)) + Math.Abs(h * Math.Cos(rad));

                    double centerX = layer.X + w / 2.0;
                    double centerY = layer.Y + h / 2.0;

                    double left = centerX - w2 / 2.0;
                    double top = centerY - h2 / 2.0;
                    double right = centerX + w2 / 2.0;
                    double bottom = centerY + h2 / 2.0;

                    if (left < minX) minX = left;
                    if (top < minY) minY = top;
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

                // Створюємо RenderTargetBitmap
                RenderTargetBitmap rtb = new RenderTargetBitmap(
                    (int)Math.Ceiling(finalWidth),
                    (int)Math.Ceiling(finalHeight),
                    96, 96,
                    PixelFormats.Pbgra32);

                DrawingVisual dv = new DrawingVisual();
                using (DrawingContext ctx = dv.RenderOpen())
                {
                    // Малюємо тільки зображення, без рамок
                    foreach (var layer in Layers)
                    {
                        if (layer.Image == null) continue;

                        ctx.PushTransform(new TranslateTransform(
                            layer.X + layer.Image.PixelWidth / 2.0 - minX,
                            layer.Y + layer.Image.PixelHeight / 2.0 - minY));

                        ctx.PushTransform(new RotateTransform(layer.Angle));

                        ctx.DrawImage(layer.Image,
                            new Rect(
                                -layer.Image.PixelWidth / 2.0,
                                -layer.Image.PixelHeight / 2.0,
                                layer.Image.PixelWidth,
                                layer.Image.PixelHeight));

                        ctx.Pop(); // RotateTransform
                        ctx.Pop(); // TranslateTransform
                    }
                }

                rtb.Render(dv);

                // Вибираємо encoder
                BitmapEncoder encoder;
                string ext = System.IO.Path.GetExtension(dlg.FileName).ToLower();

                switch (ext)
                {
                    case ".jpg":
                    case ".jpeg":
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
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при збереженні: {ex.Message}");
            }
        }

        private void Rotate(int angle)
        {
            if (IsCropMode)
            {
                IsCropMode = false;
            }
            if (IsResizeMode)
            {
                IsResizeMode = false;
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
            if (IsResizeMode)
            {
                IsResizeMode = false;
            }

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

            try
            {
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
                    _currentCropCommand.Execute();

                    // Примусово оновлюємо UI
                    SelectedLayer.OnPropertyChanged(nameof(SelectedLayer.Image));
                    SelectedLayer.OnPropertyChanged(nameof(SelectedLayer.X));
                    SelectedLayer.OnPropertyChanged(nameof(SelectedLayer.Y));
                }
                else
                {
                    // Crop всього колажу
                    _currentCropCommand = new CropCollageCommand(Layers, CropArea);
                    _currentCropCommand.Execute();

                    // Примусово оновлюємо всі шари
                    foreach (var layer in Layers)
                    {
                        layer.OnPropertyChanged(nameof(layer.Image));
                        layer.OnPropertyChanged(nameof(layer.X));
                        layer.OnPropertyChanged(nameof(layer.Y));
                    }

                    // Оновлюємо canvas
                    UpdateCanvasAfterCrop();
                }

                IsCropMode = false;
                CropArea = null;

                CropModeChanged?.Invoke();
                RotationChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при застосуванні crop: {ex.Message}");
                IsCropMode = false;
                CropArea = null;
                CropModeChanged?.Invoke();
            }
        }

        private void UpdateCanvasAfterCrop()
        {
            if (Layers.Count == 0) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Знаходимо bounds обрізаного колажу
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;

                foreach (var layer in Layers)
                {
                    if (layer.Image == null) continue;

                    if (layer.X < minX) minX = layer.X;
                    if (layer.Y < minY) minY = layer.Y;

                    double right = layer.X + layer.Image.PixelWidth;
                    double bottom = layer.Y + layer.Image.PixelHeight;

                    if (right > maxX) maxX = right;
                    if (bottom > maxY) maxY = bottom;
                }

                if (minX == double.MaxValue) return;

                // Розміри колажу
                double collageWidth = maxX - minX;
                double collageHeight = maxY - minY;

                // ВАЖЛИВО: Canvas має бути більшим за колаж
                // Беремо максимум між поточним розміром і новим розміром + запас
                double newCanvasWidth = Math.Max(collageWidth + 400, 1000);
                double newCanvasHeight = Math.Max(collageHeight + 400, 700);

                // Зберігаємо старі розміри
                double oldCanvasWidth = CanvasWidth;
                double oldCanvasHeight = CanvasHeight;

                // Оновлюємо розмір canvas
                CanvasWidth = newCanvasWidth;
                CanvasHeight = newCanvasHeight;

                // Центруємо колаж на новому canvas
                double offsetX = (newCanvasWidth - collageWidth) / 2.0 - minX;
                double offsetY = (newCanvasHeight - collageHeight) / 2.0 - minY;

                foreach (var layer in Layers)
                {
                    layer.X += offsetX;
                    layer.Y += offsetY;
                }

            }, System.Windows.Threading.DispatcherPriority.Render);
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

        private void StartResize()
        {
            try
            {
                // Вимикаємо crop mode
                if (IsCropMode)
                {
                    IsCropMode = false;
                }

                // Ініціалізуємо resize область
                if (SelectedLayer != null && SelectedLayer.Image != null)
                {
                    // Resize для вибраного шару
                    ResizeArea = new ResizeArea
                    {
                        X = SelectedLayer.X,
                        Y = SelectedLayer.Y,
                        Width = SelectedLayer.Image.PixelWidth,
                        Height = SelectedLayer.Image.PixelHeight,
                        OriginalWidth = SelectedLayer.Image.PixelWidth,
                        OriginalHeight = SelectedLayer.Image.PixelHeight
                    };
                }
                else if (Layers.Count > 0)
                {
                    // Resize для всього колажу
                    double minX = double.MaxValue, minY = double.MaxValue;
                    double maxX = double.MinValue, maxY = double.MinValue;

                    foreach (var layer in Layers)
                    {
                        if (layer.Image == null) continue;

                        if (layer.X < minX) minX = layer.X;
                        if (layer.Y < minY) minY = layer.Y;

                        double right = layer.X + layer.Image.PixelWidth;
                        double bottom = layer.Y + layer.Image.PixelHeight;

                        if (right > maxX) maxX = right;
                        if (bottom > maxY) maxY = bottom;
                    }

                    if (minX == double.MaxValue || maxX == double.MinValue)
                    {
                        MessageBox.Show("Не вдалося визначити розміри колажу.");
                        return;
                    }

                    ResizeArea = new ResizeArea
                    {
                        X = minX,
                        Y = minY,
                        Width = maxX - minX,
                        Height = maxY - minY,
                        OriginalWidth = maxX - minX,
                        OriginalHeight = maxY - minY
                    };
                }
                else
                {
                    MessageBox.Show("Немає зображень для зміни розміру.");
                    return;
                }

                // ВАЖЛИВО: спочатку встановлюємо режим, потім викликаємо подію
                IsResizeMode = true;

                // Відкладаємо виклик події, щоб UI встиг оновитись
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ResizeModeChanged?.Invoke();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при запуску resize: {ex.Message}");
                IsResizeMode = false;
                ResizeArea = null;
            }
        }

        private bool CanApplyResize()
        {
            return IsResizeMode && ResizeArea != null && ResizeArea.Width > 0 && ResizeArea.Height > 0;
        }

        private void ApplyResize()
        {
            if (!CanApplyResize()) return;

            try
            {
                if (ResizeArea.Width <= 0 || ResizeArea.Height <= 0)
                {
                    MessageBox.Show("Некоректні розміри.");
                    return;
                }

                if (SelectedLayer != null)
                {
                    // Resize одного шару
                    _currentResizeCommand = new ResizeImageCommand(
                        SelectedLayer,
                        ResizeArea.Width,
                        ResizeArea.Height);

                    if (_currentResizeCommand.CanExecute())
                    {
                        _currentResizeCommand.Execute();

                        // Оновлюємо UI
                        SelectedLayer.OnPropertyChanged(nameof(SelectedLayer.Image));
                        SelectedLayer.OnPropertyChanged(nameof(SelectedLayer.X));
                        SelectedLayer.OnPropertyChanged(nameof(SelectedLayer.Y));
                    }
                }
                else if (Layers.Count > 0)
                {
                    // Resize всього колажу
                    if (ResizeArea.OriginalWidth <= 0 || ResizeArea.OriginalHeight <= 0)
                    {
                        MessageBox.Show("Некоректні оригінальні розміри.");
                        return;
                    }

                    double scaleX = ResizeArea.Width / ResizeArea.OriginalWidth;
                    double scaleY = ResizeArea.Height / ResizeArea.OriginalHeight;

                    if (double.IsNaN(scaleX) || double.IsNaN(scaleY) ||
                        double.IsInfinity(scaleX) || double.IsInfinity(scaleY))
                    {
                        MessageBox.Show("Некоректний масштаб.");
                        return;
                    }

                    _currentResizeCommand = new ResizeCollageCommand(Layers, scaleX, scaleY);

                    if (_currentResizeCommand.CanExecute())
                    {
                        _currentResizeCommand.Execute();

                        // Оновлюємо всі шари
                        foreach (var layer in Layers)
                        {
                            layer.OnPropertyChanged(nameof(layer.Image));
                            layer.OnPropertyChanged(nameof(layer.X));
                            layer.OnPropertyChanged(nameof(layer.Y));
                        }
                    }
                }

                IsResizeMode = false;
                ResizeArea = null;

                ResizeModeChanged?.Invoke();
                RotationChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при застосуванні resize: {ex.Message}\n{ex.StackTrace}");
                IsResizeMode = false;
                ResizeArea = null;
                ResizeModeChanged?.Invoke();
            }
        }

        private void CancelResize()
        {
            try
            {
                ResizeArea = null;

                // Не викликаємо IsResizeMode = false тут, щоб уникнути рекурсії
                if (_isResizeMode)
                {
                    _isResizeMode = false;
                    OnPropertyChanged(nameof(IsResizeMode));
                }

                ResizeModeChanged?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error canceling resize: {ex.Message}");
            }
        }

        public void UpdateResizeArea(double x, double y, double width, double height)
        {
            if (ResizeArea == null) return;

            ResizeArea.X = x;
            ResizeArea.Y = y;
            ResizeArea.Width = width;
            ResizeArea.Height = height;
        }

        public void ExecuteMoveCommand(IMoveCommand command)
        {
            if (command.CanExecute())
            {
                command.Execute();
                _moveHistory.Push(command);
            }
        }

        public void UndoLastMove()
        {
            if (_moveHistory.Count > 0)
            {
                var command = _moveHistory.Pop();
                command.Undo();
            }
        }

        private bool CanMoveLayerUp()
        {
            if (SelectedLayer == null) return false;
            int index = Layers.IndexOf(SelectedLayer);
            return index >= 0 && index < Layers.Count - 1;
        }

        private void MoveLayerUp()
        {
            var command = LayerOrderCommandFactory.CreateCommand(
                Layers,
                SelectedLayer,
                LayerOrderAction.MoveUp);

            ExecuteLayerOrderCommand(command);
        }

        private bool CanMoveLayerDown()
        {
            if (SelectedLayer == null) return false;
            int index = Layers.IndexOf(SelectedLayer);
            return index > 0;
        }

        private void MoveLayerDown()
        {
            var command = LayerOrderCommandFactory.CreateCommand(
                Layers,
                SelectedLayer,
                LayerOrderAction.MoveDown);

            ExecuteLayerOrderCommand(command);
        }

        private bool CanBringLayerToFront()
        {
            if (SelectedLayer == null) return false;
            int index = Layers.IndexOf(SelectedLayer);
            return index >= 0 && index < Layers.Count - 1;
        }

        private void BringLayerToFront()
        {
            var command = LayerOrderCommandFactory.CreateCommand(
                Layers,
                SelectedLayer,
                LayerOrderAction.BringToFront);

            ExecuteLayerOrderCommand(command);
        }

        private bool CanSendLayerToBack()
        {
            if (SelectedLayer == null) return false;
            int index = Layers.IndexOf(SelectedLayer);
            return index > 0;
        }

        private void SendLayerToBack()
        {
            var command = LayerOrderCommandFactory.CreateCommand(
                Layers,
                SelectedLayer,
                LayerOrderAction.SendToBack);

            ExecuteLayerOrderCommand(command);
        }

        private void ExecuteLayerOrderCommand(ILayerOrderCommand command)
        {
            if (command != null && command.CanExecute())
            {
                command.Execute();
                _layerOrderHistory.Push(command);

                // Оновлюємо UI
                RotationChanged?.Invoke();
            }
        }

        public void UndoLayerOrder()
        {
            if (_layerOrderHistory.Count > 0)
            {
                var command = _layerOrderHistory.Pop();
                command.Undo();
                RotationChanged?.Invoke();
            }
        }
        private bool CanDeleteLayer()
        {
            return SelectedLayer != null && Layers.Contains(SelectedLayer);
        }

        private void DeleteLayer()
        {
            if (SelectedLayer == null) return;

            var result = MessageBox.Show(
                "Видалити вибраний шар?",
                "Підтвердження",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Layers.Remove(SelectedLayer);
                SelectedLayer = null;
                RotationChanged?.Invoke();
            }
        }
    }
}
