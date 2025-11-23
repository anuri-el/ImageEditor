using ImageEditor.Commands;
using ImageEditor.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
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

        public MainViewModel()
        {
            AddImageCommand = new RelayCommand(AddImage);
            SelectLayerCommand = new RelayCommand(o => SelectLayer(o));
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
    }
}
