using System.ComponentModel;
using System.Runtime.CompilerServices;

// �κ���Ҫ�ṩViewModel��Binder��View��������ʵ������ӿ�
public interface IViewModelProvider
{
    // �ṩһ��ֻ�����ԣ�����ʵ����INotifyPropertyChanged��ViewModelʵ��
    INotifyPropertyChanged ViewModel { get; }
}

public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}