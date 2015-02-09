using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DevAndSports.Helpers
{
    #region ViewModelBase

    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private Dispatcher _dispatcherUI;

        protected Dispatcher DispatcherUI
        {
            get { return _dispatcherUI ?? (_dispatcherUI = Application.Current.Dispatcher); }
        }

        protected ViewModelBase()
        {
            if (Application.Current != null)
                _dispatcherUI = Application.Current.Dispatcher;
        }

        protected void ExecuteAndEnsureAccessUI(Action action)
        {
            if (DispatcherUI.CheckAccess()) action();
            else DispatcherUI.BeginInvoke(action);
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetByRef<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return;
            field = value;
            OnPropertyChanged(propertyName);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            ExecuteAndEnsureAccessUI(() =>
            {
                var handler = PropertyChanged;
                if (handler == null) return;
                handler(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        #endregion
    }

    #endregion

    #region RelayCommand

    public interface ICommandCanExecuteRaisable : ICommand
    {
        void RaiseCanExecuteChanged();
    }

    public class RelayCommand<T> : ICommandCanExecuteRaisable
    {
        private Dispatcher _dispatcherUI;
        private Action<T> _executeCallback;
        private Func<T, bool> _canExecuteCallback;
        public event EventHandler CanExecuteChanged;

        public RelayCommand(Dispatcher dispatcherUI, Action<T> executeCallback)
            : this(dispatcherUI, executeCallback, _ => true)
        {
        }

        public RelayCommand(Dispatcher dispatcherUI, Action<T> executeCallback, Func<T, bool> canExecuteCallback)
        {
            _dispatcherUI = dispatcherUI;
            _executeCallback = executeCallback;
            _canExecuteCallback = canExecuteCallback;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecuteCallback((T)parameter);
        }

        public void Execute(object parameter)
        {
            _executeCallback((T)parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            Action action = () =>
            {
                var handler = CanExecuteChanged;
                if (handler == null) return;
                handler(this, EventArgs.Empty);
            };
            if (_dispatcherUI.CheckAccess()) action();
            else _dispatcherUI.BeginInvoke(action);
        }
    }

    public class RelayCommand : RelayCommand<object>
    {
        public RelayCommand(Dispatcher dispatcherUI, Action executeCallback)
            : base(dispatcherUI, _ => executeCallback(), _ => true)
        {
        }

        public RelayCommand(Dispatcher dispatcherUI, Action executeCallback, Func<bool> canExecuteCallback)
            : base(dispatcherUI, _ => executeCallback(), _ => canExecuteCallback())
        {
        }
    }

    #endregion

    #region ViewModels

    public static class DialogHelper
    {
        public static Task<string> OpenFileDialogAsync(string ext, string filter)
        {
            return OpenFileDialogAsync(ext, filter, CancellationToken.None);
        }

        public static async Task<string> OpenFileDialogAsync(string ext, string filter, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<string>();
            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                var dialog = new OpenFileDialog();
                dialog.DefaultExt = ext;
                dialog.AddExtension = true;
                dialog.Filter = filter;
                dialog.CheckFileExists = true;
                dialog.Multiselect = false;
                CancelEventHandler handler = null;
                handler = (_, args) =>
                {
                    dialog.FileOk -= handler;
                    tcs.TrySetResult(!string.IsNullOrWhiteSpace(dialog.FileName)
                        ? dialog.FileName
                        : null);
                };
                dialog.FileOk += handler;
                dialog.ShowDialog();
                return await tcs.Task;
            }
        }
    }

    public abstract class ViewModelBaseWithCommands : ViewModelBase
    {
        public class NamedCommandDecorator
        {
            private readonly string _name;
            private readonly ICommandCanExecuteRaisable _command;
            public string Name => _name;
            public ICommand Command => _command;

            public NamedCommandDecorator(string name, ICommandCanExecuteRaisable command)
            {
                _name = name;
                _command = command;
            }

            public void RaiseCanExecuteChanged()
            {
                _command.RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<NamedCommandDecorator> _commands;

        public IEnumerable<NamedCommandDecorator> Commands => _commands;

        protected ViewModelBaseWithCommands()
        {
            _commands = new ObservableCollection<NamedCommandDecorator>();
        }

        protected void AddCommands(IEnumerable<NamedCommandDecorator> commands)
        {
            ExecuteAndEnsureAccessUI(() =>
            {
                foreach (var command in commands)
                    _commands.Add(command);
            });
        }

        protected void AddCommand(NamedCommandDecorator command)
        {
            ExecuteAndEnsureAccessUI(() =>
            {
                _commands.Add(command);
            });
        }

        protected void RemoveCommand(NamedCommandDecorator command)
        {
            ExecuteAndEnsureAccessUI(() =>
            {
                _commands.Remove(command);
            });
        }

        protected virtual void RaiseCanExecuteChangedOnAllCommands()
        {
            ExecuteAndEnsureAccessUI(() =>
            {
                foreach (var command in _commands)
                    command.RaiseCanExecuteChanged();
            });
        }
    }

    #endregion
}
