using System.Reflection;

namespace vali_flow.Utils;

public static class UtilHelper
{
    public static string? GetCurrentMethodName()
    {
        return MethodBase.GetCurrentMethod()?.Name;
    }
}