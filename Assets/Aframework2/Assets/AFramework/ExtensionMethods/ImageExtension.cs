using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AFramework.ExtensionMethods
{
    public static class ImageExtension
    {
        public static void UpdatePivot(this Image img, float scale)
        {
            img.rectTransform.pivot = new Vector2(img.sprite.pivot.x / img.sprite.rect.width,
                img.sprite.pivot.y / img.sprite.rect.height);
            img.rectTransform.anchoredPosition = Vector2.zero;
            img.rectTransform.sizeDelta = img.sprite.rect.size * scale;
        }

        public static void SetSizeByWidth(this Image img, float width)
        {
            if (img.sprite == null)
                return;
            var sprite = img.sprite;
            float aspect = sprite.bounds.size.y / sprite.bounds.size.x;
            img.GetComponent<RectTransform>().sizeDelta = new Vector2(width, width * aspect);
        }

        public static void SetSizeByHeight(this Image img, float height)
        {
            if (img.sprite == null)
                return;
            var sprite = img.sprite;
            float aspect = sprite.bounds.size.y / sprite.bounds.size.x;
            img.GetComponent<RectTransform>().sizeDelta = new Vector2(height / aspect, height);
        }

        public static void SetAlpha(this Image img, float alpha)
        {
            Color color = img.color;
            color.a = alpha;
            img.color = color;
        }
    }
}