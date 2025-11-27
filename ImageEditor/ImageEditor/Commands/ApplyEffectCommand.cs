using ImageEditor.Effects;
using ImageEditor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ImageEditor.Commands
{
    public class ApplyEffectCommand : IEffectCommand
    {
        private readonly ObservableCollection<LayerModel> _layers;
        private readonly LayerModel _selectedLayer;
        private readonly IImageEffect _effect;
        private readonly Dictionary<LayerModel, BitmapImage> _originalImages = new();

        public ApplyEffectCommand(
            ObservableCollection<LayerModel> layers,
            LayerModel selectedLayer,
            IImageEffect effect)
        {
            _layers = layers;
            _selectedLayer = selectedLayer;
            _effect = effect;
        }

        public bool CanExecute()
        {
            return (_selectedLayer != null && _selectedLayer.Image != null) ||
                   (_layers != null && _layers.Count > 0);
        }

        public void Execute()
        {
            if (!CanExecute()) return;

            try
            {
                if (_selectedLayer != null)
                {
                    // Застосовуємо до вибраного шару
                    _originalImages[_selectedLayer] = _selectedLayer.Image;
                    _selectedLayer.Image = _effect.Apply(_selectedLayer.Image);
                }
                else
                {
                    // Застосовуємо до всіх шарів
                    foreach (var layer in _layers)
                    {
                        if (layer.Image == null) continue;
                        _originalImages[layer] = layer.Image;
                        layer.Image = _effect.Apply(layer.Image);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Помилка застосування ефекту: {ex.Message}");
                Undo();
            }
        }

        public void Undo()
        {
            foreach (var kvp in _originalImages)
            {
                kvp.Key.Image = kvp.Value;
            }
            _originalImages.Clear();
        }
    }
}
