using System.Reflection;

namespace Vali_Flow.Core.Utils;

public static class UtilHelper
{
    public static string? GetCurrentMethodName()
    {
        return MethodBase.GetCurrentMethod()?.Name;
    }
}