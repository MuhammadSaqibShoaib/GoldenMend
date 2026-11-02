using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>A public-domain painting a vessel is glazed with, and how to credit it.</summary>
    [Serializable]
    public sealed class Artwork
    {
        public string Key;
        public string Title;
        public string FullTitle;
        public string Artist;
        public string Date;
        public string Museum;
        public string Url;
        public string License;
        public Texture2D Image;

        /// <summary>"Katsushika Hokusai, 1830/33".</summary>
        public string Byline => string.IsNullOrEmpty(Date) ? Artist : $"{Artist}, {Date}";
    }

    /// <summary>
    /// The paintings the vessels are glazed with, keyed by <c>LevelSpec.Artwork</c>. Built from
    /// Content/Art/artworks.json by Golden Mend > Rebuild Game Scene. Without a catalog the game uses the
    /// generated glaze art (the Classic build).
    /// </summary>
    [CreateAssetMenu(menuName = "Golden Mend/Art Catalog", fileName = "ArtCatalog")]
    public sealed class ArtCatalog : ScriptableObject
    {
        public List<Artwork> Works = new List<Artwork>();

        public Artwork Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            foreach (var work in Works)
            {
                if (work.Key == key) return work;
            }
            return null;
        }
    }
}
