namespace Tzkt.Api.Utils
{
    public class JsonPath
    {
        public JsonPathType Type { get; }
        public string? Value { get; }

        public JsonPath(string value)
        {
            if (Regexes.Field().IsMatch(value))
            {
                Type = JsonPathType.Field;
                Value = value;
            }
            else if (Regexes.Quoted().IsMatch(value))
            {
                Type = JsonPathType.Key;
                Value = value[1..^1];
            }
            else if (Regexes.ArrayIndex().IsMatch(value))
            {
                Type = JsonPathType.Index;
                Value = value[1..^1];
            }
            else if (value == "[*]")
            {
                Type = JsonPathType.Any;
                Value = null;
            }
            else
            {
                Type = JsonPathType.None;
                Value = value;
            }
        }

        public static bool TryParse(string path, out JsonPath[] res)
        {
            res = (path.Contains('"')
                ? Regexes.JsonPathParser().Matches(path).Select(x => x.Value)
                : path.Split("."))
                .Select(x => new JsonPath(x))
                .ToArray();

            return res.All(x => x.Type != JsonPathType.None);
        }

        public static string Merge(JsonPath[] path, string value, int ind = 0)
        {
            if (ind == path.Length)
                return value;

            if (path[ind].Type > JsonPathType.Key)
                return $"[{Merge(path, value, ++ind)}]";

            return $"{{\"{path[ind].Value}\":{Merge(path, value, ++ind)}}}";
        }

        public static string?[] Select(JsonPath[] path)
        {
            return path.Select(x => x.Value).ToArray();
        }
    }

    public enum JsonPathType
    {
        None,
        Field,
        Key,
        Index,
        Any
    }
}
