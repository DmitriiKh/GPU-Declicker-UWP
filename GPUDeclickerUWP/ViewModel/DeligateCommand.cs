using System;
using System.Windows.Input;

namespace GPUDeclickerUWP.ViewModel
{
    internal class DeligateCommand : ICommand
    {
        private bool IsEnabled { get; } = true;

        private readonly SimpleEventHandler _handler;
        public event EventHandler CanExecuteChanged;

        public delegate void SimpleEventHandler();
        public DeligateCommand(SimpleEventHandler handler)
        {
            _handler = handler;
        }

        void ICommand.Execute(object org)
        {
            _handler();
        }

        bool ICommand.CanExecute(object org)
        {
            return IsEnabled;
        }

        private void OnCanExcecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
