using System;

namespace GoldenMend.Meta
{
    /// <summary>
    /// The player's choice about personalized ads, and where the game must ask before showing any.
    ///
    /// Players in the EEA, the UK and Switzerland (GDPR and its equivalents) are asked once, before the ad SDK starts.
    /// Everyone else gets personalized ads by default and can turn them off in Settings, which also counts as the US
    /// "do not sell or share my data" opt-out. A player whose country cannot be told is asked, to be safe.
    /// Not legal advice: check the regions and wording against your own privacy policy before launch.
    /// </summary>
    public sealed class AdConsent
    {
        public const int Unknown = 0;
        public const int Granted = 1;
        public const int Denied = 2;

        // EU/EEA member states, the UK and Switzerland, as ISO 3166-1 alpha-2 codes.
        static readonly string[] AskFirstCountries =
        {
            "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR", "DE", "GR", "HU", "IE", "IT", "LV", "LT", "LU",
            "MT", "NL", "PL", "PT", "RO", "SK", "SI", "ES", "SE", "IS", "LI", "NO", "GB", "UK", "CH",
        };

        readonly SaveData data;
        readonly ISaveStore store;

        public AdConsent(SaveData data, ISaveStore store)
        {
            this.data = data;
            this.store = store;
        }

        public static bool MustAskFirst(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode)) return true;
            return Array.IndexOf(AskFirstCountries, countryCode.Trim().ToUpperInvariant()) >= 0;
        }

        /// <summary>True when the consent prompt has to be answered before the ad SDK starts.</summary>
        public bool NeedsPrompt(string countryCode) => data.AdConsent == Unknown && MustAskFirst(countryCode);

        /// <summary>Whether ads may be personalized. Unknown counts as yes only where asking first is not required.</summary>
        public bool Personalized(string countryCode) =>
            data.AdConsent == Granted || (data.AdConsent == Unknown && !MustAskFirst(countryCode));

        public bool Decided => data.AdConsent != Unknown;

        public void Set(bool personalized)
        {
            data.AdConsent = personalized ? Granted : Denied;
            store.Save(data);
        }
    }
}
