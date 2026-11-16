// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does the combat feel agile and weighted (Hollow Knight-paced, one notch heavier)?
// Date: 2026-10-24
//
// Tiny helper so every prototype object can be a solid-color square with zero
// imported assets. Not meant to survive into production art.

using System.Collections.Generic;
using UnityEngine;

public static class PrototypeVisuals
{
    static readonly Dictionary<Color, Sprite> _cache = new Dictionary<Color, Sprite>();

    public static Sprite SolidSquare(Color color)
    {
        if (_cache.TryGetValue(color, out var cached) && cached != null)
            return cached;

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        _cache[color] = sprite;
        return sprite;
    }
}
