using System.Text.RegularExpressions;

namespace Repotara.Templating;

/// <summary>
/// Extracts simple <c>{{ColumnName}}</c> substitution tags from a template
/// string. Deliberately supports substitution only -- no loops, no blocks.
/// </summary>
public static partial class TagParser
{
    [GeneratedRegex(@"\{\{\s*([^{}]+?)\s*\}\}")]
    private static partial Regex TagPattern();

    /// <summary>Returns every distinct tag name found in the template, in order of first appearance.</summary>
    public static List<string> ExtractTags(string template)
    {
        var seen = new HashSet<string>();
        var tags = new List<string>();

        foreach (Match match in TagPattern().Matches(template))
        {
            var tagName = match.Groups[1].Value;
            if (seen.Add(tagName))
            {
                tags.Add(tagName);
            }
        }

        return tags;
    }
}
