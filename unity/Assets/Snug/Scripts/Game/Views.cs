using UnityEngine;
using Snug.Core;

namespace Snug
{
    /// <summary>The jelly blob: halo, body, inner light, shine, cheeks and two eyes. Pure presentation.</summary>
    public class BlobView : MonoBehaviour
    {
        SpriteRenderer halo, body, inner, dim, shine, eyeL, eyeR, cheekL, cheekR;
        Transform squash; // scaled for squash & stretch
        SkinDef skin;

        public static BlobView Create(Transform parent)
        {
            var go = new GameObject("Blob");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<BlobView>();
            v.Build();
            return v;
        }

        SpriteRenderer Part(Transform p, string n, Sprite s, int order, Vector2 pos, float scale)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        void Build()
        {
            halo = Part(transform, "Halo", SpriteFactory.Soft, 10, Vector2.zero, 4.2f);
            squash = new GameObject("Squash").transform;
            squash.SetParent(transform, false);
            body = Part(squash, "Body", SpriteFactory.Circle, 20, Vector2.zero, 2f);
            inner = Part(squash, "Inner", SpriteFactory.Soft, 21, new Vector2(-0.12f, 0.14f), 2.1f);
            dim = Part(squash, "Dim", SpriteFactory.Circle, 22, Vector2.zero, 2f);
            shine = Part(squash, "Shine", SpriteFactory.Circle, 23, new Vector2(-0.42f, 0.48f), 0.3f);
            shine.transform.localRotation = Quaternion.Euler(0, 0, 35f);
            shine.transform.localScale = new Vector3(0.32f, 0.18f, 1f);
            cheekL = Part(squash, "CheekL", SpriteFactory.Circle, 24, new Vector2(-0.46f, -0.1f), 0.2f);
            cheekR = Part(squash, "CheekR", SpriteFactory.Circle, 24, new Vector2(0.46f, -0.1f), 0.2f);
            eyeL = Part(squash, "EyeL", SpriteFactory.Circle, 25, new Vector2(-0.32f, 0.08f), 0.18f);
            eyeR = Part(squash, "EyeR", SpriteFactory.Circle, 25, new Vector2(0.32f, 0.08f), 0.18f);
            var eye = SpriteFactory.Hex("#2a1a22");
            eyeL.color = eyeR.color = eye;
            cheekL.color = cheekR.color = new Color(1f, 0.47f, 0.47f, 0.25f);
            shine.color = new Color(1, 1, 1, 0.5f);
        }

        public void SetSkin(SkinDef s)
        {
            skin = s;
            body.color = SpriteFactory.Hex(s.Dark);
            inner.color = SpriteFactory.Hex(s.Color);
        }

        /// <param name="radius">world units</param>
        /// <param name="glow01">0..1 brightness (lives)</param>
        /// <param name="face">0 open, 1 squint, 2 dazed</param>
        public void Render(Vector3 pos, float radius, Vector2 squashXY, float glow01, float alpha, int face, float time)
        {
            transform.position = pos;
            transform.localScale = Vector3.one * radius;
            squash.localScale = new Vector3(squashXY.x, squashXY.y, 1f);

            Color c = skin != null ? SpriteFactory.Hex(skin.Color) : Color.white;
            halo.color = new Color(c.r, c.g, c.b, 0.35f * glow01 * alpha);
            dim.color = new Color(0.06f, 0.11f, 0.19f, (1f - glow01) * 0.65f * alpha);
            SetAlpha(body, alpha); SetAlpha(inner, alpha); SetAlpha(shine, 0.5f * alpha);
            SetAlpha(cheekL, 0.25f * alpha); SetAlpha(cheekR, 0.25f * alpha);
            SetAlpha(eyeL, alpha); SetAlpha(eyeR, alpha);

            bool blink = (time % 3.6f) < 0.12f;
            float eyeY = face == 1 ? 0.22f : face == 2 ? 0.5f : blink ? 0.2f : 1f;
            eyeL.transform.localScale = eyeR.transform.localScale = new Vector3(0.18f, 0.18f * eyeY, 1f);
        }

        static void SetAlpha(SpriteRenderer sr, float a)
        {
            var c = sr.color; c.a = a; sr.color = c;
        }
    }

    /// <summary>A gate: two rounded bars with a gap, plus the dashed "perfect size" ghost ring.</summary>
    public class GateView : MonoBehaviour
    {
        SpriteRenderer left, right, leftShadow, rightShadow, ghost;

        public static GateView Create(Transform parent)
        {
            var go = new GameObject("Gate");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<GateView>();
            v.left = v.Bar("L", 31); v.right = v.Bar("R", 31);
            v.leftShadow = v.Bar("LS", 30); v.rightShadow = v.Bar("RS", 30);
            var g = new GameObject("Ghost");
            g.transform.SetParent(go.transform, false);
            v.ghost = g.AddComponent<SpriteRenderer>();
            v.ghost.sprite = SpriteFactory.DashedRing;
            v.ghost.sortingOrder = 29;
            return v;
        }

        SpriteRenderer Bar(string n, int order)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Bar;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>All sizes in world units. halfScreen = half the visible width.</summary>
        public void Render(float y, float gap, float barH, float halfScreen, Color col, Color dark,
            float ghostAlpha, bool showGhost, float flash)
        {
            const float overhang = 0.1f;
            float barW = halfScreen - gap / 2f + overhang;
            float shadowDrop = 0.04f;
            Color lit = Color.Lerp(col, Color.white, flash);

            Place(left, -halfScreen - overhang + barW / 2f, y, barW, barH, lit);
            Place(right, halfScreen + overhang - barW / 2f, y, barW, barH, lit);
            Place(leftShadow, -halfScreen - overhang + barW / 2f, y - shadowDrop, barW, barH, dark);
            Place(rightShadow, halfScreen + overhang - barW / 2f, y - shadowDrop, barW, barH, dark);

            ghost.enabled = showGhost;
            ghost.transform.position = new Vector3(0f, y, 0f);
            ghost.transform.localScale = Vector3.one * gap * 0.93f;
            ghost.color = new Color(1f, 0.96f, 0.91f, ghostAlpha);
        }

        static void Place(SpriteRenderer sr, float x, float y, float w, float h, Color c)
        {
            sr.transform.position = new Vector3(x, y, 0f);
            sr.size = new Vector2(Mathf.Max(w, h), h);
            sr.color = c;
        }
    }

    /// <summary>Theme gradient plus drifting fireflies that speed up with the game.</summary>
    public class Backdrop : MonoBehaviour
    {
        SpriteRenderer bg;
        SpriteRenderer[] motes;
        Vector3[] moteData; // x, y (logical), depth
        string themeId;

        public static Backdrop Create(Transform parent)
        {
            var go = new GameObject("Backdrop");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<Backdrop>();
            var bgo = new GameObject("Gradient");
            bgo.transform.SetParent(go.transform, false);
            b.bg = bgo.AddComponent<SpriteRenderer>();
            b.bg.sortingOrder = -100;
            b.motes = new SpriteRenderer[34];
            b.moteData = new Vector3[34];
            for (int i = 0; i < b.motes.Length; i++)
            {
                var m = new GameObject("Mote");
                m.transform.SetParent(go.transform, false);
                b.motes[i] = m.AddComponent<SpriteRenderer>();
                b.motes[i].sprite = SpriteFactory.Circle;
                b.motes[i].sortingOrder = -90;
                b.moteData[i] = new Vector3(Random.value * 360f, Random.value * 900f, Random.Range(0.2f, 1f));
            }
            return b;
        }

        public void Tick(float dt, float time, float speedPx, float logicalW, float logicalH, float px, ThemeDef theme)
        {
            if (themeId != theme.Id)
            {
                themeId = theme.Id;
                bg.sprite = SpriteFactory.VerticalGradient(SpriteFactory.Hex(theme.Top), SpriteFactory.Hex(theme.Bottom));
            }
            bg.transform.localScale = new Vector3((logicalW + 40) * px * 64f, (logicalH + 40) * px, 1f);

            Color mc = SpriteFactory.Hex(theme.Mote);
            for (int i = 0; i < motes.Length; i++)
            {
                var d = moteData[i];
                d.y += speedPx * 0.25f * d.z * dt;
                if (d.y > logicalH + 10) { d.y = -10; d.x = Random.value * logicalW; }
                moteData[i] = d;
                motes[i].transform.position = new Vector3((d.x - logicalW / 2f) * px, (logicalH / 2f - d.y) * px, 0f);
                motes[i].transform.localScale = Vector3.one * (2f + d.z * 3.6f) * px;
                mc.a = 0.1f + 0.25f * d.z * (0.6f + 0.4f * Mathf.Sin(time * 2f + i));
                motes[i].color = mc;
            }
        }
    }
}
