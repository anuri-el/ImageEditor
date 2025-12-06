using ImageEditor.Interfaces;
using ImageEditor.Models;
using System.Collections.ObjectModel;

namespace ImageEditor.Commands
{
    public enum LayerOrderAction
    {
        MoveUp,
        MoveDown,
        BringToFront,
        SendToBack
    }

    public class LayerOrderCommandFactory
    {
        public static ILayerOrderCommand CreateCommand(
            ObservableCollection<LayerModel> layers,
            LayerModel layer,
            LayerOrderAction action)
        {
            if (layer == null || layers == null) return null;

            int currentIndex = layers.IndexOf(layer);
            if (currentIndex < 0) return null;

            int newIndex = currentIndex;

            switch (action)
            {
                case LayerOrderAction.MoveUp:
                    newIndex = currentIndex + 1;
                    break;

                case LayerOrderAction.MoveDown:
                    newIndex = currentIndex - 1;
                    break;

                case LayerOrderAction.BringToFront:
                    newIndex = layers.Count - 1;
                    break;

                case LayerOrderAction.SendToBack:
                    newIndex = 0;
                    break;
            }

            return new MoveLayerOrderCommand(layers, layer, newIndex);
        }
    }
}
