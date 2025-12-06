using ImageEditor.Interfaces;
using ImageEditor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageEditor.Commands
{
    public class RotateCommand : IUndoableCommand
    {
        private readonly ObservableCollection<LayerModel> _layers;
        private readonly LayerModel _selectedLayer;
        private readonly int _angleDelta;
        private readonly Dictionary<LayerModel, LayerState> _originalStates = new();

        public string Description => $"Обертання на {_angleDelta}°";

        private class LayerState
        {
            public double X { get; set; }
            public double Y { get; set; }
            public int Angle { get; set; }
        }

        public RotateCommand(ObservableCollection<LayerModel> layers, LayerModel selectedLayer, int angleDelta)
        {
            _layers = layers;
            _selectedLayer = selectedLayer;
            _angleDelta = angleDelta;
        }

        public bool CanExecute()
        {
            return (_selectedLayer != null) || (_layers != null && _layers.Count > 0);
        }

        public void Execute()
        {
            if (!CanExecute()) return;

            if (_selectedLayer != null)
            {
                _originalStates[_selectedLayer] = new LayerState
                {
                    X = _selectedLayer.X,
                    Y = _selectedLayer.Y,
                    Angle = _selectedLayer.Angle
                };

                _selectedLayer.Angle = NormalizeAngle(_selectedLayer.Angle + _angleDelta);
            }
            else
            {
                RotateCollage();
            }
        }

        public void Undo()
        {
            foreach (var kvp in _originalStates)
            {
                kvp.Key.X = kvp.Value.X;
                kvp.Key.Y = kvp.Value.Y;
                kvp.Key.Angle = kvp.Value.Angle;
            }
            _originalStates.Clear();
        }

        private void RotateCollage()
        {
            if (_layers.Count == 0) return;

            foreach (var layer in _layers)
            {
                _originalStates[layer] = new LayerState
                {
                    X = layer.X,
                    Y = layer.Y,
                    Angle = layer.Angle
                };
            }

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var layer in _layers)
            {
                if (layer.Image == null) continue;

                double centerX = layer.X + layer.Image.PixelWidth / 2.0;
                double centerY = layer.Y + layer.Image.PixelHeight / 2.0;

                if (centerX < minX) minX = centerX;
                if (centerX > maxX) maxX = centerX;
                if (centerY < minY) minY = centerY;
                if (centerY > maxY) maxY = centerY;
            }

            double collageCenterX = (minX + maxX) / 2.0;
            double collageCenterY = (minY + maxY) / 2.0;

            double angleRad = _angleDelta * Math.PI / 180.0;
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);

            foreach (var layer in _layers)
            {
                if (layer.Image == null) continue;

                double layerCenterX = layer.X + layer.Image.PixelWidth / 2.0;
                double layerCenterY = layer.Y + layer.Image.PixelHeight / 2.0;

                double dx = layerCenterX - collageCenterX;
                double dy = layerCenterY - collageCenterY;

                double newDx = dx * cos - dy * sin;
                double newDy = dx * sin + dy * cos;

                double newCenterX = collageCenterX + newDx;
                double newCenterY = collageCenterY + newDy;

                layer.X = newCenterX - layer.Image.PixelWidth / 2.0;
                layer.Y = newCenterY - layer.Image.PixelHeight / 2.0;

                layer.Angle = NormalizeAngle(layer.Angle + _angleDelta);
            }
        }

        private int NormalizeAngle(double angle)
        {
            int result = ((int)Math.Round(angle) % 360);
            if (result < 0) result += 360;
            return result;
        }
    }
}
