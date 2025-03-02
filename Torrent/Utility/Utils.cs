public static class Utility
{
    // Converts bytes into a human-readable format (KB, MB, GB, etc.)
    public static string ConvertSizeToReadable(long bytes)
    {
        if (bytes <= 0)
            return "0 B";

        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        int i = 0;
        double size = bytes;

        // Determine the correct suffix
        while (size >= 1024 && i < suffixes.Length - 1)
        {
            size /= 1024;
            i++;
        }

        // Format the size with 2 decimal places
        return string.Format("{0:0.##} {1}", size, suffixes[i]);
    }
}
