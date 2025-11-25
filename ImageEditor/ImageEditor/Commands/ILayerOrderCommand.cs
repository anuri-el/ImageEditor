namespace ImageEditor.Commands
{
    public interface ILayerOrderCommand
    {
        void Execute();
        void Undo();
        bool CanExecute();
    }
}
