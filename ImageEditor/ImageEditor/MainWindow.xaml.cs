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

        public MainWindow()
        {
            InitializeComponent();

            ViewModel.LayerSelected += UpdateSelectionRect;
            ViewModel.RotationChanged += UpdateSelectionRect;
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point click = e.GetPosition(EditorCanvas);

            foreach (var item in EditorCanvas.Children.OfType<FrameworkElement>())
            {
                if (item.DataContext is LayerModel layer)
                {
                    var transform = item.TransformToVisual(EditorCanvas);
                    Point topLeft = transform.Transform(new Point(0, 0));
                    Point bottomRight = transform.Transform(new Point(item.ActualWidth, item.ActualHeight));

                    Rect rect = new Rect(topLeft, bottomRight);

                    if (rect.Contains(click))
                    {
                        ViewModel.SelectedLayer = layer;
                        return;
                    }
                }
            }

            ViewModel.SelectedLayer = null;
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
    }
}