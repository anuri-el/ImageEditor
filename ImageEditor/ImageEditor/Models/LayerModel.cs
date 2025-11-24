using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace ImageEditor.Models
{
    public class LayerModel : INotifyPropertyChanged
    {
        public string FilePath { get; set; }
        public BitmapImage Image { get; set; }

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

        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get; set; } = false;

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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
