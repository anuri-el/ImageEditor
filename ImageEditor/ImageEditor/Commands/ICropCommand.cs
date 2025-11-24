namespace ImageEditor.Commands
{
    public interface ICropCommand
    {
        void Execute();
        void Undo();
        bool CanExecute();
    }
}
