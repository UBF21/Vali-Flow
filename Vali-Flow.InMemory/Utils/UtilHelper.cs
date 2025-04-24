using System.Reflection;

namespace Vali_Flow.InMemory.Utils;

public class UtilHelper
{
    public static string? GetCurrentMethodName()
    {
        return MethodBase.GetCurrentMethod()?.Name;
    }
}