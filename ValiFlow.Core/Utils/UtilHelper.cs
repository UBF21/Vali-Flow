using System.Reflection;

namespace ValiFlow.Core.Utils;

public static class UtilHelper
{
    public static string? GetCurrentMethodName()
    {
        return MethodBase.GetCurrentMethod()?.Name;
    }
}