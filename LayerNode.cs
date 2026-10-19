using System.Collections.Generic;

namespace PixelStudio;

public enum NodeType { Layer, Folder }

public class LayerNode
{
    public NodeType Type { get; set; }
    public string Name { get; set; }
    public LayerNode? Parent { get; set; }
    public List<LayerNode> Children { get; } = new();
    public Layer? Layer { get; set; }
    public bool Visible { get; set; } = true;
    public bool IsExpanded { get; set; } = true;

    public LayerNode(NodeType type, string name)
    {
        Type = type;
        Name = name;
    }

    public bool IsLayer => Type == NodeType.Layer;
    public bool IsFolder => Type == NodeType.Folder;

    public void Detach()
    {
        Parent?.Children.Remove(this);
        Parent = null;
    }

    public void AttachTo(LayerNode newParent, int index = -1)
    {
        Detach();
        Parent = newParent;
        if (index < 0 || index > newParent.Children.Count)
            newParent.Children.Add(this);
        else
            newParent.Children.Insert(index, this);
    }
}