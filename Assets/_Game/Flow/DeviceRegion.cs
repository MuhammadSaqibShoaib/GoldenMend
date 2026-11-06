using UnityEngine;

namespace GoldenMend.Flow
{
    /// <summary>
    /// The player's country, for deciding whether to ask about ads before showing any: the SIM's country, then the
    /// mobile network's, then the phone's language region. Empty when none is known (the caller then asks, to be safe).
    /// </summary>
    public static class DeviceRegion
    {
        public static string Country()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var telephony = activity.Call<AndroidJavaObject>("getSystemService", "phone"))
                {
                    if (telephony != null)
                    {
                        string sim = telephony.Call<string>("getSimCountryIso");
                        if (!string.IsNullOrEmpty(sim)) return sim.ToUpperInvariant();
                        string network = telephony.Call<string>("getNetworkCountryIso");
                        if (!string.IsNullOrEmpty(network)) return network.ToUpperInvariant();
                    }
                }
                using (var locale = new AndroidJavaClass("java.util.Locale"))
                using (var current = locale.CallStatic<AndroidJavaObject>("getDefault"))
                {
                    string country = current.Call<string>("getCountry");
                    if (!string.IsNullOrEmpty(country)) return country.ToUpperInvariant();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Golden Mend: could not read the device country: " + e.Message);
            }
#endif
            return "";
        }
    }
}
