
using UnityEngine;


public class AdvancedButtonTest : MonoBehaviour
{
    public AdvancedButton btn;
    private void Start()
    {
        btn.onMultiClick.AddListener(OnMultiClick);
    }
    public void OnSingleClick()
    {
        Debug.Log("Single Click");
    }

    public void OnDoubleClick()
    {
        Debug.Log("Double Click");
    }

    public void OnMultiClick(int count)
    {
        Debug.Log($"Multi Click: {count}");
    }

    public void OnInspectorMultiClick(int count)
    {
        Debug.Log($"Inspector Multi Click: {count}");
    }

    public void OnLongPress()
    {
        Debug.Log($"LongPress");
    }

}

