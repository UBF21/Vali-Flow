// See https://aka.ms/new-console-template for more information

using System.Text;
using vali_flow_test.Models;
using vali_flow.Builder;
using Newtonsoft.Json;

//source of data (Database,Collections,etc)
List<User> users = new List<User>
{
    new User
    {
        Name = "Alice", IsActive = true, Age = 25, JsonData = "{\"key\":\"value\"}",
        Base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes("Hello Alice"))
    },
    new User { Name = "Bob", IsActive = false, Age = 17, JsonData = "{invalidJsonData}", Base64Data = "SGVsbG8gQm9i" },
    new User
    {
        Name = "Pablito", IsActive = true, Age = 30, JsonData = "{\"name\":\"Pablito\",\"role\":\"admin\"}",
        Base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes("Secret Data"))
    },
    new User
    {
        Name = "Charlie", IsActive = true, Age = 20, JsonData = "{\"city\":\"New York\",\"temperature\":22}",
        Base64Data = "InvalidBase64Data=="
    }
};

//Builder the Expresiones 
var builder = new ValiFlow<User>()
    .NotNull(x => x.Name) // Ensures the field is not null.
    .IsTrue(x => x.IsActive) // Ensures the boolean field is true.
    .GreaterThan(x => x.Age, 18) // Ensures the numeric field is greater than the specified value (18 in this case).
    .IsJson(x => x.JsonData) // Ensures the field contains a valid JSON string.
    .IsBase64(x => x.Base64Data); // Ensures the field contains a valid Base64-encoded string.

//valid row collection 
var usersValid = builder.EvaluateAll(users);
string jsonOutput =  JsonConvert.SerializeObject(usersValid, Formatting.Indented);
Console.WriteLine(jsonOutput);

//Get Build Expression 
var expressionResult = builder.Build();

//rows passing the expression
foreach (var user in users)
{
    var result = builder.Evaluate(user);
    string value = result ? $" {user.Name} es válido" : $" {user.Name} no es válido";
    Console.WriteLine(value);
}
