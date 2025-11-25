namespace ImageEditor.Commands
{
    public interface IMoveCommand
    {
        void Execute();
        void Undo();
        bool CanExecute();
    }
}
