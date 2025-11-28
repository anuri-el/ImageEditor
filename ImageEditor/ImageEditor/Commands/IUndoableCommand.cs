using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageEditor.Commands
{
    public interface IUndoableCommand
    {
        void Execute();
        void Undo();
        bool CanExecute();
        string Description { get; }
    }
}
