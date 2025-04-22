using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using vali_flow_test.DbConext;
using vali_flow_test.Models;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(databaseName: "TestDatabase")
    .LogTo(Console.WriteLine, LogLevel.Information) // Habilitar logging para ver las consultas SQL
    .Options;

await using var context = new AppDbContext(options);

await SeedData(context);

var request = new ListarModuloQuery
{
    Search = null,
    UbicacionId = Guid.Empty,
    ClasificacionId = Guid.Empty
};

await TestExpression(context, request);

static async Task SeedData(AppDbContext context)
{
    // Limpiar la base de datos
    context.Modulos.RemoveRange(context.Modulos);
    await context.SaveChangesAsync();

    // Agregar datos de prueba
    var modulos = new List<Modulo>
    {
        new Modulo
        {
            Id = Guid.NewGuid(),
            Nombre = "Modulo1",
            CustomerId = Guid.NewGuid(),
            UbicacionId = Guid.NewGuid(),
            ClasificacionId = Guid.NewGuid(),
            Deleted = null
        },
        new Modulo
        {
            Id = Guid.NewGuid(),
            Nombre = "Modulo2",
            CustomerId = Guid.NewGuid(),
            UbicacionId = Guid.NewGuid(),
            ClasificacionId = Guid.NewGuid(),
            Deleted = DateTime.Now // Marcado como eliminado
        },
        new Modulo()
        {
            Id = Guid.NewGuid(),
            Nombre = "TestModulo",
            CustomerId = Guid.NewGuid(),
            UbicacionId = Guid.NewGuid(),
            ClasificacionId = Guid.NewGuid(),
            Deleted = null
        }
    };

    context.Modulos.AddRange(modulos);
    await context.SaveChangesAsync();

    Console.WriteLine($"Datos de prueba agregados: {modulos.Count} módulos.");
}

static async Task TestExpression(AppDbContext context, ListarModuloQuery request)
{
    try
    {
        var builder = new ValiFlow<Modulo>();
        builder
            .Null(x => x.Deleted)
            .And()
            // .AddSubGroup(group =>
            //     group.Add(x => string.IsNullOrEmpty(request.Search))
            //         .Or()
            //         .Add(x => x.Nombre.ToLower().Contains(request.Search.ToLower())));
        .NullOrEmpty(x => request.Search)
        .Or()
        .Contains(x => x.Nombre,request.Search);
        // .And()
        // .Add(x => x.CustomerId.Equals(Guid.NewGuid()))
        // .And()
        // .Add(x => request.UbicacionId.Equals(Guid.Empty))
        // .Or()
        // .Add(x => x.UbicacionId.Equals(request.UbicacionId))
        // .And()
        // .Add(x => request.ClasificacionId.Equals(Guid.Empty))
        // .Or()
        // .Add(x => x.ClasificacionId.Equals(request.ClasificacionId));
        
        var dbcontext = new ValiFlowEvaluator<Modulo>(context);


        var especification = new QuerySpecification<Modulo>(builder);
        
        // Ejecutar la consulta
        var modulos = await dbcontext.EvaluateQueryAsync(especification);
        var data = await modulos.ToListAsync();

        Console.WriteLine($"Módulos encontrados: {data.Count}");
        foreach (var modulo in data)
        {
            Console.WriteLine($" - {modulo.Nombre} (Id: {modulo.Id})");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
        }
    }
}