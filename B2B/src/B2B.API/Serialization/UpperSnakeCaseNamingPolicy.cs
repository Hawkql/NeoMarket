using System.Text;
using System.Text.Json;

namespace B2B.Api.Serialization;

public sealed class UpperSnakeCaseNamingPolicy : JsonNamingPolicy
{
    public static readonly UpperSnakeCaseNamingPolicy Instance = new();

    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var sb = new StringBuilder(name.Length + 5);
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0)
                sb.Append('_');
            sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }
}