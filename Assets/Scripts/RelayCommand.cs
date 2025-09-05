using System;
public interface ICommand
{
    // 当命令的可执行状态可能发生变化时，触发此事件
    event EventHandler CanExecuteChanged;

    // 判断命令当前是否可以执行
    bool CanExecute(object parameter);

    // 执行命令
    void Execute(object parameter);
}
public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Predicate<object> _canExecute; // 使用Predicate<object>更通用

    public event EventHandler CanExecuteChanged;

    public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    // 适配无参数的构造函数
    public RelayCommand(Action execute, Func<bool> canExecute = null)
        : this(
            p => execute(),
            p => canExecute == null || canExecute()
            )
    {
    }

    public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

    public void Execute(object parameter) => _execute(parameter);

    // 当外部逻辑（如ViewModel中的属性）变化，可能影响CanExecute的结果时，
    // ViewModel需要手动调用此方法来通知UI更新状态。
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}