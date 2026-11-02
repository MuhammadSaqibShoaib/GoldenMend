using UnityEngine;

namespace GoldenMend.Presentation
{
    /// <summary>
    /// Short, crisp vibrations through Android's VibrationEffect API (system tick and click effects on
    /// Android 10+, timed pulses before that). Silent in the editor and on other platforms.
    /// </summary>
    public static class Haptics
    {
        // android.os.VibrationEffect predefined effect ids.
        const int EffectClick = 0;
        const int EffectDoubleClick = 1;
        const int EffectTick = 2;
        const int EffectHeavyClick = 5;

        public static bool Enabled { get; set; } = true;

        /// <summary>Lightest touch: picking a shard, a rejected tap.</summary>
        public static void Tick() => Predefined(EffectTick, 12);

        /// <summary>A shard locking into the vessel.</summary>
        public static void Click() => Predefined(EffectClick, 20);

        /// <summary>A shard flying home as part of a chain.</summary>
        public static void HeavyClick() => Predefined(EffectHeavyClick, 35);

        public static void DoubleClick() => Predefined(EffectDoubleClick, 30);

        /// <summary>Level complete: three rising pulses.</summary>
        public static void Success() => Waveform(new long[] { 0, 28, 70, 28, 70, 60 }, new[] { 0, 110, 0, 170, 0, 255 });

        /// <summary>Workbench full: two firm pulses.</summary>
        public static void Failure() => Waveform(new long[] { 0, 90, 90, 140 }, new[] { 0, 210, 0, 255 });

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static int sdk;
        static bool unavailable;

        static bool Ready()
        {
            if (!Enabled || unavailable) return false;
            if (vibrator != null) return true;
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION")) sdk = version.GetStatic<int>("SDK_INT");
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    if (sdk >= 31)
                    {
                        var manager = activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager");
                        vibrator = manager.Call<AndroidJavaObject>("getDefaultVibrator");
                    }
                    else
                    {
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    }
                }
                unavailable = vibrator == null || !vibrator.Call<bool>("hasVibrator");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Golden Mend: haptics unavailable ({e.Message}).");
                unavailable = true;
            }
            return !unavailable;
        }

        static void Predefined(int effect, long fallbackMilliseconds)
        {
            if (!Ready()) return;
            try
            {
                using (var effects = new AndroidJavaClass("android.os.VibrationEffect"))
                {
                    var fx = sdk >= 29
                        ? effects.CallStatic<AndroidJavaObject>("createPredefined", effect)
                        : effects.CallStatic<AndroidJavaObject>("createOneShot", fallbackMilliseconds, -1);
                    vibrator.Call("vibrate", fx);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Golden Mend: haptic failed ({e.Message}).");
            }
        }

        static void Waveform(long[] timings, int[] amplitudes)
        {
            if (!Ready()) return;
            try
            {
                using (var effects = new AndroidJavaClass("android.os.VibrationEffect"))
                {
                    var fx = effects.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1);
                    vibrator.Call("vibrate", fx);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Golden Mend: haptic failed ({e.Message}).");
            }
        }
#else
        static void Predefined(int effect, long fallbackMilliseconds)
        {
        }

        static void Waveform(long[] timings, int[] amplitudes)
        {
        }
#endif
    }
}
