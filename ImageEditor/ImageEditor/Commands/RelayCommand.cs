using System.Windows.Input;

namespace ImageEditor.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        private readonly Action<object> _executeParam;
        private readonly Func<object, bool> _canExecuteParam;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _executeParam = execute;
            _canExecuteParam = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_execute != null)
                return _canExecute == null || _canExecute();

            return _canExecuteParam == null || _canExecuteParam(parameter);
        }

        public void Execute(object parameter)
        {
            if (_execute != null)
                _execute();
            else
                _executeParam(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
