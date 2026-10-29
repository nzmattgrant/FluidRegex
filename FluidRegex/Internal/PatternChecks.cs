namespace FluidRegex.Internal;

/// <summary>
/// Checks ToRegex() runs on the finished pattern, for mistakes .NET accepts silently or reports in regex jargon.
/// Everything else (RawRegex syntax errors) is left to the Regex constructor.
/// </summary>
internal static class PatternChecks
{
    public static void Check(NodeList nodes)
    {
        var walk = new Walk();
        foreach (var node in nodes.Items)
        {
            walk.Visit(node, insideLookbehind: false);
        }

        // RawRegex can save a name the walk can't see, so leave unknown names to .NET then.
        if (!walk.HasRawRegex)
        {
            foreach (var name in walk.ReferencedNames)
            {
                if (!walk.SavedNames.Contains(name))
                {
                    throw new ArgumentException(
                        $"The pattern uses SameTextAsSaved(\"{name}\"), but nothing is saved as \"{name}\". " +
                        $"Add SaveTextAs(\"{name}\", ...) before it.");
                }
            }
        }

        // A reference that comes before every capture of that name can never match.
        foreach (var name in walk.ReferencedBeforeSave)
        {
            if (walk.SavedNames.Contains(name))
            {
                throw new ArgumentException(
                    $"The pattern uses SameTextAsSaved(\"{name}\") before SaveTextAs(\"{name}\", ...). " +
                    "It can only repeat text that was already saved, so it must come after SaveTextAs.");
            }
        }
    }

    private sealed class Walk
    {
        public HashSet<string> SavedNames { get; } = new(StringComparer.Ordinal);

        public HashSet<string> ReferencedNames { get; } = new(StringComparer.Ordinal);

        public HashSet<string> ReferencedBeforeSave { get; } = new(StringComparer.Ordinal);

        public bool HasRawRegex { get; private set; }

        public void Visit(RegexNode node, bool insideLookbehind)
        {
            switch (node)
            {
                case CaptureNode capture:
                    // Counted as seen on entry, so a reference inside its own capture isn't flagged.
                    SavedNames.Add(capture.Name);
                    break;
                case BackreferenceNode reference:
                    ReferencedNames.Add(reference.Name);

                    // Lookbehinds run right to left, so reading order says nothing about them.
                    if (!insideLookbehind && !SavedNames.Contains(reference.Name))
                    {
                        ReferencedBeforeSave.Add(reference.Name);
                    }

                    break;
                case LookaroundNode { IsBehind: true }:
                    insideLookbehind = true;
                    break;
                case RawNode:
                    HasRawRegex = true;
                    break;
            }

            foreach (var child in node.Children)
            {
                Visit(child, insideLookbehind);
            }
        }
    }
}
