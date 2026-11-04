using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// Every sound effect the board plays, and the music, so they can be auditioned and swapped in the Inspector.
    /// The clips are made by Tools/Audio/make_sounds.py (Kenney CC0 recordings, cleaned up, plus synthesised chimes);
    /// Golden Mend > Rebuild Sound Bank fills this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Golden Mend/Sound Bank", fileName = "SoundBank")]
    public sealed class SoundBank : ScriptableObject
    {
        [Tooltip("Shard lifted out of the rack.")]
        public AudioClip[] Pick;

        [Tooltip("Shard set down on the workbench.")]
        public AudioClip[] Park;

        [Tooltip("Shard locks into the vessel. A C5 chime pitched up the pentatonic scale through a chain.")]
        public AudioClip Lock;

        [Tooltip("A locked rack shard opens up.")]
        public AudioClip[] Uncover;

        [Tooltip("Tap on a shard that can't move yet.")]
        public AudioClip Reject;

        [Tooltip("Continue offer adds workbench slots.")]
        public AudioClip Expand;

        [Tooltip("Coins awarded.")]
        public AudioClip[] Coins;

        public AudioClip WinJingle;
        public AudioClip FailJingle;

        [Tooltip("Background loop, seamless.")]
        public AudioClip Music;

        [Range(0f, 1f)] public float EffectsVolume = 0.8f;
        [Range(0f, 1f)] public float JingleVolume = 0.7f;
        [Range(0f, 1f)] public float MusicVolume = 0.45f;
    }
}
