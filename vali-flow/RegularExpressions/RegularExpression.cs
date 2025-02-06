using System.Text.RegularExpressions;

namespace vali_flow.RegularExpressions;

public static class RegularExpression
{
    public static readonly Regex FormatEmail = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
    public static readonly Regex FormatBase64 = new Regex("^[A-Za-z0-9+/]*={0,2}$");
}