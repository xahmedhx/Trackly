

namespace Trackly.Models;

public class Admin
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
}
