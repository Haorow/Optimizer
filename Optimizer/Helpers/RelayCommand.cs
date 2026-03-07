using System;
using System.Windows.Input;

namespace Optimizer.ViewModels
{
    /// <summary>
    /// Implémentation simple de ICommand pour MVVM.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();

        public void Execute(object? parameter) => _execute();
    }

    /// <summary>
    /// Implémentation de ICommand avec paramètre typé et cast sécurisé.
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null) return true;

            // Cast sécurisé : évite InvalidCastException si le paramètre est null ou mauvais type
            if (parameter is T typed)
                return _canExecute(typed);

            // Accepter null pour les types nullables
            if (parameter == null && default(T) == null)
                return _canExecute(default!);

            return false;
        }

        public void Execute(object? parameter)
        {
            if (parameter is T typed)
            {
                _execute(typed);
                return;
            }

            // Accepter null pour les types nullables
            if (parameter == null && default(T) == null)
            {
                _execute(default!);
                return;
            }

            throw new InvalidOperationException(
                $"RelayCommand<{typeof(T).Name}> : paramètre invalide (reçu : {parameter?.GetType().Name ?? "null"})");
        }
    }
}