using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageEditor.Commands
{
    public class CommandHistory
    {
        private readonly Stack<IUndoableCommand> _undoStack = new();
        private readonly Stack<IUndoableCommand> _redoStack = new();
        private const int MaxHistorySize = 50;

        public event Action HistoryChanged;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public string UndoDescription => CanUndo ? _undoStack.Peek().Description : "";
        public string RedoDescription => CanRedo ? _redoStack.Peek().Description : "";

        public void ExecuteCommand(IUndoableCommand command)
        {
            if (command == null || !command.CanExecute())
                return;

            command.Execute();

            _undoStack.Push(command);

            _redoStack.Clear();

            if (_undoStack.Count > MaxHistorySize)
            {
                var list = _undoStack.ToList();
                list.RemoveAt(list.Count - 1);
                _undoStack.Clear();
                foreach (var cmd in list.AsEnumerable().Reverse())
                {
                    _undoStack.Push(cmd);
                }
            }

            HistoryChanged?.Invoke();
        }

        public void Undo()
        {
            if (!CanUndo) return;

            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);

            HistoryChanged?.Invoke();
        }

        public void Redo()
        {
            if (!CanRedo) return;

            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);

            HistoryChanged?.Invoke();
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            HistoryChanged?.Invoke();
        }

        public ObservableCollection<string> GetHistoryList()
        {
            var list = new ObservableCollection<string>();
            foreach (var cmd in _undoStack.Reverse())
            {
                list.Add(cmd.Description);
            }
            return list;
        }
    }
}
