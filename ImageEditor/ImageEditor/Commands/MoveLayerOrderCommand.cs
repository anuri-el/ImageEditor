using ImageEditor.Models;
using System.Collections.ObjectModel;

namespace ImageEditor.Commands
{
    public class MoveLayerOrderCommand : ILayerOrderCommand
    {
        private readonly ObservableCollection<LayerModel> _layers;
        private readonly LayerModel _layer;
        private readonly int _oldIndex;
        private readonly int _newIndex;

        public MoveLayerOrderCommand(ObservableCollection<LayerModel> layers, LayerModel layer, int newIndex)
        {
            _layers = layers;
            _layer = layer;
            _oldIndex = layers.IndexOf(layer);
            _newIndex = newIndex;
        }

        public bool CanExecute()
        {
            return _layer != null &&
                   _layers != null &&
                   _newIndex >= 0 &&
                   _newIndex < _layers.Count &&
                   _oldIndex != _newIndex;
        }

        public void Execute()
        {
            if (!CanExecute()) return;

            _layers.RemoveAt(_oldIndex);
            _layers.Insert(_newIndex, _layer);
        }

        public void Undo()
        {
            if (_layer == null || _layers == null) return;

            int currentIndex = _layers.IndexOf(_layer);
            if (currentIndex >= 0)
            {
                _layers.RemoveAt(currentIndex);
                _layers.Insert(_oldIndex, _layer);
            }
        }
    }
}
