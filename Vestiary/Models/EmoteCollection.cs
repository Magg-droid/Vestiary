using System;

namespace Vestiary.Models;

[Serializable]
public class EmoteCollection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }

    public EmoteCollection()
    {
    }

    public EmoteCollection(string name, int order = 0)
    {
        Id = Guid.NewGuid();
        Name = name;
        Order = order;
    }
}