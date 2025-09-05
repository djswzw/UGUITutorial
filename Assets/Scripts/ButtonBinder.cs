using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.ComponentModel;

[RequireComponent(typeof(Button))]
public class ButtonBinder : MonoBehaviour
{
    [Tooltip("持有ViewModel的那个View脚本，它必须实现IViewModelProvider接口")]
    public MonoBehaviour ViewProviderSource;
    [Tooltip("要绑定的ViewModel中的ICommand属性名")]
    public string CommandName;

    private Button _button;
    private ICommand _command; // 使用我们自己定义的ICommand接口

    void Start()
    {
        _button = GetComponent<Button>();

        // (这部分与TextBinder的查找逻辑完全一致)
        IViewModelProvider provider = ViewProviderSource as IViewModelProvider;
        if (provider == null) provider = ViewProviderSource.GetComponent<IViewModelProvider>();

        if (provider == null)
        {
            Debug.LogError("ViewProviderSource does not implement IViewModelProvider", gameObject);
            return;
        }

        INotifyPropertyChanged viewModel = provider.ViewModel;
        if (viewModel == null)
        {
            Debug.LogError("ViewModel is null in the provider", gameObject);
            return;
        }

        // --- 以下是ButtonBinder的核心逻辑 ---

        // 1. 通过反射，从ViewModel中获取ICommand属性
        PropertyInfo propInfo = viewModel.GetType().GetProperty(CommandName);
        if (propInfo == null)
        {
            Debug.LogError($"Command '{CommandName}' not found on ViewModel", gameObject);
            return;
        }

        _command = propInfo.GetValue(viewModel) as ICommand;
        if (_command == null)
        {
            Debug.LogError($"Property '{CommandName}' is not of type ICommand", gameObject);
            return;
        }

        // 2. 将Button的onClick事件，绑定到Command的Execute方法上
        _button.onClick.AddListener(() =>
        {
            _command.Execute(null); // parameter可以根据需要传递
        });

        // 3. 订阅Command的CanExecuteChanged事件
        _command.CanExecuteChanged += OnCanExecuteChanged;

        // 4. 在开始时，根据Command的初始状态，设置按钮是否可交互
        UpdateInteractableState();
    }

    // 当Command的可执行状态发生变化时，此方法会被调用
    private void OnCanExecuteChanged(object sender, System.EventArgs e)
    {
        UpdateInteractableState();
    }

    // 更新按钮的可交互状态
    private void UpdateInteractableState()
    {
        if (_button != null && _command != null)
        {
            _button.interactable = _command.CanExecute(null);
        }
    }

    void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
        }
        if (_command != null)
        {
            _command.CanExecuteChanged -= OnCanExecuteChanged;
        }
    }
}