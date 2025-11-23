using Microsoft.Win32;
using System.Windows.Media.Imaging;

namespace ImageEditor.Services
{
    public class ImageService
    {
        public BitmapImage? OpenImage()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff";

            if (dialog.ShowDialog() != true)
                return null;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // важливо: файл не блокується
                bitmap.UriSource = new Uri(dialog.FileName);
                bitmap.EndInit();
                bitmap.Freeze(); // для потокобезпеки

                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
