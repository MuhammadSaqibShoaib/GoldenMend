using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// Sound and haptics for every board moment. Plays the <see cref="SoundBank"/> when one is set, otherwise
    /// generated tones (tests and tools). Lock chimes climb a pentatonic scale through a chain.
    /// </summary>
    public sealed class FeedbackService : MonoBehaviour
    {
        static readonly int[] Pentatonic = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21, 24 };
        const int SampleRate = 44100;
        const int Voices = 6;

        AudioSource[] voices;
        int nextVoice;
        SoundBank bank;
        float lastUncover;

        AudioClip toneChime;
        AudioClip toneThud;
        AudioClip toneBuzz;

        public bool Muted { get; set; }

        public bool HapticsOn
        {
            get => Haptics.Enabled;
            set => Haptics.Enabled = value;
        }

        public void Init(SoundBank sounds) => bank = sounds;

        void Awake()
        {
            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
            toneChime = Tone("chime", 880f, 0.7f, 5.5f, 0.35f);
            toneThud = Tone("thud", 196f, 0.18f, 18f, 0.4f);
            toneBuzz = Tone("buzz", 110f, 0.45f, 4f, 0.35f, square: true);
        }

        public void Pick()
        {
            Play(Any(bank != null ? bank.Pick : null), Vary(), 0.7f);
            Haptics.Tick();
        }

        public void Park()
        {
            Play(Any(bank != null ? bank.Park : null) ?? toneThud, Vary(), 1f);
            Haptics.Tick();
        }

        /// <summary>The n-th lock within one tap plays a higher note and a firmer click.</summary>
        public void Lock(int chainIndex)
        {
            float pitch = Semitones(Pentatonic[Mathf.Min(chainIndex, Pentatonic.Length - 1)]);
            Play(Any(bank != null ? bank.Lock : null) ?? toneChime, pitch, 1f);
            if (chainIndex == 0) Haptics.Click();
            else Haptics.HeavyClick();
        }

        public void Uncover()
        {
            // Several shards can open on one tap; one pluck is enough.
            if (Time.unscaledTime - lastUncover < 0.08f) return;
            lastUncover = Time.unscaledTime;
            Play(Any(bank != null ? bank.Uncover : null), Vary(), 0.5f);
        }

        public void Reject()
        {
            Play(bank != null ? bank.Reject : null, 1f, 0.6f);
            Haptics.Tick();
        }

        public void Expand()
        {
            Play(bank != null ? bank.Expand : null, 1f, 1f);
            Haptics.DoubleClick();
        }

        public void Coins() => Play(Any(bank != null ? bank.Coins : null), 1f, 0.9f);

        public void Win()
        {
            if (bank != null && bank.WinJingle != null) PlayJingle(bank.WinJingle);
            else Play(toneChime, Semitones(12), 1f);
            Haptics.Success();
        }

        public void Fail()
        {
            if (bank != null && bank.FailJingle != null) PlayJingle(bank.FailJingle);
            else Play(toneBuzz, 1f, 1f);
            Haptics.Failure();
        }

        /// <summary>Long system vibration, used only if the fine-grained haptics are unavailable.</summary>
        public void Buzz()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (HapticsOn && Application.isPlaying) Handheld.Vibrate();
#endif
        }

        void Play(AudioClip clip, float pitch, float volume)
        {
            if (Muted || clip == null || voices == null) return;
            var voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.pitch = pitch;
            voice.volume = volume * (bank != null ? bank.EffectsVolume : 1f);
            voice.clip = clip;
            voice.Play();
        }

        void PlayJingle(AudioClip clip)
        {
            if (Muted || voices == null) return;
            var voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.pitch = 1f;
            voice.volume = bank.JingleVolume;
            voice.clip = clip;
            voice.Play();
        }

        static AudioClip Any(AudioClip[] clips) =>
            clips == null || clips.Length == 0 ? null : clips[Random.Range(0, clips.Length)];

        /// <summary>A small random pitch shift so repeated sounds don't feel mechanical.</summary>
        static float Vary() => Random.Range(0.94f, 1.06f);

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
