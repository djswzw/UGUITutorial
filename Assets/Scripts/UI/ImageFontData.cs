using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "NewImageFontData", menuName = "UI/Image Font Data", order = 1)]
public class ImageFontData : ScriptableObject
{
    public static event Action<ImageFontData> OnDataChanged;

    // 用于在运行时快速查找Sprite的字典，通过懒加载进行初始化。
    private Dictionary<char, Sprite> _spriteDict;

    // 在Inspector面板中进行配置的映射列表。
    public List<CharSpriteMapping> mappings = new List<CharSpriteMapping>();

    // 默认的行高，如果没有指定字符，则使用此高度。
    [Tooltip("当一行没有任何字符，或者需要一个基础行高时使用。通常设置为最高字符的高度。")]
    public float defaultLineHeight = 100f;

    /// <summary>
    /// 定义了单个字符与Sprite的映射关系。
    /// </summary>
    [System.Serializable]
    public class CharSpriteMapping
    {
        [Tooltip("要映射的字符")]
        public char character;
        [Tooltip("该字符对应的Sprite图片")]
        public Sprite sprite;
    }

    private void OnValidate()
    {
        // 关键：当数据变化时，清空旧的字典缓存，并广播全局事件
        _spriteDict = null;
        OnDataChanged?.Invoke(this);
    }

    /// <summary>
    /// 根据字符，获取其对应的Sprite。
    /// </summary>
    /// <param name="character">要查找的字符</param>
    /// <returns>如果找到则返回Sprite，否则返回null。</returns>
    public Sprite GetSprite(char character)
    {
        // Dictionary提供了O(1)的查找效率，远高于遍历List的O(n)。
        if (_spriteDict == null)
        {
            _spriteDict = new Dictionary<char, Sprite>();
            foreach (var mapping in mappings)
            {
                // 防止重复的字符定义导致错误
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