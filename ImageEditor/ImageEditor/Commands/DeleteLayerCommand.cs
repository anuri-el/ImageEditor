using ImageEditor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageEditor.Commands
{
    public class DeleteLayerCommand : IUndoableCommand
    {
        private readonly ObservableCollection<LayerModel> _layers;
        private readonly LayerModel _layer;
        private int _originalIndex;

        public string Description => "Видалення шару";

        public DeleteLayerCommand(ObservableCollection<LayerModel> layers, LayerModel layer)
        {
            _layers = layers;
            _layer = layer;
        }

        public bool CanExecute()
        {
            return _layer != null && _layers != null && _layers.Contains(_layer);
        }

        public void Execute()
        {
            if (!CanExecute()) return;

            _originalIndex = _layers.IndexOf(_layer);
            _layers.Remove(_layer);
        }

        public void Undo()
        {
            if (_layer != null && _originalIndex >= 0 && _originalIndex <= _layers.Count)
            {
                _layers.Insert(_originalIndex, _layer);
            }
        }
    }
}
