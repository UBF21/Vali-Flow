using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Interfaces.Evaluators.Read;
using Vali_Flow.Interfaces.Evaluators.Write;

namespace Vali_Flow.Extensions;

/// <summary>
/// Extension methods to register <see cref="ValiFlowEvaluator{T}"/> in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ValiFlowEvaluator{T}"/> as a scoped service, resolving the required
    /// <see cref="DbContext"/> from the container automatically.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TContext">The <see cref="DbContext"/> type registered in the container.</typeparam>
    /// <param name="services">The service collection to add the evaluator to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services
    ///     .AddDbContext&lt;AppDbContext&gt;(...)
    ///     .AddValiFlowEvaluator&lt;Order, AppDbContext&gt;();
    ///
    /// // Then inject as:
    /// // IEvaluatorRead&lt;Order&gt; or IEvaluatorWrite&lt;Order&gt; or ValiFlowEvaluator&lt;Order&gt;
    /// </code>
    /// </example>
    public static IServiceCollection AddValiFlowEvaluator<T, TContext>(this IServiceCollection services)
        where T : class
        where TContext : DbContext
    {
        services.AddScoped<ValiFlowEvaluator<T>>(sp =>
        {
            var context = sp.GetRequiredService<TContext>();
            return new ValiFlowEvaluator<T>(context);
        });

        services.AddScoped<IEvaluatorRead<T>>(sp => sp.GetRequiredService<ValiFlowEvaluator<T>>());
        services.AddScoped<IEvaluatorWrite<T>>(sp => sp.GetRequiredService<ValiFlowEvaluator<T>>());

        return services;
    }
}
