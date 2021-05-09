using System.IO;

public class PrefabListItem
{
    public string FileName { get; set; }

    public string FilePath { get; set; }

    public bool IsSelected { get; set; }

    public string Guid { get; set; }

    public string DisplayFileName =>
        string.IsNullOrWhiteSpace(FilePath)
            ? string.Empty
            : (Path.GetFileName(FilePath) + "  " + "[" + Path.GetDirectoryName(FilePath) + "]");
}