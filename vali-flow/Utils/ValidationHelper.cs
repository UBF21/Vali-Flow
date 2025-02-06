using System.Text.Json;

namespace vali_flow.Utils;

public static class ValidationHelper
{
    public static bool IsValidJson(string? val)
    {
        if (string.IsNullOrWhiteSpace(val))
            return false;

        try
        {
            JsonDocument.Parse(val);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}