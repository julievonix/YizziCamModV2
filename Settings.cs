using System.IO;
using BepInEx;

namespace YizziCamModV2
{
    public static class Settings
    {
        private static string FilePath => Path.Combine(Paths.ConfigPath, "YizziCamMod.cfg");

        public static void Save(int viewMode, float fov, bool watermark, float smoothing,
            int timePreset, bool rain, float nearClip, int summonInputMode, bool fpvRawRotation,
            bool fpvClipping, float fpvClipLag)
        {
            using (StreamWriter sw = new StreamWriter(FilePath))
            {
                sw.WriteLine("viewMode=" + viewMode);
                sw.WriteLine("fov=" + fov.ToString("F2"));
                sw.WriteLine("watermark=" + (watermark ? "1" : "0"));
                sw.WriteLine("smoothing=" + smoothing.ToString("F4"));
                sw.WriteLine("timePreset=" + timePreset);
                sw.WriteLine("rain=" + (rain ? "1" : "0"));
                sw.WriteLine("nearClip=" + nearClip.ToString("F4"));
                sw.WriteLine("summonMode=" + summonInputMode);
                sw.WriteLine("fpvRawRotation=" + (fpvRawRotation ? "1" : "0"));
                sw.WriteLine("fpvClipping=" + (fpvClipping ? "1" : "0"));
                sw.WriteLine("fpvClipLag=" + fpvClipLag.ToString("F4"));
            }
        }

        public static bool Load(out int viewMode, out float fov, out bool watermark,
            out float smoothing, out int timePreset, out bool rain, out float nearClip, out int summonInputMode,
            out bool fpvRawRotation, out bool fpvClipping, out float fpvClipLag)
        {
            viewMode = 0;
            fov = 60f;
            watermark = true;
            smoothing = 0.05f;
            timePreset = 1;
            rain = false;
            nearClip = 0.1f;
            summonInputMode = 0;
            bool? legacyUseF6 = null;
            bool parsedSummonMode = false;
            fpvRawRotation = false;
            fpvClipping = false;
            fpvClipLag = 0.5f;

            if (!File.Exists(FilePath))
                return false;

            foreach (string line in File.ReadAllLines(FilePath))
            {
                string[] parts = line.Split('=');
                if (parts.Length != 2) continue;
                string key = parts[0].Trim();
                string val = parts[1].Trim();

                switch (key)
                {
                    case "viewMode": int.TryParse(val, out viewMode); break;
                    case "fpv": if (val == "1") viewMode = 0; break;
                    case "fov": float.TryParse(val, out fov); break;
                    case "watermark": watermark = val == "1"; break;
                    case "smoothing": float.TryParse(val, out smoothing); break;
                    case "timePreset": int.TryParse(val, out timePreset); break;
                    case "rain": rain = val == "1"; break;
                    case "nearClip": float.TryParse(val, out nearClip); break;
                    case "useF6": legacyUseF6 = val == "1"; break;
                    case "summonMode":
                        if (int.TryParse(val, out int sm))
                        {
                            summonInputMode = sm;
                            parsedSummonMode = true;
                        }
                        break;
                    case "fpvRawRotation": fpvRawRotation = val == "1"; break;
                    case "fpvClipping": fpvClipping = val == "1"; break;
                    case "fpvClipLag": float.TryParse(val, out fpvClipLag); break;
                }
            }
            if (!parsedSummonMode && legacyUseF6.HasValue)
                summonInputMode = legacyUseF6.Value ? 0 : 1;
            if (summonInputMode < 0 || summonInputMode > 2)
                summonInputMode = 0;
            return true;
        }
    }
}
