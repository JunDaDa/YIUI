namespace ET
{
    public static class I2LocalizeHelper
    {
        public static string GetTranslation(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            return EventSystem.Instance.Invoke<Invoke_Localization_GetTranslation, string>(new Invoke_Localization_GetTranslation()
            {
                Key = key
            });
        }
    }
}