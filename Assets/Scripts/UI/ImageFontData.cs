using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "NewImageFontData", menuName = "UI/Image Font Data", order = 1)]
public class ImageFontData : ScriptableObject
{
    public static event Action<ImageFontData> OnDataChanged;

    // ����������ʱ���ٲ���Sprite���ֵ䣬ͨ�������ؽ��г�ʼ����
    private Dictionary<char, Sprite> _spriteDict;

    // ��Inspector����н������õ�ӳ���б�
    public List<CharSpriteMapping> mappings = new List<CharSpriteMapping>();

    // Ĭ�ϵ��иߣ����û��ָ���ַ�����ʹ�ô˸߶ȡ�
    [Tooltip("��һ��û���κ��ַ���������Ҫһ�������и�ʱʹ�á�ͨ������Ϊ����ַ��ĸ߶ȡ�")]
    public float defaultLineHeight = 100f;

    /// <summary>
    /// �����˵����ַ���Sprite��ӳ���ϵ��
    /// </summary>
    [System.Serializable]
    public class CharSpriteMapping
    {
        [Tooltip("Ҫӳ����ַ�")]
        public char character;
        [Tooltip("���ַ���Ӧ��SpriteͼƬ")]
        public Sprite sprite;
    }

    private void OnValidate()
    {
        // �ؼ��������ݱ仯ʱ����վɵ��ֵ仺�棬���㲥ȫ���¼�
        _spriteDict = null;
        OnDataChanged?.Invoke(this);
    }

    /// <summary>
    /// �����ַ�����ȡ���Ӧ��Sprite��
    /// </summary>
    /// <param name="character">Ҫ���ҵ��ַ�</param>
    /// <returns>����ҵ��򷵻�Sprite�����򷵻�null��</returns>
    public Sprite GetSprite(char character)
    {
        // Dictionary�ṩ��O(1)�Ĳ���Ч�ʣ�Զ���ڱ���List��O(n)��
        if (_spriteDict == null)
        {
            _spriteDict = new Dictionary<char, Sprite>();
            foreach (var mapping in mappings)
            {
                // ��ֹ�ظ����ַ����嵼�´���
                if (!_spriteDict.ContainsKey(mapping.character))
                {
                    _spriteDict.Add(mapping.character, mapping.sprite);
                }
            }
        }

        _spriteDict.TryGetValue(character, out Sprite sprite);
        return sprite;
    }
}