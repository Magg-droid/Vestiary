using System;

namespace Wardrobe.Models;

[Serializable]
public class DesignMetadata
{
    public Guid DesignId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string CustomImagePath { get; set; } = string.Empty;
    public int Order { get; set; }

    public DesignMetadata()
    {
    }

    public DesignMetadata(Guid designId, string nickname = "", string customImagePath = "", int order = 0)
    {
        DesignId = designId;
        Nickname = nickname;
        CustomImagePath = customImagePath;
        Order = order;
    }
}
