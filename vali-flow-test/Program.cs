// See https://aka.ms/new-console-template for more information

using vali_flow_test.Models;
using vali_flow.Builder;

List<User> users = new List<User>
{
    new User { Name = "Alice", IsActive = true, Age = 25 },
    new User { Name = "Bob", IsActive = false, Age = 17 },
    new User { Name = "", IsActive = true, Age = 30 },
    new User { Name = "Charlie", IsActive = true, Age = 20 }
};

var validator = new ValiFlow<User>()
    .NotNull(x => x.Name)
    .IsTrue(x => x.IsActive)
    .GreaterThan(x => x.Age, 18);  // Asegura que la edad sea mayor a 18

foreach (var user in users)
{
    var result = validator.Evaluate(user);
    Console.WriteLine(result ? $"✅ {user.Name} es válido" : $"❌ {user.Name} no es válido");
}