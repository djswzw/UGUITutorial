using UnityEngine;
using UnityEngine.UI;
using System.ComponentModel;
using System.Reflection;

[RequireComponent(typeof(Text))]
public class TextBinder : MonoBehaviour
{
    [Tooltip("持有ViewModel的那个View脚本，它必须实现IViewModelProvider接口")]
    public MonoBehaviour ViewProviderSource;
    [Tooltip("要绑定的ViewModel中的属性名")]
    public string PropertyName;

    private Text _textComponent;
    private PropertyInfo _propertyInfo;
    private INotifyPropertyChanged _viewModel;

    void Start()
    {
        _textComponent = GetComponent<Text>();

        // 查找实现了IViewModelProvider接口的组件
        IViewModelProvider provider = ViewProviderSource as IViewModelProvider;
        if (provider == null)
        {
            // 如果直接拖拽的不对，可以尝试在它的GameObject上查找
            provider = ViewProviderSource.GetComponent<IViewModelProvider>();
        }

        if (provider == null)
        {
            Debug.LogError("ViewProviderSource does not implement IViewModelProvider", gameObject);
            return;
        }

        // 从Provider获取ViewModel
        _viewModel = provider.ViewModel;
        if (_viewModel == null)
        {
            Debug.LogError("ViewModel is null in the provider", gameObject);
            return;
        }

        // 使用反射获取属性
        _propertyInfo = _viewModel.GetType().GetProperty(PropertyName);
        if (_propertyInfo == null)
        {
            Debug.LogError($"Property '{PropertyName}' not found on ViewModel of type {_viewModel.GetType().Name}", gameObject);
            return;
        }

        // 订阅ViewModel的属性变更事件
        _viewModel.PropertyChanged += OnPropertyChanged;
        UpdateText(); // 首次更新
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == PropertyName)
        {
            UpdateText();
        }
    }

    private void UpdateText()
    {
        _textComponent.text = _propertyInfo.GetValue(_viewModel)?.ToString() ?? "";
    }

    void OnDestroy()
    {
        if (_viewModel != null) _viewModel.PropertyChanged -= OnPropertyChanged;
    }
}