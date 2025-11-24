using System.Windows.Media.Imaging;

namespace ImageEditor.Models
{
    public class LayerModel
    {
        public string FilePath { get; set; }
        public BitmapImage Image { get; set; }

        // Позиція шару
        public double X { get; set; }
        public double Y { get; set; }

        public bool IsVisible { get; set; } = true;

        public bool IsSelected { get; set; } = false;

        private double _angle;
        public double Angle
        {
            get => _angle;
            set
            {
                _angle = value % 360;
            }
        }
    }
}
