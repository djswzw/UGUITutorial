using System.ComponentModel;
using System.Runtime.CompilerServices;

// 任何想要提供ViewModel给Binder的View，都必须实现这个接口
public interface IViewModelProvider
{
    // 提供一个只读属性，返回实现了INotifyPropertyChanged的ViewModel实例
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