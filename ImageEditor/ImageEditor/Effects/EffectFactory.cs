using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageEditor.Effects
{
    public static class EffectFactory
    {
        public static IImageEffect CreateEffect(string effectType)
        {
            return effectType switch
            {
                "Grayscale" => new GrayscaleEffect(),
                "Sepia" => new SepiaEffect(),
                "Invert" => new InvertEffect(),
                "Brighten" => new BrightnessEffect(50),
                "Darken" => new BrightnessEffect(-50),
                "HighContrast" => new ContrastEffect(1.5),
                "LowContrast" => new ContrastEffect(0.5),
                "Blur" => new BlurEffect(5),
                _ => null
            };
        }

        public static List<EffectInfo> GetAvailableEffects()
        {
            return new List<EffectInfo>
            {
                new EffectInfo("Grayscale", "⬜ Чорно-білий"),
                new EffectInfo("Sepia", "🟫 Сепія"),
                new EffectInfo("Invert", "🔄 Інверсія"),
                new EffectInfo("Brighten", "☀️ Світліше"),
                new EffectInfo("Darken", "🌙 Темніше"),
                new EffectInfo("HighContrast", "📊 Високий контраст"),
                new EffectInfo("LowContrast", "📉 Низький контраст"),
                new EffectInfo("Blur", "🌫️ Розмиття")
            };
        }
    }

    public class EffectInfo
    {
        public string Type { get; set; }
        public string DisplayName { get; set; }

        public EffectInfo(string type, string displayName)
        {
            Type = type;
            DisplayName = displayName;
        }
    }
}
