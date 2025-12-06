namespace ImageEditor.Interfaces
{
    public interface ICropCommand
    {
        void Execute();
        void Undo();
        bool CanExecute();
    }
}
