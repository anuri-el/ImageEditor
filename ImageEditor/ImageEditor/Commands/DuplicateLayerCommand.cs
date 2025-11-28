using ImageEditor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageEditor.Commands
{
    public class DuplicateLayerCommand : IUndoableCommand
    {
        private readonly ObservableCollection<LayerModel> _layers;
        private readonly LayerModel _sourceLayer;
        private LayerModel _duplicatedLayer;
        private int _insertIndex;

        public string Description => "Дублювання шару";

        public DuplicateLayerCommand(ObservableCollection<LayerModel> layers, LayerModel sourceLayer)
        {
            _layers = layers;
            _sourceLayer = sourceLayer;
        }

        public bool CanExecute()
        {
            return _sourceLayer != null && _layers != null;
        }

        public void Execute()
        {
            if (!CanExecute()) return;

            // Створюємо дублікат шару
            _duplicatedLayer = new LayerModel
            {
                Image = _sourceLayer.Image, // BitmapImage є frozen, тому можна використовувати той самий
                X = _sourceLayer.X + 20, // Зміщуємо трохи вправо
                Y = _sourceLayer.Y + 20, // і вниз
                Angle = _sourceLayer.Angle,
                IsVisible = _sourceLayer.IsVisible,
                OriginalWidth = _sourceLayer.OriginalWidth,
                OriginalHeight = _sourceLayer.OriginalHeight
            };

            // Вставляємо відразу після оригіналу
            _insertIndex = _layers.IndexOf(_sourceLayer) + 1;
            _layers.Insert(_insertIndex, _duplicatedLayer);
        }

        public void Undo()
        {
            if (_duplicatedLayer != null && _layers.Contains(_duplicatedLayer))
            {
                _layers.Remove(_duplicatedLayer);
            }
        }
    }
}
