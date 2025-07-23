namespace Trackly.Models;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Employee> Employees { get; set; } = new List<Employee>();
    public Admin Admin { get; set; } = new Admin();
}