using ImageEditor.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImageEditor
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel => DataContext as MainViewModel;

        private bool _isDraggingCrop = false;
        private bool _isResizingCrop = false;
        private Point _dragStartPoint;
        private double _cropStartX, _cropStartY, _cropStartWidth, _cropStartHeight;
        private string _resizeHandle;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel.LayerSelected += UpdateSelectionRect;
            ViewModel.RotationChanged += UpdateSelectionRect;
            ViewModel.CropModeChanged += UpdateCropGrid;
        }

        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.UpdateCanvasSize(e.NewSize.Width, e.NewSize.Height);
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.IsCropMode) return;

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

                double localX = dx * cos - dy * sin + layer.Image.PixelWidth / 2.0;
                double localY = dx * sin + dy * cos + layer.Image.PixelHeight / 2.0;

                if (localX >= 0 && localX <= layer.Image.PixelWidth &&
                    localY >= 0 && localY <= layer.Image.PixelHeight)
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

            SelectionRect.Visibility = Visibility.Visible;

            double w = layer.Image.PixelWidth;
            double h = layer.Image.PixelHeight;
            double angle = layer.Angle;
            double rad = Math.PI * angle / 180.0;

            double w2 = Math.Abs(w * Math.Cos(rad)) + Math.Abs(h * Math.Sin(rad));
            double h2 = Math.Abs(w * Math.Sin(rad)) + Math.Abs(h * Math.Cos(rad));

            SelectionRect.Width = w2;
            SelectionRect.Height = h2;

            Canvas.SetLeft(SelectionRect, layer.X - (w2 - w) / 2);
            Canvas.SetTop(SelectionRect, layer.Y - (h2 - h) / 2);

            SelRotate.Angle = angle;
            SelRotate.CenterX = SelectionRect.Width / 2;
            SelRotate.CenterY = SelectionRect.Height / 2;
        }

        private void UpdateCropGrid()
        {
            if (ViewModel.IsCropMode && ViewModel.CropArea != null)
            {
                CropGridCanvas.Visibility = Visibility.Visible;
                CropGridCanvas.Width = EditorCanvas.ActualWidth;
                CropGridCanvas.Height = EditorCanvas.ActualHeight;

                var cropArea = ViewModel.CropArea;

                // Позиціонуємо crop border
                Canvas.SetLeft(CropBorder, cropArea.X);
                Canvas.SetTop(CropBorder, cropArea.Y);
                CropBorder.Width = cropArea.Width;
                CropBorder.Height = cropArea.Height;

                // Оновлюємо затемнення навколо crop області
                // Верхнє
                Canvas.SetLeft(DimTop, 0);
                Canvas.SetTop(DimTop, 0);
                DimTop.Width = EditorCanvas.ActualWidth;
                DimTop.Height = cropArea.Y;

                // Ліве
                Canvas.SetLeft(DimLeft, 0);
                Canvas.SetTop(DimLeft, cropArea.Y);
                DimLeft.Width = cropArea.X;
                DimLeft.Height = cropArea.Height;

                // Праве
                Canvas.SetLeft(DimRight, cropArea.X + cropArea.Width);
                Canvas.SetTop(DimRight, cropArea.Y);
                DimRight.Width = EditorCanvas.ActualWidth - cropArea.X - cropArea.Width;
                DimRight.Height = cropArea.Height;

                // Нижнє
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
                _dragStartPoint = e.GetPosition(CropGridCanvas);
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
                double deltaX = currentPoint.X - _dragStartPoint.X;
                double deltaY = currentPoint.Y - _dragStartPoint.Y;

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

        private void ResizeHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isResizingCrop = true;
                _dragStartPoint = e.GetPosition(CropGridCanvas);
                _cropStartX = ViewModel.CropArea.X;
                _cropStartY = ViewModel.CropArea.Y;
                _cropStartWidth = ViewModel.CropArea.Width;
                _cropStartHeight = ViewModel.CropArea.Height;

                var element = sender as FrameworkElement;
                _resizeHandle = element.Name;

                element.CaptureMouse();
                e.Handled = true;
            }
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizingCrop)
            {
                Point currentPoint = e.GetPosition(CropGridCanvas);
                ResizeCropArea(currentPoint);
            }
        }

        private void ResizeHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isResizingCrop = false;
            (sender as FrameworkElement)?.ReleaseMouseCapture();
        }

        private void ResizeCropArea(Point currentPoint)
        {
            double deltaX = currentPoint.X - _dragStartPoint.X;
            double deltaY = currentPoint.Y - _dragStartPoint.Y;

            var ratio = ViewModel.GetEffectiveRatio();
            double newX = _cropStartX;
            double newY = _cropStartY;
            double newWidth = _cropStartWidth;
            double newHeight = _cropStartHeight;

            switch (_resizeHandle)
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

            // Обмеження мінімального розміру
            newWidth = Math.Max(50, newWidth);
            newHeight = Math.Max(50, newHeight);

            // Обмеження межами canvas
            newX = Math.Max(0, Math.Min(newX, EditorCanvas.ActualWidth - newWidth));
            newY = Math.Max(0, Math.Min(newY, EditorCanvas.ActualHeight - newHeight));

            if (newX + newWidth > EditorCanvas.ActualWidth)
                newWidth = EditorCanvas.ActualWidth - newX;
            if (newY + newHeight > EditorCanvas.ActualHeight)
                newHeight = EditorCanvas.ActualHeight - newY;

            ViewModel.UpdateCropArea(newX, newY, newWidth, newHeight);
            UpdateCropGrid();
        }
    }
}