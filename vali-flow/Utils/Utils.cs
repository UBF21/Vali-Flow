using System.Reflection;

namespace vali_flow.Utils;

public static class Utils
{
    public static string? GetCurrentMethodName()
    {
        return MethodBase.GetCurrentMethod()?.Name;
    }
}