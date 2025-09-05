using System;
public interface ICommand
{
    // ������Ŀ�ִ��״̬���ܷ����仯ʱ���������¼�
    event EventHandler CanExecuteChanged;

    // �ж����ǰ�Ƿ����ִ��
    bool CanExecute(object parameter);

    // ִ������
    void Execute(object parameter);
}
public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Predicate<object> _canExecute; // ʹ��Predicate<object>��ͨ��

    public event EventHandler CanExecuteChanged;

    public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    // �����޲����Ĺ��캯��
    public RelayCommand(Action execute, Func<bool> canExecute = null)
        : this(
            p => execute(),
            p => canExecute == null || canExecute()
            )
    {
    }

    public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

    public void Execute(object parameter) => _execute(parameter);

    // ���ⲿ�߼�����ViewModel�е����ԣ��仯������Ӱ��CanExecute�Ľ��ʱ��
    // ViewModel��Ҫ�ֶ����ô˷�����֪ͨUI����״̬��
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}