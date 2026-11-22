namespace ShouldIRestock
{
    public static class Text
    {
        public static string L(string key, string fallback)
        {
            try
            {
                string value = ModHelper.GetLocalized(ModIds.Id, key);
                if (!string.IsNullOrEmpty(value) && value != key)
                {
                    return value;
                }
            }
            catch
            {
            }

            return fallback;
        }

        public static string F(string key, string fallback, params object[] args)
        {
            try
            {
                return string.Format(L(key, fallback), args);
            }
            catch
            {
                return fallback;
            }
        }
    }
}
