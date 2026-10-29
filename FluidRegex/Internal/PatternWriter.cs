using System.Text;

namespace FluidRegex.Internal;

/// <summary>Turns nodes into pattern text. All counts are written through <see cref="WriteQuantified"/>.</summary>
internal static class PatternWriter
{
    public static string Render(NodeList nodes)
    {
        var builder = new StringBuilder();
        foreach (var node in nodes.Items)
        {
            node.Write(builder);
        }

        return builder.ToString();
    }

    /// <summary>Writes a node with a count, wrapping it in (?:...) unless it is a single atom.</summary>
    public static void WriteQuantified(StringBuilder builder, RegexNode node, Times times)
    {
        if (times.IsOnce)
        {
            node.Write(builder);
            return;
        }

        if (node.IsAtom)
        {
            node.Write(builder);
        }
        else
        {
            builder.Append("(?:");
            node.Write(builder);
            builder.Append(')');
        }

        builder.Append(times.Suffix);
    }

    /// <summary>
    /// Writes a node that is about to be put inside parentheses (an uncounted capture or a lookaround). When the node
    /// is a lone either-or, its own (?:...) is dropped: (cat|dog) rather than ((?:cat|dog)).
    /// </summary>
    public static void WriteWithoutOuterAlternationGroup(StringBuilder builder, RegexNode node)
    {
        var single = node is SequenceNode { Nodes.Count: 1 } sequence ? sequence.Nodes.Items[0] : node;
        if (single is AlternationNode alternation)
        {
            alternation.WriteAlternatives(builder);
        }
        else
        {
            node.Write(builder);
        }
    }
}
