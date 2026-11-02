using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// Every sound effect the board plays, so they can be auditioned and swapped in the Inspector.
    /// Built from Kenney's CC0 audio packs by Golden Mend > Rebuild Game Scene.
    /// </summary>
    [CreateAssetMenu(menuName = "Golden Mend/Sound Bank", fileName = "SoundBank")]
    public sealed class SoundBank : ScriptableObject
    {
        [Tooltip("Shard lifted out of the rack.")]
        public AudioClip[] Pick;

        [Tooltip("Shard set down on the workbench.")]
        public AudioClip[] Park;

        [Tooltip("Shard locks into the vessel. Pitched up the pentatonic scale through a chain.")]
        public AudioClip[] Lock;

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

        [Range(0f, 1f)] public float EffectsVolume = 0.8f;
        [Range(0f, 1f)] public float JingleVolume = 0.7f;
    }
}
