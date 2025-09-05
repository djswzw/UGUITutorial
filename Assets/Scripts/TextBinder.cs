using UnityEngine;
using UnityEngine.UI;
using System.ComponentModel;
using System.Reflection;

[RequireComponent(typeof(Text))]
public class TextBinder : MonoBehaviour
{
    [Tooltip("����ViewModel���Ǹ�View�ű���������ʵ��IViewModelProvider�ӿ�")]
    public MonoBehaviour ViewProviderSource;
    [Tooltip("Ҫ�󶨵�ViewModel�е�������")]
    public string PropertyName;

    private Text _textComponent;
    private PropertyInfo _propertyInfo;
    private INotifyPropertyChanged _viewModel;

    void Start()
    {
        _textComponent = GetComponent<Text>();

        // ����ʵ����IViewModelProvider�ӿڵ����
        IViewModelProvider provider = ViewProviderSource as IViewModelProvider;
        if (provider == null)
        {
            // ���ֱ����ק�Ĳ��ԣ����Գ���������GameObject�ϲ���
            provider = ViewProviderSource.GetComponent<IViewModelProvider>();
        }

        if (provider == null)
        {
            Debug.LogError("ViewProviderSource does not implement IViewModelProvider", gameObject);
            return;
        }

        // ��Provider��ȡViewModel
        _viewModel = provider.ViewModel;
        if (_viewModel == null)
        {
            Debug.LogError("ViewModel is null in the provider", gameObject);
            return;
        }

        // ʹ�÷����ȡ����
        _propertyInfo = _viewModel.GetType().GetProperty(PropertyName);
        if (_propertyInfo == null)
        {
            Debug.LogError($"Property '{PropertyName}' not found on ViewModel of type {_viewModel.GetType().Name}", gameObject);
            return;
        }

        // ����ViewModel�����Ա���¼�
        _viewModel.PropertyChanged += OnPropertyChanged;
        UpdateText(); // �״θ���
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