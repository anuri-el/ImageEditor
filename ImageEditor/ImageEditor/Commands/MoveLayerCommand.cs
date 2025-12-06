using ImageEditor.Interfaces;
using ImageEditor.Models;

namespace ImageEditor.Commands
{
    public class MoveLayerCommand : IMoveCommand, IUndoableCommand
    {
        private readonly LayerModel _layer;
        private readonly double _newX;
        private readonly double _newY;
        private double _originalX;
        private double _originalY;

        public string Description => "Переміщення шару";

        public MoveLayerCommand(LayerModel layer, double newX, double newY)
        {
            _layer = layer;
            _newX = newX;
            _newY = newY;
        }

        public bool CanExecute()
        {
            return _layer != null;
        }

        public void Execute()
        {
            if (!CanExecute()) return;

            _originalX = _layer.X;
            _originalY = _layer.Y;

            _layer.X = _newX;
            _layer.Y = _newY;
        }

        public void Undo()
        {
            if (_layer == null) return;

            _layer.X = _originalX;
            _layer.Y = _originalY;
        }
    }
}
