using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(AdvancedButton), true)]
[CanEditMultipleObjects] 
public class AdvancedButtonEditor : ButtonEditor
{
    // --- ���л����Ե����� ---
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
        // 1. �����ȵ��û����OnEnable����
        base.OnEnable();

        // 2. ͨ��FindProperty�ҵ������Լ����������л��ֶ�
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
        // 3. �����ȵ��û����OnInspectorGUI����
        // �����Ƴ�Buttonԭ�е������ֶΣ�Interactable, Transition, OnClick�ȣ�
        base.OnInspectorGUI();

        // �ָ��ߣ��ý��������
        EditorGUILayout.Space();

        // 4. �������л���������ÿ�λ���ǰ�ı�׼����
        serializedObject.Update();

        // 5. ʹ��EditorGUILayout.PropertyField�����������Լ����ֶ�
        // ���ַ�ʽ���Զ�����ö����༭��Undo/Redo�����и������
        EditorGUILayout.PropertyField(onSingleClickProp);
        EditorGUILayout.PropertyField(onDoubleClickProp);
        EditorGUILayout.PropertyField(onMultiClickProp);
        EditorGUILayout.PropertyField(onLongPressStartProp);
        EditorGUILayout.PropertyField(onLongPressingProp);
        EditorGUILayout.PropertyField(onLongPressEndProp);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(multiClickThresholdProp);
        EditorGUILayout.PropertyField(longPressThresholdProp);

        // 6. Ӧ�������޸ģ�����ÿ�λ��ƽ�����ı�׼����
        serializedObject.ApplyModifiedProperties();
    }
}