namespace UI.Helpers
{
    public static class ResourceTextHelper
    {
        public static string L(string key, string fallback)
        {
            var value = AppResource.Labels.ResourceManager.GetString(key)
                        ?? AppResource.Actions.ResourceManager.GetString(key)
                        ?? AppResource.message.ResourceManager.GetString(key)
                        ?? AppResource.Shipping.ResourceManager.GetString(key);

            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
