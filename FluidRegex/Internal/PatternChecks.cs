using System.Text.RegularExpressions;

namespace FluidRegex.Internal;

/// <summary>
/// Checks ToRegex() runs on the finished pattern, for mistakes .NET accepts silently.
/// Everything .NET reports itself (undefined groups, RawRegex syntax errors) is left to the Regex constructor.
/// </summary>
internal static class PatternChecks
{
    public static void Check(NodeList nodes, RegexOptions options)
    {
        var walk = new Walk();
        foreach (var node in nodes.Items)
        {
            walk.Visit(node, insideLookbehind: false);
        }

        // A reference that comes before every capture of that name can never match.
        foreach (var name in walk.ReferencedBeforeCapture)
        {
            if (walk.CaptureNames.Contains(name))
            {
                throw new ArgumentException(
                    $"The pattern uses TheSameTextAsCapture(\"{name}\") before CaptureAs(\"{name}\", ...). " +
                    "A reference only matches text that has already been captured, so it must come after the capture.");
            }
        }

        if ((options & RegexOptions.ExplicitCapture) != 0)
        {
            if (walk.HasUnnamedCapture)
            {
                throw new ArgumentException(
                    "RegexOptions.ExplicitCapture stops Capture(...) from capturing anything. Use CaptureAs(name, ...) or remove the option.");
            }

            if (walk.HasNumberedReference)
            {
                throw new ArgumentException(
                    "RegexOptions.ExplicitCapture leaves no numbered captures for TheSameTextAsCapture(number) to refer to. Refer to the capture by name instead.");
            }
        }
    }

    private sealed class Walk
    {
        public HashSet<string> CaptureNames { get; } = new(StringComparer.Ordinal);

        public HashSet<string> ReferencedBeforeCapture { get; } = new(StringComparer.Ordinal);

        public bool HasUnnamedCapture { get; private set; }

        public bool HasNumberedReference { get; private set; }

        public void Visit(RegexNode node, bool insideLookbehind)
        {
            switch (node)
            {
                case CaptureNode capture:
                    if (capture.Name is null)
                    {
                        HasUnnamedCapture = true;
                    }
                    else
                    {
                        // Counted as seen on entry, so a reference inside its own capture isn't flagged.
                        CaptureNames.Add(capture.Name);
                    }

                    break;
                case BackreferenceNode reference:
                    if (reference.Name is null)
                    {
                        HasNumberedReference = true;
                    }
                    else if (!insideLookbehind && !CaptureNames.Contains(reference.Name))
                    {
                        // Lookbehinds run right to left, so reading order says nothing about them.
                        ReferencedBeforeCapture.Add(reference.Name);
                    }

                    break;
                case LookaroundNode { IsBehind: true }:
                    insideLookbehind = true;
                    break;
            }

            foreach (var child in node.Children)
            {
                Visit(child, insideLookbehind);
            }
        }
    }
}
