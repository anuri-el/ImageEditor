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

        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Оновлюємо розмір Canvas при зміні розміру ScrollViewer
            ViewModel.UpdateCanvasSize(e.NewSize.Width, e.NewSize.Height);
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point click = e.GetPosition(EditorCanvas);

            // Перевіряємо шари від верхнього до нижнього (в зворотному порядку)
            for (int i = ViewModel.Layers.Count - 1; i >= 0; i--)
            {
                var layer = ViewModel.Layers[i];
                if (layer.Image == null) continue;

                // Отримуємо центр зображення
                double centerX = layer.X + layer.Image.PixelWidth / 2.0;
                double centerY = layer.Y + layer.Image.PixelHeight / 2.0;

                // Переводимо точку кліку в локальні координати (враховуючи обертання)
                double dx = click.X - centerX;
                double dy = click.Y - centerY;

                double angleRad = -layer.Angle * Math.PI / 180.0;
                double cos = Math.Cos(angleRad);
                double sin = Math.Sin(angleRad);

                double localX = dx * cos - dy * sin + layer.Image.PixelWidth / 2.0;
                double localY = dx * sin + dy * cos + layer.Image.PixelHeight / 2.0;

                // Перевіряємо чи потрапили в межі зображення
                if (localX >= 0 && localX <= layer.Image.PixelWidth &&
                    localY >= 0 && localY <= layer.Image.PixelHeight)
                {
                    ViewModel.SelectLayerCommand.Execute(layer);
                    UpdateSelectionRect();
                    return;
                }
            }

            // Якщо не потрапили ні в один шар - знімаємо виділення
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
    }
}