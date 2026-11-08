using UnityEngine;

namespace Snug
{
    /// <summary>
    /// Generates every sprite at runtime so the prototype needs no imported art.
    /// Circle-type sprites are 1 world unit across at scale 1; set localScale to the wanted diameter.
    /// </summary>
    public static class SpriteFactory
    {
        static Sprite circle, soft, ring, dashedRing, bar, pixel;

        public static Sprite Circle => circle ? circle : circle = Radial("circle", 256, (d) => Edge(d, 1f, 0.012f));
        public static Sprite Soft => soft ? soft : soft = Radial("soft", 128, (d) => Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d));
        public static Sprite Ring => ring ? ring : ring = Radial("ring", 256, (d) => Edge(d, 1f, 0.012f) * (1f - Edge(d, 0.9f, 0.012f)));

        public static Sprite DashedRing => dashedRing ? dashedRing : dashedRing = MakeDashedRing();

        /// <summary>Rounded bar for 9-slicing (corner radius 11 logical px at 0.01 world/px).</summary>
        public static Sprite Bar => bar ? bar : bar = MakeBar();

        public static Sprite Pixel
        {
            get
            {
                if (pixel) return pixel;
                var t = NewTex(4, 4);
                var px = new Color32[16];
                for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                t.SetPixels32(px);
                t.Apply();
                return pixel = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            }
        }

        public static Sprite VerticalGradient(Color top, Color bottom)
        {
            const int h = 128;
            var t = NewTex(2, h);
            for (int y = 0; y < h; y++)
            {
                Color c = Color.Lerp(bottom, top, y / (h - 1f));
                t.SetPixel(0, y, c);
                t.SetPixel(1, y, c);
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, 2, h), new Vector2(0.5f, 0.5f), h); // 1 unit tall
        }

        // ------------------------------------------------------------------ helpers
        static Texture2D NewTex(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
        }

        static float Edge(float d, float radius, float feather) => Mathf.Clamp01((radius - d) / feather);

        static Sprite Radial(string name, int size, System.Func<float, float> alphaAt)
        {
            var t = NewTex(size, size);
            var px = new Color32[size * size];
            float half = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                float a = alphaAt(Mathf.Sqrt(dx * dx + dy * dy));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(px);
            t.Apply();
            t.name = name;
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Sprite MakeDashedRing()
        {
            const int size = 256;
            const int dashes = 28;
            var t = NewTex(size, size);
            var px = new Color32[size * size];
            float half = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float band = Edge(d, 1f, 0.015f) * (1f - Edge(d, 0.965f, 0.015f));
                float ang = (Mathf.Atan2(dy, dx) / (Mathf.PI * 2f) + 0.5f) * dashes;
                float dash = (ang - Mathf.Floor(ang)) < 0.55f ? 1f : 0f;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(band * dash * 255));
            }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Sprite MakeBar()
        {
            const int size = 64;
            const float radius = 30f;
            var t = NewTex(size, size);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float a = Mathf.Clamp01(radius - d + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(px);
            t.Apply();
            // 30 texture px of corner = 11 logical px = 0.11 world units -> ppu 272
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 272f, 0,
                SpriteMeshType.FullRect, new Vector4(31, 31, 31, 31));
        }

        static readonly System.Collections.Generic.Dictionary<string, Color> hexCache = new System.Collections.Generic.Dictionary<string, Color>();

        /// <summary>Parses "#rrggbb" once and caches it, so per-frame calls don't allocate.</summary>
        public static Color Hex(string hex)
        {
            if (hexCache.TryGetValue(hex, out var cached)) return cached;
            var c = ColorUtility.TryParseHtmlString(hex, out var parsed) ? parsed : Color.magenta;
            hexCache[hex] = c;
            return c;
        }
    }
}
