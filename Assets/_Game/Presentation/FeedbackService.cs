using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>Procedural sounds: a chime that climbs a pentatonic scale through a chain.</summary>
    public sealed class FeedbackService : MonoBehaviour
    {
        static readonly int[] Pentatonic = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21, 24 };
        const int SampleRate = 44100;

        AudioSource source;
        AudioClip chime;
        AudioClip thud;
        AudioClip buzz;

        public bool Muted { get; set; }

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            chime = Tone("chime", 880f, 0.7f, 5.5f, 0.35f);
            thud = Tone("thud", 196f, 0.18f, 18f, 0.4f);
            buzz = Tone("buzz", 110f, 0.45f, 4f, 0.35f, square: true);
        }

        /// <summary>The n-th lock within one tap plays a higher note.</summary>
        public void Lock(int chainIndex) => Play(chime, Semitones(Pentatonic[Mathf.Min(chainIndex, Pentatonic.Length - 1)]));

        public void Park() => Play(thud, 1f);

        public void Fail() => Play(buzz, 1f);

        public void Win(int step) => Play(chime, Semitones(Pentatonic[Mathf.Min(step * 2, Pentatonic.Length - 1)]));

        void Play(AudioClip clip, float pitch)
        {
            if (Muted || clip == null) return;
            source.pitch = pitch;
            source.PlayOneShot(clip);
        }

        static float Semitones(int n) => Mathf.Pow(2f, n / 12f);

        static AudioClip Tone(string name, float frequency, float seconds, float decay, float volume, bool square = false)
        {
            int count = Mathf.CeilToInt(SampleRate * seconds);
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float phase = 2f * Mathf.PI * frequency * t;
                float wave = square
                    ? Mathf.Sign(Mathf.Sin(phase)) * 0.5f
                    : Mathf.Sin(phase) + 0.35f * Mathf.Sin(phase * 2.01f) + 0.15f * Mathf.Sin(phase * 3.02f);
                float attack = Mathf.Clamp01(t / 0.004f);
                data[i] = wave * attack * Mathf.Exp(-decay * t) * volume;
            }
            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
