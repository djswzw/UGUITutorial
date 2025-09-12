// 作用：一个自定义的Graphic组件，能够使用ImageFontData资产来将字符串渲染为图片序列。
// 特性：支持字符间距、自动换行、行间距，并在Editor模式下实时预览。尺寸计算与Image组件完全一致。
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[AddComponentMenu("UI/Extensions/Image Font Text (Optimized)")]
[ExecuteAlways]
public class ImageFontText : MaskableGraphic
{
    #region Public Fields

    [SerializeField]
    private ImageFontData m_FontData;
    public ImageFontData fontData { get { return m_FontData; } set { if (m_FontData != value) { m_FontData = value; SetAllDirty(); } } }

    [SerializeField, TextArea(3, 10)]
    private string m_Text = "";
    public string text { get { return m_Text; } set { if (m_Text != value) { m_Text = value; SetAllDirty(); } } }

    [SerializeField]
    private float m_CharSpacing = 0f;
    public float charSpacing { get { return m_CharSpacing; } set { if (m_CharSpacing != value) { m_CharSpacing = value; SetAllDirty(); } } }

    [SerializeField]
    private float m_LineSpacing = 0f;
    public float lineSpacing { get { return m_LineSpacing; } set { if (m_LineSpacing != value) { m_LineSpacing = value; SetAllDirty(); } } }

    #endregion

    public float pixelsPerUnit
    {
        get
        {
            float spritePixelsPerUnit = 100; // 默认值
            // 优先使用FontData中第一个有效Sprite的pixelsPerUnit
            if (fontData != null)
            {
                foreach (var mapping in fontData.mappings)
                {
                    if (mapping.sprite != null)
                    {
                        spritePixelsPerUnit = mapping.sprite.pixelsPerUnit;
                        break;
                    }
                }
            }

            // 如果有Canvas，则使用Canvas的referencePixelsPerUnit；否则使用默认值。
            float referencePixelsPerUnit = 100;
            if (canvas)
                referencePixelsPerUnit = canvas.referencePixelsPerUnit;

            return spritePixelsPerUnit / referencePixelsPerUnit;
        }
    }

    // 图片字体的材质，最终来源于其Sprite所在的图集的材质
    public override Texture mainTexture
    {
        get
        {
            if (m_FontData == null || m_FontData.mappings.Count == 0) return s_WhiteTexture;

            foreach (var mapping in m_FontData.mappings)
            {
                if (mapping.sprite != null && mapping.sprite.texture != null)
                    return mapping.sprite.texture;
            }
            return s_WhiteTexture;
        }
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        ImageFontData.OnDataChanged += OnFontDataChanged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ImageFontData.OnDataChanged -= OnFontDataChanged;
    }

    private void OnFontDataChanged(ImageFontData changedData)
    {
        if (m_FontData != null && m_FontData == changedData)
        {
            SetAllDirty();
        }
    }
    /// <summary>
    /// 核心的网格生成方法。
    /// </summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (fontData == null || string.IsNullOrEmpty(text)) return;

        Rect selfRect = rectTransform.rect;
        Vector2 selfPivot = rectTransform.pivot;

        var lines = new List<LineInfo>();
        var currentLine = new LineInfo();

        // --- 第一遍：预计算排版 ---
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\n')
            {
                lines.Add(currentLine);
                currentLine = new LineInfo();
                continue;
            }

            Sprite sprite = fontData.GetSprite(c);
            if (sprite == null) continue;

            // 关键修正：所有尺寸计算，必须在预计算阶段就除以pixelsPerUnit，转换为Canvas单位
            float charWidth = sprite.rect.width / pixelsPerUnit;
            float charHeight = sprite.rect.height / pixelsPerUnit;

            if (currentLine.width + charWidth > selfRect.width && currentLine.chars.Count > 0)
            {
                lines.Add(currentLine);
                currentLine = new LineInfo();
            }

            currentLine.chars.Add(new CharInfo { character = c, sprite = sprite, width = charWidth, height = charHeight });
            currentLine.width += charWidth + (currentLine.chars.Count > 1 ? m_CharSpacing : 0);
            currentLine.height = Mathf.Max(currentLine.height, charHeight);
        }
        lines.Add(currentLine);

        // --- 第二遍：顶点生成 ---
        float totalHeight = 0;
        foreach (var line in lines)
        {
            totalHeight += (line.height > 0 ? line.height : fontData.defaultLineHeight / pixelsPerUnit);
        }
        totalHeight += Mathf.Max(0, lines.Count - 1) * m_LineSpacing;

        // 计算起始Y坐标 (假设垂直方向为居中对齐, 0.5f)
        float startY = (1 - selfPivot.y) * selfRect.height - (selfRect.height - totalHeight) * 0.5f;

        float currentY = startY;
        foreach (var line in lines)
        {
            float lineHeight = (line.height > 0 ? line.height : fontData.defaultLineHeight / pixelsPerUnit);
            // 计算当前行的起始X坐标 (假设水平方向为居中对齐, 0.5f)
            float startX = -selfPivot.x * selfRect.width + (selfRect.width - line.width) * 0.5f;
            float currentX = startX;

            foreach (var charInfo in line.chars)
            {
                Sprite sprite = charInfo.sprite;
                Vector4 outerUV = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);

                // Y坐标的计算需要考虑行高和字符自身高度的差异，以实现底部对齐 (0.0f)
                float yOffset = (lineHeight - charInfo.height) * 0.0f;
                Vector3 bottomLeft = new Vector3(currentX, currentY - lineHeight + yOffset);
                Vector3 topLeft = new Vector3(currentX, currentY - lineHeight + yOffset + charInfo.height);
                Vector3 topRight = new Vector3(currentX + charInfo.width, currentY - lineHeight + yOffset + charInfo.height);
                Vector3 bottomRight = new Vector3(currentX + charInfo.width, currentY - lineHeight + yOffset);

                AddQuad(vh, bottomLeft, topLeft, topRight, bottomRight, color, outerUV);

                currentX += charInfo.width + m_CharSpacing;
            }

            currentY -= (lineHeight + m_LineSpacing);
        }
    }

    private void AddQuad(VertexHelper vh, Vector3 bottomLeft, Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, Color32 color, Vector4 uv)
    {
        int vertIndex = vh.currentVertCount;
        vh.AddVert(bottomLeft, color, new Vector2(uv.x, uv.y));
        vh.AddVert(topLeft, color, new Vector2(uv.x, uv.w));
        vh.AddVert(topRight, color, new Vector2(uv.z, uv.w));
        vh.AddVert(bottomRight, color, new Vector2(uv.z, uv.y));
        vh.AddTriangle(vertIndex, vertIndex + 1, vertIndex + 2);
        vh.AddTriangle(vertIndex + 2, vertIndex + 3, vertIndex);
    }

    #region Helper Classes
    private class CharInfo { public char character; public Sprite sprite; public float width; public float height; }
    private class LineInfo { public float width = 0f; public float height = 0f; public List<CharInfo> chars = new List<CharInfo>(); }
    #endregion
}