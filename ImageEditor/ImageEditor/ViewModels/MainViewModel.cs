using ImageEditor.Commands;
using ImageEditor.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
            set { _selectedLayer = value; OnPropertyChanged(); }
        }

        public RelayCommand AddImageCommand { get; }
        public RelayCommand SelectLayerCommand { get; }
        public RelayCommand SaveCommand { get; }

        public MainViewModel()
        {
            AddImageCommand = new RelayCommand(AddImage);
            SelectLayerCommand = new RelayCommand(o => SelectLayer(o));
            SaveCommand = new RelayCommand(SaveCollage);
        }

        private void AddImage()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Image Files|*.png;*.jpg;*.bmp";

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
                "PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg|Bitmap (*.bmp)|*.bmp";

            if (dlg.ShowDialog() != true)
                return;

            // Canvas береться через MainWindow (передамо через статичний доступ)
            var canvas = Application.Current.MainWindow.FindName("MainCanvas") as Canvas;
            if (canvas == null)
            {
                MessageBox.Show("Canvas не знайдено!");
                return;
            }

            // Рендер у зображення
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)canvas.Width,
                (int)canvas.Height,
                96, 96, PixelFormats.Pbgra32);

            rtb.Render(canvas);

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
                default:
                    encoder = new PngBitmapEncoder();
                    break;
            }

            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (FileStream fs = new FileStream(dlg.FileName, FileMode.Create))
            {
                encoder.Save(fs);
            }

            MessageBox.Show("Файл збережено успішно!");
        }
    }
}
