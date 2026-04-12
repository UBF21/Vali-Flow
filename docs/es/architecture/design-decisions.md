# Vali-Flow — Decisiones de diseño

Este documento registra las decisiones arquitectónicas significativas tomadas en el ecosistema Vali-Flow, el razonamiento detrás de cada una y los trade-offs aceptados.

---

## 1. Árboles de expresión como DSL, no cadenas

**Decisión:** Usar árboles de expresión C# (`Expression<Func<T, bool>>`) como representación del filtro en lugar de un lenguaje de consulta basado en cadenas o un diccionario de condiciones.

**Razonamiento:**
- Seguridad de tipos en tiempo de compilación: el compilador detecta nombres de propiedades mal escritos y discrepancias de tipos en tiempo de build, no en runtime.
- Soporte de refactorización: renombrar una propiedad se propaga correctamente en los IDEs.
- Componible: las condiciones de `ValiFlow<T>` son combinables con `.And()`, `.Or()`, `.Not()` sin concatenación de cadenas.

**Trade-off:** Los árboles de expresión son más complejos de inspeccionar y serializar que las cadenas. La capa IR (ver §3) existe específicamente para cerrar esta brecha para los backends NoSQL.

---

## 2. Patrón de especificación para EF Core

**Decisión:** Los criterios de consulta se expresan como objetos `BasicSpecification<T>` y `QuerySpecification<T>` en lugar de pasar expresiones LINQ crudas directamente a los métodos del evaluador.

**Razonamiento:**
- Desacopla "qué consultar" de "cómo ejecutarlo". Una spec puede construirse en un servicio de dominio y pasarse a un evaluador de infraestructura.
- Lleva no solo el filtro sino también includes, ordenamiento, paginación y hints de EF — un único objeto de traspaso.
- Testeable: las specs son objetos planos que pueden instanciarse en tests unitarios sin un DbContext.

**Trade-off:** Una capa de abstracción más. Para consultas simples, construir una spec completa puede sentirse verboso. El evaluador InMemory acepta instancias `ValiFlow<T>` crudas directamente para facilitar esto.

---

## 3. Representación Intermedia (IR) para adaptadores NoSQL

**Decisión:** Los adaptadores NoSQL no recorren el árbol de expresiones crudo directamente — primero lo convierten a un árbol de nodos IR simples (records sellados: `EqualNode`, `ComparisonNode`, `LikeNode`, `InNode`, `NullNode`, `AndNode`, `OrNode`, `NotNode`) usando `ExpressionToIRVisitor`.

**Razonamiento:**
- Los árboles de expresión contienen artefactos del compilador C# (closures, nullables elevados, constant folding) que son difíciles de mapear 1:1 a lenguajes de consulta NoSQL.
- Los nodos IR son simples y bien definidos — los adaptadores traducen IR, no árboles de expresión crudos, haciendo que cada adaptador tenga 100–200 líneas en lugar de 500+.
- Los nodos IR son records sellados inmutables — seguros para cachear y compartir.

**Trade-off:** Una traducción en dos pasos (expresión → IR → consulta nativa) agrega un pase de visita. El overhead es negligible en runtime (el árbol IR es pequeño) pero agrega una capa conceptual para los contribuidores.

---

## 4. Sin preocupación de ejecución en los traductores

**Decisión:** `ValiFlow<T>`, el traductor SQL y todos los adaptadores NoSQL son transformadores puros. No mantienen conexiones, no ejecutan consultas ni gestionan estado.

**Razonamiento:**
- Responsabilidad única: un traductor traduce; un driver ejecuta.
- Testeable de forma aislada: el traductor completo puede testearse asertando sobre la cadena/documento de salida sin una base de datos real.
- Componible: la salida del traductor puede combinarse con fragmentos escritos manualmente.

**Trade-off:** Los usuarios deben conectar la salida a un driver por su cuenta (unas pocas líneas de código). Esto se elige explícitamente sobre wrappers de conveniencia que crearían acoplamiento fuerte a una versión específica del driver.

---

## 5. Objetos de dialecto, no flags

**Decisión:** Las diferencias de dialecto SQL (comillas, prefijo de parámetros, operadores insensibles a mayúsculas) se codifican en implementaciones de `ISqlDialect` en lugar de en un enum de dialecto o un diccionario de configuración.

**Razonamiento:**
- Principio Abierto/Cerrado: agregar un nuevo dialecto requiere solo una nueva clase, sin cambios en el traductor.
- Testeable: cada dialecto es un objeto plano que puede instanciarse en tests.
- Componible: las aplicaciones pueden crear dialectos personalizados para entornos inusuales.

**Dialectos incorporados:** `SqlServerDialect`, `PostgreSqlDialect`, `MySqlDialect`, `SqliteDialect`, `OracleDialect`.

---

## 6. Evaluador InMemory sincrónico

**Decisión:** `Vali-Flow.InMemory` es completamente sincrónico — sin tipos de retorno `Task<T>` en ningún método.

**Razonamiento:**
- Las operaciones en memoria son CPU-bound y se completan instantáneamente. Envolverlas en `Task.FromResult` agrega ruido sin valor.
- El código de test se lee más naturalmente sin `await` en todas partes.
- Coincide con los nombres de métodos del evaluador EF Core para que los mismos objetos de especificación funcionen en ambos contextos.

**Trade-off:** El código de producción que llama al evaluador InMemory a través de una abstracción debe exponer métodos sincrónicos o sobrecargados. Para interfaces solo-async, los llamadores envuelven con `Task.FromResult`.

---

## 7. Parámetro `negateCondition` en lugar de un wrapper `.Not()`

**Decisión:** Los métodos de lectura del evaluador aceptan un parámetro `bool negateCondition` que invierte el filtro, en lugar de requerir que los llamadores envuelvan el filtro en una llamada `ValiFlow<T>.Not()`.

**Razonamiento:**
- Conveniencia: consultar registros "fallidos" (los que no coinciden con una spec) es un caso de uso común. `negateCondition: true` cuesta un flip de parámetro en lugar de construir una nueva spec.
- Evita duplicación de specs: el mismo objeto spec se reutiliza para consultas que pasan y que fallan.

**Trade-off:** Un parámetro booleano es menos descubrible que un método nombrado. `EvaluateQueryFailedAsync` y `EvaluateGetFirstFailedAsync` son wrappers de conveniencia que establecen `negateCondition: true` internamente para los casos más comunes.

---

## 8. Operaciones bulk mediante EFCore.BulkExtensions

**Decisión:** `BulkInsertAsync`, `BulkUpdateAsync`, `BulkDeleteAsync` y `BulkInsertOrUpdateAsync` delegan a `EFCore.BulkExtensions` en lugar de implementar un mecanismo bulk personalizado.

**Razonamiento:**
- EFCore.BulkExtensions es el estándar de facto para operaciones bulk en EF Core, battle-tested en SQL Server, PostgreSQL, SQLite y MySQL.
- Implementar correctamente operaciones bulk (batching, identity output, columnas generadas) para múltiples bases de datos es una superficie grande que no es la preocupación de Vali-Flow.
- Los llamadores pueden pasar un `BulkConfig` para controlar todos los ajustes de EFCore.BulkExtensions.

**Trade-off:** Toma una dependencia en EFCore.BulkExtensions. Las aplicaciones que no usan operaciones bulk igualmente incluyen la dependencia.

---

## 9. `ValiSort<T>` para ordenamiento dinámico

**Decisión:** El ordenamiento dinámico (ordenar por un nombre de propiedad conocido solo en runtime) se provee por `ValiSort<T>` de Core en lugar de por helpers LINQ OrderBy basados en reflexión.

**Razonamiento:**
- Centraliza la lógica de ordenamiento en Core donde puede ser usada por todos los paquetes.
- `ValiSort<T>` compila el selector de clave de ordenamiento una vez, evitando llamadas repetidas de reflexión.

**Trade-off:** El ordenamiento dinámico por una cadena en runtime requiere que la propiedad esté mapeada en la configuración de ordenamiento. Las propiedades no mapeadas lanzan en runtime — esto es por diseño para detectar configuraciones incorrectas tempranamente.

---

## 10. ProjectReference local a Core durante el desarrollo

**Decisión:** Todos los paquetes referencian `Vali-Flow.Core` mediante un `ProjectReference` local en lugar de una referencia NuGet durante el desarrollo activo.

**Razonamiento:**
- Core y los paquetes se iteran juntos. Una referencia local permite que los cambios a Core se incorporen inmediatamente sin publicar.
- Evita desfase de versiones entre Core y los paquetes durante los ciclos de desarrollo.

**Ruta de migración:** Una vez que Core v2.6.0 se publique en NuGet, todas las entradas `ProjectReference` serán reemplazadas por `<PackageReference Include="Vali-Flow.Core" Version="2.6.0" />`.

---

## Ver también

- [Visión general de la arquitectura](overview.md)
- [Guía de contribución](../internal/contributing.md)
