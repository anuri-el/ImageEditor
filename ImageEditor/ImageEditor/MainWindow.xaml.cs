using ImageEditor.Commands;
using ImageEditor.Models;
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
        private Point _cropDragStartPoint;
        private double _cropStartX, _cropStartY, _cropStartWidth, _cropStartHeight;
        private string _cropResizeHandle;

        private bool _isResizing = false;
        private Point _resizeStartPoint;
        private double _resizeStartX, _resizeStartY, _resizeStartWidth, _resizeStartHeight;
        private string _resizeHandleType;

        private bool _isDraggingLayer = false;
        private Point _layerDragStartPoint;
        private double _layerStartX, _layerStartY;
        private LayerModel _draggedLayer;

        public MainWindow()
        {
            InitializeComponent();

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

        // Обробка кліку на полотно для виділення шару
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.IsCropMode || ViewModel.IsResizeMode) return;

            Point click = e.GetPosition(EditorCanvas);
            bool hitLayer = false;

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
                    hitLayer = true;

                    ViewModel.SelectLayerCommand.Execute(layer);
                    UpdateSelectionRect();

                    if (e.RightButton == MouseButtonState.Pressed)
                    {
                        ShowLayerContextMenu(e.GetPosition(this));
                        e.Handled = true;
                        return;
                    }

                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        _isDraggingLayer = true;
                        _draggedLayer = layer;
                        _layerDragStartPoint = click;
                        _layerStartX = layer.X;
                        _layerStartY = layer.Y;

                        EditorCanvas.CaptureMouse();
                        e.Handled = true;
                    }

                    return;
                }
            }

            if (!hitLayer)
            {
                ViewModel.SelectedLayer = null;
                SelectionRect.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowLayerContextMenu(Point position)
        {
            try
            {
                var contextMenu = this.FindResource("LayerContextMenu") as ContextMenu;
                if (contextMenu != null && ViewModel.SelectedLayer != null)
                {
                    contextMenu.PlacementTarget = this;
                    contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                    contextMenu.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing context menu: {ex.Message}");
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingLayer && _draggedLayer != null && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(EditorCanvas);

                double deltaX = currentPoint.X - _layerDragStartPoint.X;
                double deltaY = currentPoint.Y - _layerDragStartPoint.Y;

                _draggedLayer.X = _layerStartX + deltaX;
                _draggedLayer.Y = _layerStartY + deltaY;

                UpdateSelectionRect();

                EditorCanvas.Cursor = Cursors.SizeAll;

                e.Handled = true;
            }
            else if (!ViewModel.IsCropMode && !ViewModel.IsResizeMode)
            {
                Point mousePos = e.GetPosition(EditorCanvas);
                bool overImage = false;

                for (int i = ViewModel.Layers.Count - 1; i >= 0; i--)
                {
                    var layer = ViewModel.Layers[i];
                    if (layer.Image == null) continue;

                    double centerX = layer.X + layer.Image.PixelWidth / 2.0;
                    double centerY = layer.Y + layer.Image.PixelHeight / 2.0;

                    double dx = mousePos.X - centerX;
                    double dy = mousePos.Y - centerY;

                    double angleRad = -layer.Angle * Math.PI / 180.0;
                    double cos = Math.Cos(angleRad);
                    double sin = Math.Sin(angleRad);

                    double localXFromCenter = dx * cos - dy * sin;
                    double localYFromCenter = dx * sin + dy * cos;

                    if (Math.Abs(localXFromCenter) <= layer.Image.PixelWidth / 2.0 &&
                        Math.Abs(localYFromCenter) <= layer.Image.PixelHeight / 2.0)
                    {
                        overImage = true;
                        break;
                    }
                }

                EditorCanvas.Cursor = overImage ? Cursors.Hand : Cursors.Arrow;
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingLayer && _draggedLayer != null)
            {
                var moveCommand = new MoveLayerCommand(
                    _draggedLayer,
                    _draggedLayer.X,
                    _draggedLayer.Y);

                if (Math.Abs(_draggedLayer.X - _layerStartX) > 0.1 ||
                    Math.Abs(_draggedLayer.Y - _layerStartY) > 0.1)
                {
                    ViewModel.ExecuteMoveCommand(moveCommand);
                }

                _isDraggingLayer = false;
                _draggedLayer = null;

                EditorCanvas.ReleaseMouseCapture();
                e.Handled = true;
            }
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

                SelectionRect.Width = w;
                SelectionRect.Height = h;

                Canvas.SetLeft(SelectionRect, layer.X);
                Canvas.SetTop(SelectionRect, layer.Y);

                SelRotate.Angle = layer.Angle;
                SelRotate.CenterX = w / 2.0;
                SelRotate.CenterY = h / 2.0;
            }
            catch (Exception ex)
            {
                SelectionRect.Visibility = Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine($"Error updating selection rect: {ex.Message}");
            }
        }

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


        private void UpdateResizeGrid()
        {
            try
            {
                if (ResizeGridCanvas == null || ResizeBorder == null || EditorCanvas == null)
                {
                    System.Diagnostics.Debug.WriteLine("Resize UI elements not initialized yet");
                    return;
                }

                if (ViewModel?.IsResizeMode == true && ViewModel?.ResizeArea != null)
                {
                    var resizeArea = ViewModel.ResizeArea;

                    if (double.IsNaN(resizeArea.X) || double.IsNaN(resizeArea.Y) ||
                        double.IsNaN(resizeArea.Width) || double.IsNaN(resizeArea.Height) ||
                        resizeArea.Width <= 0 || resizeArea.Height <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Invalid resize area values");
                        ResizeGridCanvas.Visibility = Visibility.Collapsed;
                        return;
                    }

                    if (EditorCanvas.ActualWidth <= 0 || EditorCanvas.ActualHeight <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Canvas not ready yet");
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

                    Canvas.SetLeft(ResizeBorder, resizeArea.X);
                    Canvas.SetTop(ResizeBorder, resizeArea.Y);
                    ResizeBorder.Width = resizeArea.Width;
                    ResizeBorder.Height = resizeArea.Height;

                    if (ResizeTop != null && ResizeBottom != null && ResizeLeft != null && ResizeRight != null)
                    {
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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Z для undo переміщення
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ViewModel.Undo();
                UpdateSelectionRect();
                e.Handled = true;
            }
            // Ctrl+Y або Ctrl+Shift+Z для redo
            else if ((e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control) ||
                     (e.Key == Key.Z && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)))
            {
                ViewModel.Redo();
                UpdateSelectionRect();
                e.Handled = true;
            }
            // Ctrl+D для дублювання
            else if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (ViewModel.DuplicateLayerCommand.CanExecute(null))
                {
                    ViewModel.DuplicateLayerCommand.Execute(null);
                }
                e.Handled = true;
            }
            // Ctrl+] - вгору
            else if (e.Key == Key.OemCloseBrackets && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (ViewModel.MoveLayerUpCommand.CanExecute(null))
                {
                    ViewModel.MoveLayerUpCommand.Execute(null);
                }
                e.Handled = true;
            }
            // Ctrl+[ - вниз
            else if (e.Key == Key.OemOpenBrackets && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (ViewModel.MoveLayerDownCommand.CanExecute(null))
                {
                    ViewModel.MoveLayerDownCommand.Execute(null);
                }
                e.Handled = true;
            }
            // Ctrl+Shift+] - наверх
            else if (e.Key == Key.OemCloseBrackets &&
                     Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (ViewModel.BringLayerToFrontCommand.CanExecute(null))
                {
                    ViewModel.BringLayerToFrontCommand.Execute(null);
                }
                e.Handled = true;
            }
            // Ctrl+Shift+[ - вниз
            else if (e.Key == Key.OemOpenBrackets &&
                     Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (ViewModel.SendLayerToBackCommand.CanExecute(null))
                {
                    ViewModel.SendLayerToBackCommand.Execute(null);
                }
                e.Handled = true;
            }
            // Delete - видалити шар
            else if (e.Key == Key.Delete)
            {
                if (ViewModel.DeleteLayerCommand.CanExecute(null))
                {
                    ViewModel.DeleteLayerCommand.Execute(null);
                }
                e.Handled = true;
            }
        }
        private void ToggleLayersPanel(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LayersPanel != null)
                {
                    if (LayersPanel.Visibility == Visibility.Visible)
                    {
                        LayersPanel.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        LayersPanel.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}");
            }
        }

        private void MoveLayerUp_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.MoveLayerUpCommand.CanExecute(null))
            {
                ViewModel.MoveLayerUpCommand.Execute(null);
            }
        }

        private void MoveLayerDown_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.MoveLayerDownCommand.CanExecute(null))
            {
                ViewModel.MoveLayerDownCommand.Execute(null);
            }
        }

        private void BringLayerToFront_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.BringLayerToFrontCommand.CanExecute(null))
            {
                ViewModel.BringLayerToFrontCommand.Execute(null);
            }
        }

        private void SendLayerToBack_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SendLayerToBackCommand.CanExecute(null))
            {
                ViewModel.SendLayerToBackCommand.Execute(null);
            }
        }

        private void DeleteLayer_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.DeleteLayerCommand.CanExecute(null))
            {
                ViewModel.DeleteLayerCommand.Execute(null);
            }
        }

        private void DuplicateLayer_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.DuplicateLayerCommand.CanExecute(null))
            {
                ViewModel.DuplicateLayerCommand.Execute(null);
            }
        }
    }
}