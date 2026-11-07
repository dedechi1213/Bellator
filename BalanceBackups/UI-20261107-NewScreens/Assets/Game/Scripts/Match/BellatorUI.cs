using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bellator.Match
{
    /// <summary>Shared 1080p UI primitives. Artwork is never modified or regenerated.</summary>
    public static class BellatorUI
    {
        public static readonly Color Gold = new Color(.85f, .67f, .35f);
        public static readonly Color Ink = new Color(.13f, .105f, .085f, .98f);
        public static GameObject Rect(Transform parent, string name, float x, float y, float r, float t)
        {
            var g = new GameObject(name, typeof(RectTransform)); g.transform.SetParent(parent, false);
            var rect = g.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(x,y); rect.anchorMax = new Vector2(r,t);
            rect.offsetMin = rect.offsetMax = Vector2.zero; return g;
        }
        public static bool Artwork(GameObject root, string key)
        {
            var sprite = Resources.Load<Sprite>("BellatorUI/" + key);
            if (sprite == null) return false;
            var background = Rect(root.transform, "OriginalArtwork", 0,0,1,1);
            background.transform.SetAsFirstSibling();
            var image = background.AddComponent<Image>(); image.sprite = sprite; image.raycastTarget = false;
            return true;
        }
        public static TextMeshProUGUI Label(Transform parent, string name, string value, float x, float y, float r, float t, float size = 28)
        {
            var label = Rect(parent,name,x,y,r,t).AddComponent<TextMeshProUGUI>();
            label.text = value; label.fontSize = size; label.color = new Color(.94f,.88f,.74f);
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false; label.richText = false;
            label.enableAutoSizing = true; label.fontSizeMin = Mathf.Min(18,size); label.fontSizeMax = size;
            return label;
        }
        public static Button Button(Transform parent, string name, string title, float x, float y, float r, float t, Action action, bool transparent = false)
        {
            var root = Rect(parent,name,x,y,r,t); var image = root.AddComponent<Image>();
            image.color = transparent ? Color.clear : new Color(.4f,.16f,.13f);
            var button = root.AddComponent<Button>(); button.targetGraphic = image;
            if (!transparent) Label(root.transform,"Label",title,.025f,.03f,.975f,.97f);
            // The original button stays visible. A subtle overlay supplies pointer/keyboard feedback.
            var feedback = Rect(root.transform,"Focus",0,0,1,1).AddComponent<Image>();
            feedback.raycastTarget = false; feedback.color = Color.clear; button.targetGraphic = feedback;
            var colors = button.colors; colors.normalColor = Color.clear; colors.highlightedColor = new Color(1,.8f,.4f,.13f);
            colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(0,0,0,.25f);
            colors.disabledColor = new Color(0,0,0,.35f); button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action()); return button;
        }
    }
}
