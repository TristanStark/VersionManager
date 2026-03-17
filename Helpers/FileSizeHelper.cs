namespace VersionManager.Helpers;

public static class FileSizeHelper
{
    public static string ToReadableSize(long bytes)
    {
        string[] units = ["o", "Ko", "Mo", "Go", "To"];
        double size = bytes;
        int unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {units[unit]}";
    }
}