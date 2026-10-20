namespace FluidRegex.Internal;

/// <summary>
/// An immutable, append-only list of nodes. Appending shares the existing list, so a builder can branch into
/// several chains without any of them seeing the others' pieces.
/// </summary>
internal sealed class NodeList
{
    public static readonly NodeList Empty = new(null, null, 0);

    private readonly NodeList? _previous;
    private readonly RegexNode? _last;
    private RegexNode[]? _items;

    private NodeList(NodeList? previous, RegexNode? last, int count)
    {
        _previous = previous;
        _last = last;
        Count = count;
    }

    public int Count { get; }

    /// <summary>The nodes in reading order. Built once, on first use.</summary>
    public RegexNode[] Items => _items ??= BuildItems();

    public NodeList Append(RegexNode node) => new(this, node, Count + 1);

    public NodeList AppendAll(NodeList nodes)
    {
        var result = this;
        foreach (var node in nodes.Items)
        {
            result = result.Append(node);
        }

        return result;
    }

    private RegexNode[] BuildItems()
    {
        var items = new RegexNode[Count];
        var current = this;
        for (var i = Count - 1; i >= 0; i--)
        {
            items[i] = current!._last!;
            current = current._previous;
        }

        return items;
    }
}
