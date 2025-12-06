namespace ImageEditor.Interfaces
{
    public interface IResizeCommand
    {
        void Execute();
        void Undo();
        bool CanExecute();
    }
}
