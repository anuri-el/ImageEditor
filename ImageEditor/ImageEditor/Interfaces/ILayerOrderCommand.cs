namespace ImageEditor.Interfaces
{
    public interface ILayerOrderCommand
    {
        void Execute();
        void Undo();
        bool CanExecute();
    }
}
