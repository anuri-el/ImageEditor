using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ImageEditor.Effects
{
    public interface IImageEffect
    {
        string Name { get; }
        string Description { get; }
        BitmapImage Apply(BitmapImage source);
    }
}
