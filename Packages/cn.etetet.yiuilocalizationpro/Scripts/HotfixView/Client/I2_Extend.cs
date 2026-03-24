using I2.Loc;

namespace ET.Client
{
    public static class I2_Extend
    {
        public static string Localize(this I2 i2)
        {
            return LocalizationManager.GetTranslation(i2.Key);
        }

        public static string Localize(this I2 i2, object p1)
        {
            var translation = LocalizationManager.GetTranslation(i2.Key);
            return string.Format(translation, p1);
        }

        public static string Localize(this I2 i2, object p1, object p2)
        {
            var translation = LocalizationManager.GetTranslation(i2.Key);
            return string.Format(translation, p1, p2);
        }

        public static string Localize(this I2 i2, object p1, object p2, object p3)
        {
            var translation = LocalizationManager.GetTranslation(i2.Key);
            return string.Format(translation, p1, p2, p3);
        }

        public static string Localize(this I2 i2, params object[] p1)
        {
            var translation = LocalizationManager.GetTranslation(i2.Key);
            return string.Format(translation, p1);
        }
    }
}