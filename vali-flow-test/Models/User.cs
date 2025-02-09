namespace vali_flow_test.Models;

public class User
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Age { get; set; }
    public string JsonData { get; set; } = string.Empty;
    public string Base64Data  { get; set; } = string.Empty;
}
