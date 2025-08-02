public static class UrlHelper
{
    public static string Join(params string[] parts)
    {
        return string.Join("/", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim('/')));
    }
}
