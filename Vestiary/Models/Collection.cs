using System;
using System.Collections.Generic;

namespace Vestiary.Models;

[Serializable]
public class Collection
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> FolderPaths { get; set; } = new();
    public int Order { get; set; }

    public Collection()
    {
    }

    public Collection(string name, List<string> folderPaths, int order = 0)
    {
        Id = Guid.NewGuid();
        Name = name;
        FolderPaths = folderPaths ?? new();
        Order = order;
    }
}
