using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(AdvancedButton), true)]
[CanEditMultipleObjects] 
public class AdvancedButtonEditor : ButtonEditor
{
    // --- 序列化属性的引用 ---
    private SerializedProperty onSingleClickProp;
    private SerializedProperty onDoubleClickProp;
    private SerializedProperty onMultiClickProp;
    private SerializedProperty onLongPressStartProp;
    private SerializedProperty onLongPressingProp;
    private SerializedProperty onLongPressEndProp;
    private SerializedProperty multiClickThresholdProp;
    private SerializedProperty longPressThresholdProp;

    protected override void OnEnable()
    {
        // 1. 必须先调用基类的OnEnable方法
        base.OnEnable();

        // 2. 通过FindProperty找到我们自己新增的序列化字段
        onSingleClickProp = serializedObject.FindProperty("onSingleClick");
        onDoubleClickProp = serializedObject.FindProperty("onDoubleClick");
        onMultiClickProp = serializedObject.FindProperty("onMultiClick");
        onLongPressStartProp = serializedObject.FindProperty("onLongPressStart");
        onLongPressingProp = serializedObject.FindProperty("onLongPressing");
        onLongPressEndProp = serializedObject.FindProperty("onLongPressEnd");
        multiClickThresholdProp = serializedObject.FindProperty("multiClickThreshold");
        longPressThresholdProp = serializedObject.FindProperty("longPressThreshold");
    }

    public override void OnInspectorGUI()
    {
        // 3. 必须先调用基类的OnInspectorGUI方法
        // 这会绘制出Button原有的所有字段（Interactable, Transition, OnClick等）
        base.OnInspectorGUI();

        // 分隔线，让界面更清晰
        EditorGUILayout.Space();

        // 4. 更新序列化对象，这是每次绘制前的标准操作
        serializedObject.Update();

        // 5. 使用EditorGUILayout.PropertyField来绘制我们自己的字段
        // 这种方式能自动处理好多对象编辑、Undo/Redo等所有复杂情况
        EditorGUILayout.PropertyField(onSingleClickProp);
        EditorGUILayout.PropertyField(onDoubleClickProp);
        EditorGUILayout.PropertyField(onMultiClickProp);
        EditorGUILayout.PropertyField(onLongPressStartProp);
        EditorGUILayout.PropertyField(onLongPressingProp);
        EditorGUILayout.PropertyField(onLongPressEndProp);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(multiClickThresholdProp);
        EditorGUILayout.PropertyField(longPressThresholdProp);

        // 6. 应用所有修改，这是每次绘制结束后的标准操作
        serializedObject.ApplyModifiedProperties();
    }
}