using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Snug
{
    // ====================================================================== particles
    /// <summary>Pooled sprite particles (no ParticleSystem, so no material/shader setup in any render pipeline).</summary>
    public class Particles : MonoBehaviour
    {
        class P { public SpriteRenderer sr; public Vector2 pos, vel; public float life, max, size, gravity, grow; public bool ring; public Color col; }
        readonly List<P> live = new List<P>();
        readonly Stack<P> free = new Stack<P>();

        public static Particles Create(Transform parent)
        {
            var go = new GameObject("Particles");
            go.transform.SetParent(parent, false);
            return go.AddComponent<Particles>();
        }

        P Get(bool ring)
        {
            P p = free.Count > 0 ? free.Pop() : new P();
            if (p.sr == null)
            {
                var go = new GameObject("p");
                go.transform.SetParent(transform, false);
                p.sr = go.AddComponent<SpriteRenderer>();
                p.sr.sortingOrder = 50;
            }
            p.sr.sprite = ring ? SpriteFactory.Ring : SpriteFactory.Circle;
            p.sr.enabled = true;
            p.ring = ring;
            live.Add(p);
            return p;
        }

        /// <summary>World-space burst around a circle of radius r.</summary>
        public void Burst(Vector2 center, float r, int count, Color a, Color b, float speed, float sizeMax)
        {
            for (int i = 0; i < count; i++)
            {
                float ang = UnityEngine.Random.value * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                var p = Get(false);
                p.pos = center + dir * r;
                p.vel = dir * UnityEngine.Random.Range(0.8f, speed) + Vector2.up * 0.4f;
                p.life = p.max = UnityEngine.Random.Range(0.35f, 0.8f);
                p.size = UnityEngine.Random.Range(0.04f, sizeMax);
                p.gravity = -2.2f;
                p.col = UnityEngine.Random.value < 0.4f ? b : a;
            }
        }

        public void Splat(Vector2 center, float r, Color c)
        {
            for (int i = 0; i < 34; i++)
            {
                var p = Get(false);
                p.pos = center + new Vector2(UnityEngine.Random.Range(-r, r), UnityEngine.Random.Range(-0.08f, 0.08f));
                p.vel = new Vector2(UnityEngine.Random.Range(-2.6f, 2.6f), UnityEngine.Random.Range(-0.4f, 3.2f));
                p.life = p.max = UnityEngine.Random.Range(0.5f, 1f);
                p.size = UnityEngine.Random.Range(0.06f, 0.16f);
                p.gravity = -6f;
                p.col = c;
            }
        }

        public void Ring(Vector2 center, float r)
        {
            var p = Get(true);
            p.pos = center; p.vel = Vector2.zero; p.life = p.max = 0.45f; p.size = r * 2f; p.grow = 5.2f; p.gravity = 0;
            p.col = new Color(1f, 0.96f, 0.91f, 0.7f);
        }

        public void Tick(float dt)
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var p = live[i];
                p.life -= dt;
                if (p.life <= 0) { p.sr.enabled = false; live.RemoveAt(i); free.Push(p); continue; }
                float k = p.life / p.max;
                if (p.ring) p.size += p.grow * dt;
                else { p.vel.y += p.gravity * dt; p.pos += p.vel * dt; }
                p.sr.transform.position = p.pos;
                p.sr.transform.localScale = Vector3.one * (p.ring ? p.size : p.size * k + 0.01f);
                var c = p.col; c.a *= p.ring ? k : Mathf.Clamp01(k * 1.4f); p.sr.color = c;
            }
        }
    }

    // ====================================================================== audio
    /// <summary>Synthesized sounds (no audio assets). Perfect notes climb a pentatonic scale with the chain.</summary>
    public class SnugAudio : MonoBehaviour
    {
        enum Wave { Sine, Triangle, Square, Saw }
        AudioSource oneShot, hum;
        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static readonly int[] Penta = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21, 24 };
        public bool Enabled = true;

        public static SnugAudio Create(Transform parent)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<SnugAudio>();
            a.oneShot = go.AddComponent<AudioSource>();
            a.oneShot.playOnAwake = false;
            a.hum = go.AddComponent<AudioSource>();
            a.hum.clip = Synth("hum", 220f, 1f, Wave.Sine, 1f, 1f, false);
            a.hum.loop = true;
            a.hum.volume = 0f;
            a.hum.Play();
            return a;
        }

        static float Note(int step) => 523.25f * Mathf.Pow(2f, Penta[Mathf.Clamp(step, 0, Penta.Length - 1)] / 12f);

        public void Perfect(int chainStep) { Play("p" + chainStep, () => Mix(Synth("", Note(chainStep), 0.22f, Wave.Triangle, 0.5f), Synth("", Note(chainStep) * 2f, 0.16f, Wave.Sine, 0.18f))); }
        public void Good() => Play("good", () => Synth("good", 392f, 0.12f, Wave.Triangle, 0.35f));
        public void Loose() => Play("loose", () => Synth("loose", 160f, 0.22f, Wave.Sine, 0.5f, 0.6f));
        public void Squeeze() => Play("squeeze", () => Mix(Synth("", 700f, 0.08f, Wave.Square, 0.12f, 1.6f), Synth("", Note(6), 0.25f, Wave.Triangle, 0.45f)));
        public void Crash() => Play("crash", () => Mix(Synth("", 220f, 0.35f, Wave.Saw, 0.25f, 0.3f), Synth("", 90f, 0.4f, Wave.Sine, 0.6f, 0.5f)));
        public void Coin() => Play("coin", () => Mix(Synth("", 1318f, 0.07f, Wave.Square, 0.1f), Synth("", 1760f, 0.12f, Wave.Square, 0.1f)));
        public void Ui() => Play("ui", () => Synth("ui", 660f, 0.06f, Wave.Triangle, 0.2f));
        public void Reward() => Play("reward", () => Mix(Synth("", Note(0), 0.4f, Wave.Triangle, 0.35f), Synth("", Note(3), 0.4f, Wave.Triangle, 0.3f), Synth("", Note(5), 0.4f, Wave.Triangle, 0.25f)));

        /// <summary>Continuous hum whose pitch follows the blob size: you can hear how big you are.</summary>
        public void SetHum(bool on, bool holding, float radiusPx)
        {
            float target = Enabled && on ? (holding ? 0.09f : 0.03f) : 0f;
            hum.volume = Mathf.MoveTowards(hum.volume, target, Time.unscaledDeltaTime * 0.6f);
            hum.pitch = (140f + radiusPx * 3.2f) / 220f;
        }

        void Play(string key, Func<AudioClip> make)
        {
            if (!Enabled) return;
            if (!cache.TryGetValue(key, out var clip)) cache[key] = clip = make();
            oneShot.PlayOneShot(clip);
        }

        const int Rate = 44100;

        static AudioClip Synth(string name, float freq, float dur, Wave w, float vol, float slide = 1f, bool envelope = true)
        {
            int n = Mathf.CeilToInt(Rate * dur);
            var data = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float f = freq * Mathf.Pow(slide, t / dur);
                phase += f / Rate;
                float ph = (float)(phase - Math.Floor(phase));
                float s;
                switch (w)
                {
                    case Wave.Triangle: s = 4f * Mathf.Abs(ph - 0.5f) - 1f; break;
                    case Wave.Square: s = ph < 0.5f ? 1f : -1f; break;
                    case Wave.Saw: s = 2f * ph - 1f; break;
                    default: s = Mathf.Sin(ph * Mathf.PI * 2f); break;
                }
                float env = envelope ? Mathf.Min(1f, t / 0.008f) * Mathf.Exp(-5f * t / dur) : 1f;
                data[i] = s * env * vol;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Mix(params AudioClip[] clips)
        {
            int n = 0;
            foreach (var c in clips) n = Mathf.Max(n, c.samples);
            var outData = new float[n];
            foreach (var c in clips)
            {
                var d = new float[c.samples];
                c.GetData(d, 0);
                for (int i = 0; i < d.Length; i++) outData[i] += d[i];
            }
            var clip = AudioClip.Create("mix", n, 1, Rate, false);
            clip.SetData(outData, 0);
            return clip;
        }
    }

    // ====================================================================== haptics
    /// <summary>Short taps, not Handheld.Vibrate's long buzz. iOS uses Plugins/iOS/SnugHaptics.mm.</summary>
    public static class Haptics
    {
        public static bool Enabled = true;

        public static void Light() => Play(10, 60, 0);
        public static void Medium() => Play(18, 120, 1);
        public static void Heavy() => Play(45, 255, 2);

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void _SnugImpact(int style);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static int sdk = -1;
#endif

        static void Play(long ms, int amplitude, int iosStyle)
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (vibrator == null)
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                        sdk = version.GetStatic<int>("SDK_INT");
                }
                if (sdk >= 26)
                {
                    using (var effects = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effects.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude))
                        vibrator.Call("vibrate", effect);
                }
                else vibrator.Call("vibrate", ms);
            }
            catch (Exception) { /* no vibrator */ }
#elif UNITY_IOS && !UNITY_EDITOR
            _SnugImpact(iosStyle);
#endif
        }

        // Referencing Handheld.Vibrate makes Unity add the Android VIBRATE permission. Never called.
        public static void EnsureAndroidPermission()
        {
#if UNITY_ANDROID
            if (DateTime.Now.Year < 2000) Handheld.Vibrate();
#endif
        }
    }
}
