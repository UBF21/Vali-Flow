using System.Linq.Expressions;

namespace vali_flow.Interfaces.Types;

/// <summary>
/// Define las operaciones para construir, agregar condiciones y evaluar expresiones lógicas para una entidad de tipo <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="TBuilder">Tipo del builder específico que implementa esta interfaz.</typeparam>
/// <typeparam name="T">Tipo de la entidad que se está evaluando.</typeparam>
public interface IExpression<out TBuilder,T>
{
    /// <summary>
    /// Construye una expresión booleana a partir de las condiciones añadidas.
    /// </summary>
    /// <returns>Una expresión booleana que representa la evaluación de las condiciones agregadas.</returns>
    /// <example>
    /// <code>
    /// var builder = new ConditionBuilder();
    /// builder.Add(x => x.Age > 18).Add(x => x.Name == "John");
    /// var expression = builder.Build(); // La expresión resultante representará la condición (Age > 18) AND (Name == "John")
    /// </code>
    /// </example>
    Expression<Func<T, bool>> Build();

    /// <summary>
    /// Agrega una condición a la lista de condiciones, basándose en una expresión booleana.
    /// </summary>
    /// <param name="condition">La condición a agregar representada por una expresión booleana.</param>
    /// <returns>El builder actual para permitir el encadenamiento de métodos.</returns>
    /// <exception cref="ArgumentNullException">Lanza una excepción si la condición es nula.</exception>
    /// <example>
    /// <code>
    /// var builder = new ConditionBuilder();
    /// builder.Add(x => x.Age > 18); // Agrega una condición que verifica si la edad es mayor que 18
    /// </code>
    /// </example>
    TBuilder Add(Expression<Func<T, bool>> condition);

    /// <summary>
    /// Agrega una condición a la lista de condiciones basándose en una propiedad específica de la entidad de tipo <typeparamref name="T"/>
    /// y un predicado que evalúa un valor de tipo <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">El tipo del valor a evaluar.</typeparam>
    /// <param name="selector">Una expresión que selecciona la propiedad o valor de tipo <typeparamref name="TValue"/> de la entidad de tipo <typeparamref name="T"/>.</param>
    /// <param name="predicate">Una expresión booleana que evalúa el valor seleccionado por el <paramref name="selector"/>.</param>
    /// <returns>El builder actual para permitir el encadenamiento de métodos.</returns>
    /// <exception cref="ArgumentNullException">Lanza una excepción si cualquiera de los parámetros es nulo.</exception>
    /// <example>
    /// <code>
    /// builder.Add(x => x.Age, age => age >= 18 );
    /// </code>
    /// </example>
    TBuilder Add<TValue>(Expression<Func<T, TValue>> selector, Expression<Func<TValue, bool>> predicate);

    /// <summary>
    /// Agrega un grupo de condiciones que se evaluarán como una sub-expresión. 
    /// Este grupo se puede agregar con otras condiciones lógicas, como AND u OR.
    /// </summary>
    /// <param name="groupBuilder">La acción que construye el grupo de condiciones.</param>
    /// <returns>El builder actual para permitir el encadenamiento de métodos.</returns>
    /// <example>
    /// <code>
    /// var builder = new ConditionBuilder();
    /// builder.AddSubGroup(group => group.Add(x => x.Age > 18).Add(x => x.Name == "John"));
    /// // Este ejemplo agrega un subgrupo con las condiciones (Age > 18) AND (Name == "John")
    /// </code>
    /// </example>
    TBuilder AddSubGroup(Action<TBuilder> groupBuilder);

    /// <summary>
    /// Evalúa una condición específica sobre un objeto de tipo <typeparamref name="T"/>.
    /// </summary>
    /// <param name="obj">El objeto a evaluar.</param>
    /// <returns>El valor booleano resultante de la evaluación de las condiciones.</returns>
    /// <exception cref="InvalidOperationException">Lanza una excepción si hay un error al evaluar las condiciones.</exception>
    /// <example>
    /// <code>
    /// var builder = new ConditionBuilder();
    /// builder.Add(x => x.Age > 18);
    /// bool result = builder.Evaluate(new Person { Age = 25 }); // Devuelve true
    /// </code>
    /// </example>
    public bool Evaluate(T obj);

    /// <summary>
    /// Evalúa una condición sobre cada elemento de una colección de objetos de tipo <typeparamref name="T"/>.
    /// </summary>
    /// <param name="collection">La colección de elementos a evaluar.</param>
    /// <returns>Una lista de elementos de la colección que cumplen con las condiciones especificadas.</returns>
    /// <exception cref="ArgumentException">Lanza una excepción si la colección es nula o vacía.</exception>
    /// <example>
    /// <code>
    /// var builder = new ConditionBuilder();
    /// builder.Add(x => x.Age > 18);
    /// var result = builder.EvaluateAll(new List() { new Person { Age = 25 }, new Person { Age = 15 } });
    /// // Devuelve una lista que solo contiene la persona con edad 25
    /// </code>
    /// </example>
    public IEnumerable<T> EvaluateAll(IEnumerable<T> collection);

    /// <summary>
    /// Define una operación lógica "AND" entre las condiciones.
    /// </summary>
    /// <returns>El builder actual para permitir el encadenamiento de métodos.</returns>
    /// <example>
    /// <code>
    /// var builder = new ConditionBuilder();
    /// builder.Add(x => x.Age > 18).And().Add(x => x.Name == "John");
    /// // La expresión resultante será (Age > 18) AND (Name == "John")
    /// </code>
    /// </example>
    TBuilder And();

    /// <summary>
    /// Define una operación lógica "OR" entre las condiciones.
    /// </summary>
    /// <returns>El builder actual para permitir el encadenamiento de métodos.</returns>
    /// <example>
    /// <code>
    /// var builder = new ConditionBuilder();
    /// builder.Add(x => x.Age > 18).Or().Add(x => x.Name == "John");
    /// // La expresión resultante será (Age > 18) OR (Name == "John")
    /// </code>
    /// </example>
    TBuilder Or();
}