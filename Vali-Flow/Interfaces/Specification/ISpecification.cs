using System.Linq.Expressions;
using Vali_Flow.Core.Builder;

namespace Vali_Flow.Interfaces.Specification;

public interface ISpecification<T>
{
    ValiFlow<T> Filter { get; }
    IEnumerable<IIncludeExpression<T>>? Includes { get; }
    bool AsNoTracking { get; }
    bool AsSplitQuery { get; }  
}