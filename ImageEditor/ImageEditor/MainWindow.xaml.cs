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

            // Підписуємося на подію вибору шару
            ViewModel.LayerSelected += UpdateSelectionRect;
        }

        // -------------------------------
        // ВИБІР ШАРУ КЛІКОМ ПО CANVAS
        // -------------------------------

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point click = e.GetPosition(EditorCanvas);

            // Перебираємо шари з кінця (верхні перевіряємо першими)
            for (int i = ViewModel.Layers.Count - 1; i >= 0; i--)
            {
                var layer = ViewModel.Layers[i];
                var img = layer.Image;

                if (img == null) continue;

                double left = layer.X;
                double top = layer.Y;
                double right = left + img.PixelWidth;
                double bottom = top + img.PixelHeight;

                if (click.X >= left && click.X <= right &&
                    click.Y >= top && click.Y <= bottom)
                {
                    ViewModel.SelectedLayer = layer;
                    return;
                }
            }

            // Клік поза всіма — зняти виділення
            ViewModel.SelectedLayer = null;
        }

        // -------------------------------
        // ОНОВЛЕННЯ РАМКИ ВИДІЛЕННЯ
        // -------------------------------

        private void UpdateSelectionRect()
        {
            var layer = ViewModel.SelectedLayer;

            if (layer == null || layer.Image == null)
            {
                SelectionRect.Visibility = Visibility.Collapsed;
                return;
            }

            SelectionRect.Visibility = Visibility.Visible;

            // Встановлюємо позицію
            Canvas.SetLeft(SelectionRect, layer.X);
            Canvas.SetTop(SelectionRect, layer.Y);

            // Встановлюємо розміри
            SelectionRect.Width = layer.Image.PixelWidth;
            SelectionRect.Height = layer.Image.PixelHeight;
        }
    }
}