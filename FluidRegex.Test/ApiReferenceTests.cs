using System.Reflection;
using System.Text.RegularExpressions;

namespace FluidRegex.Test;

/// <summary>
/// docs/api-reference.md lists every public member, and only those. Under a "## TypeName" heading, each table row whose
/// first cell is in backticks is one member, written as its C# signature.
/// </summary>
[TestClass]
public class ApiReferenceTests
{
    private const string ReferenceFile = "api-reference.md";
    private const string TypeHeadingPrefix = "## ";

    private const BindingFlags DeclaredPublicMembers =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly Regex MemberRow = new(@"^\|\s*`([^`]+)`\s*\|");

    private static readonly Dictionary<Type, string> Keywords = new()
    {
        [typeof(string)] = "string",
        [typeof(char)] = "char",
        [typeof(int)] = "int",
    };

    private static readonly Type[] PublicTypes = typeof(TextPattern).Assembly.GetExportedTypes();

    [TestMethod]
    public void The_Reference_Lists_Every_Public_Member_And_Nothing_Else()
    {
        var documented = DocumentedMembers();
        var actual = PublicMembers();

        var missing = actual.Except(documented).OrderBy(member => member, StringComparer.Ordinal).ToArray();
        var notPublic = documented.Except(actual).OrderBy(member => member, StringComparer.Ordinal).ToArray();

        Assert.IsTrue(
            missing.Length == 0 && notPublic.Length == 0,
            "Add these to docs/api-reference.md:\n" + string.Join("\n", missing) +
            "\n\nThese are in docs/api-reference.md but are not public:\n" + string.Join("\n", notPublic));
    }

    [TestMethod]
    public void The_Reference_Examples_Emit_What_The_Reference_Says()
    {
        var start = new TextPattern();
        Assert.AreEqual(@"\p{L}+", start.Letter(Times.OneOrMore).ToString());
        Assert.AreEqual(@"\s+", start.Whitespace(Times.OneOrMore).ToString());
        Assert.AreEqual("(?:ab)+", start.Text("ab", Times.OneOrMore).ToString());
        Assert.AreEqual(@"(?<number>[0-9]+)", start.SaveTextAs("number", start.Digit(), Times.OneOrMore).ToString());
        Assert.AreEqual(@"(?<![^\r\n])(?!(?<=\r)\n)", start.StartOfLine().ToString());
        Assert.AreEqual(@"(?![^\r\n])(?!(?<=\r)\n)", start.EndOfLine().ToString());
        Assert.AreEqual("{4}", Times.Exactly(4).ToString());
        Assert.AreEqual("", default(Times).ToString());
        Assert.AreEqual("+?", Times.OneOrMore.AsFewAsPossible().ToString());

        var hostCharacters = new CharacterSet()
            .CharactersInRange('a', 'z').Digits()
            .Characters("-._");
        Assert.AreEqual("[-a-z0-9._]", hostCharacters.ToString());
    }

    private static HashSet<string> DocumentedMembers()
    {
        var publicTypeNames = new HashSet<string>(PublicTypes.Select(type => type.Name), StringComparer.Ordinal);
        var members = new HashSet<string>(StringComparer.Ordinal);
        string? typeName = null;

        foreach (var line in File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, ReferenceFile)))
        {
            if (line.StartsWith(TypeHeadingPrefix, StringComparison.Ordinal))
            {
                var heading = line.Substring(TypeHeadingPrefix.Length).Trim();
                typeName = publicTypeNames.Contains(heading) ? heading : null;
                continue;
            }

            var row = MemberRow.Match(line);
            if (typeName is not null && row.Success)
            {
                members.Add(typeName + ": " + row.Groups[1].Value);
            }
        }

        return members;
    }

    private static HashSet<string> PublicMembers()
    {
        var members = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in PublicTypes)
        {
            foreach (var constructor in type.GetConstructors())
            {
                members.Add(type.Name + ": " + type.Name + Parameters(constructor));
            }

            foreach (var property in type.GetProperties(DeclaredPublicMembers))
            {
                members.Add(type.Name + ": " + StaticPrefix(type, property.GetMethod!) + property.Name);
            }

            foreach (var method in type.GetMethods(DeclaredPublicMembers).Where(method => !method.IsSpecialName))
            {
                members.Add(type.Name + ": " + StaticPrefix(type, method) + method.Name + Parameters(method));
            }
        }

        return members;
    }

    private static string StaticPrefix(Type type, MethodInfo method) => method.IsStatic ? type.Name + "." : "";

    private static string Parameters(MethodBase method) =>
        "(" + string.Join(", ", method.GetParameters().Select(Parameter)) + ")";

    private static string Parameter(ParameterInfo parameter)
    {
        var isParams = parameter.IsDefined(typeof(ParamArrayAttribute), inherit: false);
        var text = (isParams ? "params " : "") + TypeName(parameter.ParameterType) + " " + parameter.Name;
        return parameter.IsOptional ? text + " = default" : text;
    }

    private static string TypeName(Type type)
    {
        if (type.IsArray)
        {
            return TypeName(type.GetElementType()!) + "[]";
        }

        return Keywords.TryGetValue(type, out var keyword) ? keyword : type.Name;
    }
}
