
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Trackly.Models;

 public class TimePlanViewModel
    {
        public int Id { get; set; }
        [Required]
        [Display(Name = "Time Plan Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Employee")]
        public int? EmployeeId { get; set; }

        public List<TimePlanItemViewModel> Items { get; set; } = new List<TimePlanItemViewModel>();
    }

    public class TimePlanItemViewModel
    {
        public int Id { get; set; }
        [Required]
        [Display(Name = "Task Name")]
        public string TaskName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Duration (Days)")]
        public int DurationInDays { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";
    }
