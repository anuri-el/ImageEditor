namespace ImageEditor.Interfaces
{
    public interface IMoveCommand
    {
        void Execute();
        void Undo();
        bool CanExecute();
    }
}
