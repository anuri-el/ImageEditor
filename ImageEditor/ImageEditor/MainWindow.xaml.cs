using ImageEditor.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImageEditor
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel => DataContext as MainViewModel;

        // Crop variables
        private bool _isDraggingCrop = false;
        private bool _isResizingCrop = false;
        private Point _cropDragStartPoint;
        private double _cropStartX, _cropStartY, _cropStartWidth, _cropStartHeight;
        private string _cropResizeHandle;

        // Resize variables
        private bool _isResizing = false;
        private Point _resizeStartPoint;
        private double _resizeStartX, _resizeStartY, _resizeStartWidth, _resizeStartHeight;
        private string _resizeHandleType;

        public MainWindow()
        {
            InitializeComponent();

            // Підписуємось на події після того як вікно завантажене
            this.Loaded += (s, e) =>
            {
                ViewModel.LayerSelected += UpdateSelectionRect;
                ViewModel.RotationChanged += UpdateSelectionRect;
                ViewModel.CropModeChanged += UpdateCropGrid;
                ViewModel.ResizeModeChanged += UpdateResizeGrid;
            };
        }

        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.UpdateCanvasSize(e.NewSize.Width, e.NewSize.Height);
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.IsCropMode || ViewModel.IsResizeMode) return;

            Point click = e.GetPosition(EditorCanvas);

            for (int i = ViewModel.Layers.Count - 1; i >= 0; i--)
            {
                var layer = ViewModel.Layers[i];
                if (layer.Image == null) continue;

                double centerX = layer.X + layer.Image.PixelWidth / 2.0;
                double centerY = layer.Y + layer.Image.PixelHeight / 2.0;

                double dx = click.X - centerX;
                double dy = click.Y - centerY;

                double angleRad = -layer.Angle * Math.PI / 180.0;
                double cos = Math.Cos(angleRad);
                double sin = Math.Sin(angleRad);

                double localXFromCenter = dx * cos - dy * sin;
                double localYFromCenter = dx * sin + dy * cos;

                bool isInside = Math.Abs(localXFromCenter) <= layer.Image.PixelWidth / 2.0 &&
                               Math.Abs(localYFromCenter) <= layer.Image.PixelHeight / 2.0;

                if (isInside)
                {
                    ViewModel.SelectLayerCommand.Execute(layer);
                    UpdateSelectionRect();
                    return;
                }
            }

            ViewModel.SelectedLayer = null;
            SelectionRect.Visibility = Visibility.Collapsed;
        }

        private void UpdateSelectionRect()
        {
            var layer = ViewModel.SelectedLayer;
            if (layer == null || layer.Image == null)
            {
                SelectionRect.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                SelectionRect.Visibility = Visibility.Visible;

                double w = layer.Image.PixelWidth;
                double h = layer.Image.PixelHeight;
                double angle = layer.Angle;
                double rad = Math.PI * angle / 180.0;

                double w2 = Math.Abs(w * Math.Cos(rad)) + Math.Abs(h * Math.Sin(rad));
                double h2 = Math.Abs(w * Math.Sin(rad)) + Math.Abs(h * Math.Cos(rad));

                SelectionRect.Width = w2;
                SelectionRect.Height = h2;

                double centerX = layer.X + w / 2.0;
                double centerY = layer.Y + h / 2.0;

                Canvas.SetLeft(SelectionRect, centerX - w2 / 2.0);
                Canvas.SetTop(SelectionRect, centerY - h2 / 2.0);

                SelRotate.Angle = angle;
                SelRotate.CenterX = w2 / 2.0;
                SelRotate.CenterY = h2 / 2.0;
            }
            catch (Exception ex)
            {
                SelectionRect.Visibility = Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine($"Error updating selection rect: {ex.Message}");
            }
        }

        // ==================== CROP METHODS ====================

        private void UpdateCropGrid()
        {
            if (ViewModel.IsCropMode && ViewModel.CropArea != null)
            {
                CropGridCanvas.Visibility = Visibility.Visible;
                CropGridCanvas.Width = EditorCanvas.ActualWidth;
                CropGridCanvas.Height = EditorCanvas.ActualHeight;

                var cropArea = ViewModel.CropArea;

                Canvas.SetLeft(CropBorder, cropArea.X);
                Canvas.SetTop(CropBorder, cropArea.Y);
                CropBorder.Width = cropArea.Width;
                CropBorder.Height = cropArea.Height;

                Canvas.SetLeft(DimTop, 0);
                Canvas.SetTop(DimTop, 0);
                DimTop.Width = EditorCanvas.ActualWidth;
                DimTop.Height = cropArea.Y;

                Canvas.SetLeft(DimLeft, 0);
                Canvas.SetTop(DimLeft, cropArea.Y);
                DimLeft.Width = cropArea.X;
                DimLeft.Height = cropArea.Height;

                Canvas.SetLeft(DimRight, cropArea.X + cropArea.Width);
                Canvas.SetTop(DimRight, cropArea.Y);
                DimRight.Width = EditorCanvas.ActualWidth - cropArea.X - cropArea.Width;
                DimRight.Height = cropArea.Height;

                Canvas.SetLeft(DimBottom, 0);
                Canvas.SetTop(DimBottom, cropArea.Y + cropArea.Height);
                DimBottom.Width = EditorCanvas.ActualWidth;
                DimBottom.Height = EditorCanvas.ActualHeight - cropArea.Y - cropArea.Height;
            }
            else
            {
                CropGridCanvas.Visibility = Visibility.Collapsed;
            }
        }

        private void CropBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDraggingCrop = true;
                _cropDragStartPoint = e.GetPosition(CropGridCanvas);
                _cropStartX = ViewModel.CropArea.X;
                _cropStartY = ViewModel.CropArea.Y;
                CropBorder.CaptureMouse();
                e.Handled = true;
            }
        }

        private void CropBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingCrop)
            {
                Point currentPoint = e.GetPosition(CropGridCanvas);
                double deltaX = currentPoint.X - _cropDragStartPoint.X;
                double deltaY = currentPoint.Y - _cropDragStartPoint.Y;

                double newX = Math.Max(0, Math.Min(_cropStartX + deltaX, EditorCanvas.ActualWidth - CropBorder.Width));
                double newY = Math.Max(0, Math.Min(_cropStartY + deltaY, EditorCanvas.ActualHeight - CropBorder.Height));

                ViewModel.UpdateCropArea(newX, newY, ViewModel.CropArea.Width, ViewModel.CropArea.Height);
                UpdateCropGrid();
            }
        }

        private void CropBorder_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingCrop = false;
            CropBorder.ReleaseMouseCapture();
        }

        private void CropResizeHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isResizingCrop = true;
                _cropDragStartPoint = e.GetPosition(CropGridCanvas);
                _cropStartX = ViewModel.CropArea.X;
                _cropStartY = ViewModel.CropArea.Y;
                _cropStartWidth = ViewModel.CropArea.Width;
                _cropStartHeight = ViewModel.CropArea.Height;

                var element = sender as FrameworkElement;
                _cropResizeHandle = element.Name;

                element.CaptureMouse();
                e.Handled = true;
            }
        }

        private void CropResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizingCrop)
            {
                Point currentPoint = e.GetPosition(CropGridCanvas);
                ResizeCropArea(currentPoint);
            }
        }

        private void CropResizeHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isResizingCrop = false;
            (sender as FrameworkElement)?.ReleaseMouseCapture();
        }

        private void ResizeCropArea(Point currentPoint)
        {
            double deltaX = currentPoint.X - _cropDragStartPoint.X;
            double deltaY = currentPoint.Y - _cropDragStartPoint.Y;

            var ratio = ViewModel.GetEffectiveRatio();
            double newX = _cropStartX;
            double newY = _cropStartY;
            double newWidth = _cropStartWidth;
            double newHeight = _cropStartHeight;

            switch (_cropResizeHandle)
            {
                case "TopLeft":
                    if (ratio.IsFreeform)
                    {
                        newX = _cropStartX + deltaX;
                        newY = _cropStartY + deltaY;
                        newWidth = _cropStartWidth - deltaX;
                        newHeight = _cropStartHeight - deltaY;
                    }
                    else
                    {
                        double avgDelta = (deltaX + deltaY) / 2;
                        newWidth = _cropStartWidth - avgDelta;
                        newHeight = newWidth * (ratio.Height / ratio.Width);
                        newX = _cropStartX + (_cropStartWidth - newWidth);
                        newY = _cropStartY + (_cropStartHeight - newHeight);
                    }
                    break;

                case "TopRight":
                    if (ratio.IsFreeform)
                    {
                        newY = _cropStartY + deltaY;
                        newWidth = _cropStartWidth + deltaX;
                        newHeight = _cropStartHeight - deltaY;
                    }
                    else
                    {
                        newWidth = _cropStartWidth + deltaX;
                        newHeight = newWidth * (ratio.Height / ratio.Width);
                        newY = _cropStartY - (newHeight - _cropStartHeight);
                    }
                    break;

                case "BottomLeft":
                    if (ratio.IsFreeform)
                    {
                        newX = _cropStartX + deltaX;
                        newWidth = _cropStartWidth - deltaX;
                        newHeight = _cropStartHeight + deltaY;
                    }
                    else
                    {
                        newWidth = _cropStartWidth - deltaX;
                        newHeight = newWidth * (ratio.Height / ratio.Width);
                        newX = _cropStartX + deltaX;
                    }
                    break;

                case "BottomRight":
                    if (ratio.IsFreeform)
                    {
                        newWidth = _cropStartWidth + deltaX;
                        newHeight = _cropStartHeight + deltaY;
                    }
                    else
                    {
                        newWidth = _cropStartWidth + deltaX;
                        newHeight = newWidth * (ratio.Height / ratio.Width);
                    }
                    break;
            }

            newWidth = Math.Max(50, newWidth);
            newHeight = Math.Max(50, newHeight);

            newX = Math.Max(0, Math.Min(newX, EditorCanvas.ActualWidth - newWidth));
            newY = Math.Max(0, Math.Min(newY, EditorCanvas.ActualHeight - newHeight));

            if (newX + newWidth > EditorCanvas.ActualWidth)
                newWidth = EditorCanvas.ActualWidth - newX;
            if (newY + newHeight > EditorCanvas.ActualHeight)
                newHeight = EditorCanvas.ActualHeight - newY;

            ViewModel.UpdateCropArea(newX, newY, newWidth, newHeight);
            UpdateCropGrid();
        }

        // ==================== RESIZE METHODS ====================

        private void UpdateResizeGrid()
        {
            try
            {
                // Перевірка чи існують всі UI елементи
                if (ResizeGridCanvas == null || ResizeBorder == null || EditorCanvas == null)
                {
                    System.Diagnostics.Debug.WriteLine("Resize UI elements not initialized yet");
                    return;
                }

                if (ViewModel?.IsResizeMode == true && ViewModel?.ResizeArea != null)
                {
                    var resizeArea = ViewModel.ResizeArea;

                    // Перевірка на валідність значень
                    if (double.IsNaN(resizeArea.X) || double.IsNaN(resizeArea.Y) ||
                        double.IsNaN(resizeArea.Width) || double.IsNaN(resizeArea.Height) ||
                        resizeArea.Width <= 0 || resizeArea.Height <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Invalid resize area values");
                        ResizeGridCanvas.Visibility = Visibility.Collapsed;
                        return;
                    }

                    // Перевірка чи Canvas має розміри
                    if (EditorCanvas.ActualWidth <= 0 || EditorCanvas.ActualHeight <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Canvas not ready yet");
                        // Спробуємо ще раз після layout update
                        EditorCanvas.UpdateLayout();

                        if (EditorCanvas.ActualWidth <= 0 || EditorCanvas.ActualHeight <= 0)
                        {
                            ResizeGridCanvas.Visibility = Visibility.Collapsed;
                            return;
                        }
                    }

                    ResizeGridCanvas.Visibility = Visibility.Visible;
                    ResizeGridCanvas.Width = EditorCanvas.ActualWidth;
                    ResizeGridCanvas.Height = EditorCanvas.ActualHeight;

                    // Позиціонуємо resize border
                    Canvas.SetLeft(ResizeBorder, resizeArea.X);
                    Canvas.SetTop(ResizeBorder, resizeArea.Y);
                    ResizeBorder.Width = resizeArea.Width;
                    ResizeBorder.Height = resizeArea.Height;

                    // Перевірка чи існують ручки перед оновленням їх позицій
                    if (ResizeTop != null && ResizeBottom != null && ResizeLeft != null && ResizeRight != null)
                    {
                        // Оновлюємо позиції бокових ручок (по центру)
                        Canvas.SetLeft(ResizeTop, resizeArea.Width / 2);
                        Canvas.SetLeft(ResizeBottom, resizeArea.Width / 2);
                        Canvas.SetTop(ResizeLeft, resizeArea.Height / 2);
                        Canvas.SetTop(ResizeRight, resizeArea.Height / 2);
                    }
                }
                else
                {
                    ResizeGridCanvas.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating resize grid: {ex.Message}\n{ex.StackTrace}");
                if (ResizeGridCanvas != null)
                {
                    ResizeGridCanvas.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ResizeImageHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isResizing = true;
                _resizeStartPoint = e.GetPosition(ResizeGridCanvas);
                _resizeStartX = ViewModel.ResizeArea.X;
                _resizeStartY = ViewModel.ResizeArea.Y;
                _resizeStartWidth = ViewModel.ResizeArea.Width;
                _resizeStartHeight = ViewModel.ResizeArea.Height;

                var element = sender as FrameworkElement;
                _resizeHandleType = element.Tag as string;

                element.CaptureMouse();
                e.Handled = true;
            }
        }

        private void ResizeImageHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizing)
            {
                Point currentPoint = e.GetPosition(ResizeGridCanvas);
                ResizeImageArea(currentPoint);
            }
        }

        private void ResizeImageHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isResizing = false;
            (sender as FrameworkElement)?.ReleaseMouseCapture();
        }

        private void ResizeImageArea(Point currentPoint)
        {
            double deltaX = currentPoint.X - _resizeStartPoint.X;
            double deltaY = currentPoint.Y - _resizeStartPoint.Y;

            double newX = _resizeStartX;
            double newY = _resizeStartY;
            double newWidth = _resizeStartWidth;
            double newHeight = _resizeStartHeight;

            const double minSize = 50;

            switch (_resizeHandleType)
            {
                case "TopLeft":
                    {
                        double aspectRatio = _resizeStartWidth / _resizeStartHeight;
                        double avgDelta = (deltaX + deltaY) / 2;

                        newWidth = Math.Max(minSize, _resizeStartWidth - avgDelta);
                        newHeight = newWidth / aspectRatio;

                        newX = _resizeStartX + (_resizeStartWidth - newWidth);
                        newY = _resizeStartY + (_resizeStartHeight - newHeight);
                    }
                    break;

                case "TopRight":
                    {
                        double aspectRatio = _resizeStartWidth / _resizeStartHeight;
                        double avgDelta = (deltaX - deltaY) / 2;

                        newWidth = Math.Max(minSize, _resizeStartWidth + avgDelta);
                        newHeight = newWidth / aspectRatio;

                        newY = _resizeStartY + (_resizeStartHeight - newHeight);
                    }
                    break;

                case "BottomLeft":
                    {
                        double aspectRatio = _resizeStartWidth / _resizeStartHeight;
                        double avgDelta = (-deltaX + deltaY) / 2;

                        newWidth = Math.Max(minSize, _resizeStartWidth - avgDelta);
                        newHeight = newWidth / aspectRatio;

                        newX = _resizeStartX + (_resizeStartWidth - newWidth);
                    }
                    break;

                case "BottomRight":
                    {
                        double aspectRatio = _resizeStartWidth / _resizeStartHeight;
                        double avgDelta = (deltaX + deltaY) / 2;

                        newWidth = Math.Max(minSize, _resizeStartWidth + avgDelta);
                        newHeight = newWidth / aspectRatio;
                    }
                    break;

                case "Top":
                    newY = Math.Min(_resizeStartY + deltaY, _resizeStartY + _resizeStartHeight - minSize);
                    newHeight = _resizeStartHeight - (newY - _resizeStartY);
                    break;

                case "Bottom":
                    newHeight = Math.Max(minSize, _resizeStartHeight + deltaY);
                    break;

                case "Left":
                    newX = Math.Min(_resizeStartX + deltaX, _resizeStartX + _resizeStartWidth - minSize);
                    newWidth = _resizeStartWidth - (newX - _resizeStartX);
                    break;

                case "Right":
                    newWidth = Math.Max(minSize, _resizeStartWidth + deltaX);
                    break;
            }

            newX = Math.Max(0, Math.Min(newX, EditorCanvas.ActualWidth - newWidth));
            newY = Math.Max(0, Math.Min(newY, EditorCanvas.ActualHeight - newHeight));

            if (newX + newWidth > EditorCanvas.ActualWidth)
                newWidth = EditorCanvas.ActualWidth - newX;
            if (newY + newHeight > EditorCanvas.ActualHeight)
                newHeight = EditorCanvas.ActualHeight - newY;

            ViewModel.UpdateResizeArea(newX, newY, newWidth, newHeight);
            UpdateResizeGrid();
        }
    }
}