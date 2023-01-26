using System.Text.RegularExpressions;

namespace Sicoob.Visualizer.Monitor.Comuns;
public static class Helper
{
    private const string userKey = "user";
    private const string emailKey = "email";
    private const string fileKey = "file";
    public static Tuple<string?, string?, string?> GetSearch(this string? text)
    {
        string?
            userName = null,
            userEmail = null,
            fileName = null;

        var dic = text.ToDictionary();

        if (dic.ContainsKey(userKey))
            userName = dic[userKey];

        if (dic.ContainsKey(emailKey))
            userEmail = dic[emailKey];

        if (dic.ContainsKey(fileKey))
            fileName = dic[fileKey];

        return (userName, userEmail, fileName).ToTuple();
    }

    public static Dictionary<string, string> ToDictionary(this string? text)
    {
        text ??= string.Empty;

        string[] words = Expressions.SeparetedBySpaces().Split(text);

        var dic = words
            .Where(x => !string.IsNullOrEmpty(x) && !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Split(':'))
            .Where(x => x.Length > 0)
            .GroupBy(x => x[0], x => x.Length > 1 ? x[1] : x[0])
            .ToDictionary(x => x.Key, x => x.First());

        foreach (var item in dic.Keys)
        {
            string value = dic[item];

            if (value.Length > 1 &&
                value.StartsWith('"') &&
                value.EndsWith('"'))
                value = value.Remove(0, 1)
                    .Remove(value.Length - 2, 1);

            dic[item] = value;
        }

        return dic;
    }
}
public partial class Expressions
{
    [GeneratedRegex("(?<=\")\\s")]
    public static partial Regex SeparetedBySpaces();

    [GeneratedRegex("(?<!\")(?<=:\")[^\"]*(?=\")|(?<=\")[^\"]*(?=\")|[^:]+(?=:|$)")]
    public static partial Regex SeparetedByDoublePoint();
}