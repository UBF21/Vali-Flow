using System.Text.RegularExpressions;

namespace Vali_Flow.Core.RegularExpressions;

/// <summary>
/// Provides regular expressions for validating different formats.
/// </summary>
public class RegularExpression
{
    /// <summary>
    /// Regular expression for validating email addresses.
    /// </summary>
    /// <remarks>
    /// Accepts email addresses containing alphanumeric characters, dots, hyphens, and the '@' symbol, 
    /// followed by a valid domain.
    /// </remarks>
    public static readonly Regex FormatEmail = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
    
    /// <summary>
    /// Regular expression for validating Base64-encoded strings.
    /// </summary>
    /// <remarks>
    /// Ensures that the string contains only valid Base64 characters (A-Z, a-z, 0-9, +, /) 
    /// and optionally up to two padding '=' characters.
    /// </remarks>
    public static readonly Regex FormatBase64 = new("^[A-Za-z0-9+/]*={0,2}$");
}