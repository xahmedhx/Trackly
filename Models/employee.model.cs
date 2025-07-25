using System.Text.Json.Serialization;

namespace Trackly.Models;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
     public int Status { get; set; } = 0; // 0: Inactive, 1: Active
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int DepartmentId { get; set; }

    [JsonIgnore]
    public Department Department { get; set; } = new Department();
    [JsonIgnore]
    public List<Timeplan> Timeplans { get; set; } = new List<Timeplan>();
}
public class Timeplan
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public DateTime CreatedDate { get; set; }

        [JsonIgnore]
        public Employee? Employee { get; set; }
        public List<TimePlanItem> Items { get; set; } = new List<TimePlanItem>();
    }

    public class TimePlanItem
    {
        public int Id { get; set; }
        public int TimeplanId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DurationInDays { get; set; }
        public string Status { get; set; } = "Pending";
        public Timeplan? Timeplan { get; set; }
    }