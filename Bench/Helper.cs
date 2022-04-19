namespace Bench;

public static class Helper
{
    // get directory total size
    public static long GetDirectorySize(this string path, string? ignoreExt = null)
    {
        long size = 0;
        foreach (string file in Directory.GetFiles(path))
        {
            if(ignoreExt != null && Path.GetExtension(file) == ignoreExt)
            {
                continue;
            }
            var fi = new FileInfo(file);
            size += fi.Length;
        }
        foreach (string dir in Directory.GetDirectories(path))
        {
            size += GetDirectorySize(dir);
        }
        return size;
    }
    
    // get file size
    public static long GetFileSize(this string path)
    {
        var fi = new FileInfo(path);
        return fi.Length;
    }
}