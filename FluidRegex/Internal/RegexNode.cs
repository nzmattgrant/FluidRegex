using System.Text;

namespace FluidRegex.Internal;

/// <summary>One piece of a pattern. Builders hold an immutable list of nodes and write them out on ToString().</summary>
internal abstract class RegexNode
{
    /// <summary>True when a count can follow this node directly, without wrapping it in (?:...).</summary>
    public abstract bool IsAtom { get; }

    /// <summary>True when the node matches a position (an anchor or a lookaround), not characters.</summary>
    public virtual bool IsZeroWidth => false;

    /// <summary>The nodes inside this one, in reading order. Used by the checks ToRegex() runs.</summary>
    public virtual IEnumerable<RegexNode> Children => [];

    public abstract void Write(StringBuilder builder);
}

/// <summary>Literal text, always escaped. Only a single character can take a count without a wrapper.</summary>
internal sealed class TextNode(string text) : RegexNode
{
    public override bool IsAtom => text.Length == 1;

    public override void Write(StringBuilder builder) => builder.Append(Escaping.OutsideSet(text));
}

/// <summary>A ready-made regex token such as \d, \p{L}, . or the line-break group.</summary>
internal sealed class TokenNode(string token) : RegexNode
{
    public override bool IsAtom => true;

    public override void Write(StringBuilder builder) => builder.Append(token);
}

/// <summary>A character set: [...] or [^...].</summary>
internal sealed class SetNode(CharacterSet set, bool negated) : RegexNode
{
    public override bool IsAtom => true;

    public override void Write(StringBuilder builder) =>
        builder.Append(negated ? "[^" : "[").Append(set.Contents).Append(']');
}

/// <summary>A position such as \A, \z or \b. Anchors never take a count.</summary>
internal sealed class AnchorNode(string token) : RegexNode
{
    public override bool IsAtom => true;

    public override bool IsZeroWidth => true;

    public override void Write(StringBuilder builder) => builder.Append(token);
}

/// <summary>A condition on the surrounding text: (?=...), (?!...), (?&lt;=...) or (?&lt;!...).</summary>
internal sealed class LookaroundNode(bool isBehind, bool isNegative, RegexNode content) : RegexNode
{
    public bool IsBehind => isBehind;

    public override bool IsAtom => true;

    public override bool IsZeroWidth => true;

    public override IEnumerable<RegexNode> Children => [content];

    public override void Write(StringBuilder builder)
    {
        builder.Append(isBehind ? "(?<" : "(?").Append(isNegative ? '!' : '=');
        PatternWriter.WriteWithoutOuterAlternationGroup(builder, content);
        builder.Append(')');
    }
}

/// <summary>Several nodes one after another, such as the contents of a reused TextPattern.</summary>
internal sealed class SequenceNode(NodeList nodes) : RegexNode
{
    public NodeList Nodes => nodes;

    public override bool IsAtom => nodes.Count == 1 && nodes.Items[0].IsAtom;

    public override bool IsZeroWidth
    {
        get
        {
            foreach (var node in nodes.Items)
            {
                if (!node.IsZeroWidth)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public override IEnumerable<RegexNode> Children => nodes.Items;

    public override void Write(StringBuilder builder)
    {
        foreach (var node in nodes.Items)
        {
            node.Write(builder);
        }
    }
}

/// <summary>A node followed by a count. Anything that isn't an atom is wrapped in (?:...) first.</summary>
internal sealed class QuantifiedNode(RegexNode inner, Times times) : RegexNode
{
    // Never an atom, so a second count always wraps: (?:\d+)* and never \d+*.
    public override bool IsAtom => false;

    public override bool IsZeroWidth => inner.IsZeroWidth;

    public override IEnumerable<RegexNode> Children => [inner];

    public override void Write(StringBuilder builder) => PatternWriter.WriteQuantified(builder, inner, times);
}

/// <summary>
/// A named capture (?&lt;name&gt;...), written by SaveTextAs. The count goes inside so the capture holds the whole run;
/// when the count allows zero, the optional part moves outside so a missing capture reports Success == false.
/// </summary>
internal sealed class CaptureNode(string name, RegexNode content, Times times) : RegexNode
{
    public string Name => name;

    public override bool IsAtom => true;

    public override bool IsZeroWidth => content.IsZeroWidth;

    public override IEnumerable<RegexNode> Children => [content];

    public override void Write(StringBuilder builder)
    {
        var inner = times.WithMinimumOfOne();
        WriteCapture(builder, inner);

        if (times.Minimum == 0)
        {
            builder.Append(times.IsAsFewAsPossible ? "??" : "?");
        }
    }

    private void WriteCapture(StringBuilder builder, Times inner)
    {
        builder.Append("(?<").Append(name).Append('>');
        if (inner.IsOnce)
        {
            PatternWriter.WriteWithoutOuterAlternationGroup(builder, content);
        }
        else
        {
            PatternWriter.WriteQuantified(builder, content, inner);
        }

        builder.Append(')');
    }
}

/// <summary>Either-or, always grouped so that '|' can never reach the pieces around it: (?:a|b).</summary>
internal sealed class AlternationNode(RegexNode[] alternatives) : RegexNode
{
    public override bool IsAtom => true;

    public override bool IsZeroWidth
    {
        get
        {
            foreach (var alternative in alternatives)
            {
                if (!alternative.IsZeroWidth)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public override IEnumerable<RegexNode> Children => alternatives;

    public override void Write(StringBuilder builder)
    {
        builder.Append("(?:");
        WriteAlternatives(builder);
        builder.Append(')');
    }

    /// <summary>Writes a|b without the surrounding group, for places that already supply parentheses.</summary>
    public void WriteAlternatives(StringBuilder builder)
    {
        for (var i = 0; i < alternatives.Length; i++)
        {
            if (i > 0)
            {
                builder.Append('|');
            }

            alternatives[i].Write(builder);
        }
    }
}

/// <summary>The same text as an earlier named capture, written by SameTextAsSaved: \k&lt;name&gt;.</summary>
internal sealed class BackreferenceNode(string name) : RegexNode
{
    public string Name => name;

    public override bool IsAtom => true;

    public override void Write(StringBuilder builder) => builder.Append(@"\k<").Append(name).Append('>');
}

/// <summary>A reused pattern that ignores case, written (?i:...) so the setting covers only that pattern.</summary>
internal sealed class IgnoreCaseNode(RegexNode content) : RegexNode
{
    public override bool IsAtom => true;

    public override bool IsZeroWidth => content.IsZeroWidth;

    public override IEnumerable<RegexNode> Children => [content];

    public override void Write(StringBuilder builder)
    {
        builder.Append("(?i:");
        PatternWriter.WriteWithoutOuterAlternationGroup(builder, content);
        builder.Append(')');
    }
}

/// <summary>Raw regex text, always wrapped in (?:...) so a '|' or an inline option can't leak into the rest.</summary>
internal sealed class RawNode(string pattern) : RegexNode
{
    public override bool IsAtom => true;

    public override void Write(StringBuilder builder) => builder.Append("(?:").Append(pattern).Append(')');
}
