namespace ImageEditor.Commands
{
    public interface IResizeCommand
    {
        void Execute();
        void Undo();
        bool CanExecute();
    }
}
