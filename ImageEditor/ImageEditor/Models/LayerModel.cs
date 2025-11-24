using ImageEditor.ViewModels;
using System.Windows.Media.Imaging;

namespace ImageEditor.Models
{
    public class LayerModel : BaseViewModel
    {
        public string FilePath { get; set; }
        public BitmapImage Image { get; set; }

        // Оригінальні розміри для інформації
        public double OriginalWidth { get; set; }
        public double OriginalHeight { get; set; }

        private double _x;
        public double X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged();
            }
        }

        private double _y;
        public double Y
        {
            get => _y;
            set
            {
                _y = value;
                OnPropertyChanged();
            }
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        private int _angle;
        public int Angle
        {
            get => _angle;
            set
            {
                _angle = value % 360;
                OnPropertyChanged();
            }
        }
    }
}
