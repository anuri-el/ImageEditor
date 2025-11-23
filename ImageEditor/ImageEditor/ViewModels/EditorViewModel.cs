using ImageEditor.Commands;
using ImageEditor.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace ImageEditor.ViewModels
{
    public class EditorViewModel : INotifyPropertyChanged
    {
        private readonly ImageService _imageService = new();

        private BitmapImage? _loadedImage;
        public BitmapImage? LoadedImage
        {
            get => _loadedImage;
            set
            {
                _loadedImage = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand OpenImageCommand { get; }

        public EditorViewModel()
        {
            OpenImageCommand = new RelayCommand(_ => OpenImage());
        }

        private void OpenImage()
        {
            var bmp = _imageService.OpenImage();
            if (bmp != null)
                LoadedImage = bmp;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
