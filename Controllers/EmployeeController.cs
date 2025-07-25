using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trackly.Data;
using Trackly.Models;

namespace Trackly.Controllers
{
    // Keep your existing API controller
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmployeesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> GetAll()
        {
            return await _context.Employees.Include(e => e.Department).Include(e => e.Timeplans).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetById(int id)
        {
            var employee = await _context.Employees.Include(e => e.Department).Include(e => e.Timeplans).FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null) return NotFound();
            return employee;
        }

        [HttpPost]
        public async Task<ActionResult<Employee>> Create(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Employee employee)
        {
            if (id != employee.Id) return BadRequest();
            _context.Entry(employee).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Employees.Any(e => e.Id == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    // Add a new MVC Controller for handling views and forms
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }

        // Employee list view
        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Timeplans)
                .ToListAsync();
            return View(employees);
        }

        // Employee details view
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Timeplans)
                    .ThenInclude(t => t.Items)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return NotFound();

            return View(employee);
        }

        // GET: Show the create time plan form
        [HttpGet]
        public async Task<IActionResult> CreateTimePlan()
        {
            var employeeId = await GetCurrentEmployeeId();
            if (employeeId == null)
            {
                return RedirectToAction("Login", "Account"); // Or wherever your login is
            }

            var employee = await _context.Employees.FindAsync(employeeId);
            ViewBag.EmployeeName = employee?.Name;

            var model = new TimePlanViewModel
            {
                EmployeeId = employeeId,
                Items = new List<TimePlanItemViewModel>
        {
            new TimePlanItemViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1),
                Status = "Pending"
            }
        }
            };
            return View(model);
        }

        private async Task<int?> GetCurrentEmployeeId()
        {
            if (User.IsInRole("Employee"))
            {
                var username = User.FindFirstValue(ClaimTypes.Name);
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Username == username);
                return employee?.Id;
            }
            return null;
        }

        // POST: Handle form submission for time plan creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTimePlan(TimePlanViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Create the main timeplan record
                var timeplan = new Timeplan
                {
                    Title = model.Title,
                    EmployeeId = model.EmployeeId.Value,
                    CreatedDate = DateTime.Now,

                    Items = new List<TimePlanItem>()
                };

                // Add the items (only non-empty task names)
                foreach (var item in model.Items.Where(i => !string.IsNullOrWhiteSpace(i.TaskName)))
                {
                    // Always calculate duration from start and end dates
                    var duration = 1; // Default to 1 day
                    if (item.EndDate >= item.StartDate)
                    {
                        duration = (int)(item.EndDate - item.StartDate).TotalDays + 1; // Include both start and end days
                    }

                    timeplan.Items.Add(new TimePlanItem
                    {
                        TaskName = item.TaskName.Trim(),
                        StartDate = item.StartDate,
                        EndDate = item.EndDate,
                        DurationInDays = duration,
                        Status = item.Status ?? "Pending"
                    });
                }

                if (!timeplan.Items.Any())
                {
                    ModelState.AddModelError("", "Please add at least one task to the time plan.");
                    return View(model);
                }

                _context.Timeplans.Add(timeplan);
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"Time plan '{timeplan.Title}' created successfully with {timeplan.Items.Count} task(s)!";
                return RedirectToAction("MyTimePlans"); // Redirect to the employee's time plans list
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError("", "An error occurred while saving the time plan: " + errorMessage);
                return View(model);
            }

        }
        // GET: Show time plans for an employee
        public async Task<IActionResult> TimePlans(int employeeId)
        {
            var employee = await _context.Employees
                .Include(e => e.Timeplans)
                    .ThenInclude(t => t.Items)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
                return NotFound();

            return View(employee);
        }

        // GET: View specific time plan details
        public async Task<IActionResult> TimePlanDetails(int id)
        {
            var timeplan = await _context.Timeplans
                .Include(t => t.Employee)
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (timeplan == null)
                return NotFound();

            return View(timeplan);
        }

        public async Task<IActionResult> MyTimePlans()
        {
            var employeeId = await GetCurrentEmployeeId();
            if (employeeId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employees
                .Include(e => e.Timeplans)
                    .ThenInclude(t => t.Items)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
                return NotFound("Employee profile not found");

            ViewBag.Employee = employee;
            return View("TimePlanView", employee.Timeplans.ToList());
        }
        // Add this method to your EmployeeController:

        // Add this method to your EmployeeController:

        [HttpGet]
        public async Task<IActionResult> EditTimePlan(int id)
        {
            Console.WriteLine($"[DEBUG] EditTimePlan GET called for ID: {id}");

            var employeeId = await GetCurrentEmployeeId();
            Console.WriteLine($"[DEBUG] Current EmployeeId: {employeeId}");

            if (employeeId == null)
            {
                Console.WriteLine($"[ERROR] EmployeeId is null - redirecting to login");
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var timeplan = await _context.Timeplans
                    .Include(t => t.Items)
                    .Include(t => t.Employee)
                    .FirstOrDefaultAsync(t => t.Id == id && t.EmployeeId == employeeId);

                Console.WriteLine($"[DEBUG] Timeplan found: {timeplan != null}");

                if (timeplan == null)
                {
                    Console.WriteLine($"[ERROR] Time plan not found for editing, ID: {id}");
                    return NotFound("Time plan not found or access denied");
                }

                Console.WriteLine($"[DEBUG] Editing time plan: {timeplan.Title} with {timeplan.Items?.Count ?? 0} items");

                // Get navigation info for previous/next time plans
                var allTimePlans = await _context.Timeplans
                    .Where(t => t.EmployeeId == employeeId)
                    .OrderBy(t => t.CreatedDate)
                    .Select(t => new { t.Id, t.Title })
                    .ToListAsync();

                var currentIndex = allTimePlans.FindIndex(t => t.Id == id);
                ViewBag.PreviousTimePlan = currentIndex > 0 ? allTimePlans[currentIndex - 1] : null;
                ViewBag.NextTimePlan = currentIndex < allTimePlans.Count - 1 ? allTimePlans[currentIndex + 1] : null;
                ViewBag.EmployeeName = timeplan.Employee?.Name ?? "Unknown";
                ViewBag.IsEditing = true;

                var model = new TimePlanViewModel
                {
                    Id = timeplan.Id,
                    Title = timeplan.Title ?? "",
                    EmployeeId = timeplan.EmployeeId,
                    Items = new List<TimePlanItemViewModel>()
                };

                // Safely populate items
                if (timeplan.Items != null && timeplan.Items.Any())
                {
                    model.Items = timeplan.Items.Select(i => new TimePlanItemViewModel
                    {
                        Id = i.Id,
                        TaskName = i.TaskName ?? "",
                        StartDate = i.StartDate,
                        EndDate = i.EndDate,
                        DurationInDays = i.DurationInDays,
                        Status = i.Status ?? "Pending"
                    }).ToList();
                }
                else
                {
                    // Add a default empty item if no items exist
                    model.Items.Add(new TimePlanItemViewModel
                    {
                        Id = 0,
                        TaskName = "",
                        StartDate = DateTime.Today,
                        EndDate = DateTime.Today.AddDays(1),
                        DurationInDays = 1,
                        Status = "Pending"
                    });
                }

                Console.WriteLine($"[DEBUG] Model created with {model.Items.Count} items");

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in EditTimePlan: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
                }

                // Return a safe model in case of error
                var safeModel = new TimePlanViewModel
                {
                    Id = id,
                    Title = "",
                    EmployeeId = employeeId,
                    Items = new List<TimePlanItemViewModel>
            {
                new TimePlanItemViewModel
                {
                    Id = 0,
                    TaskName = "",
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(1),
                    DurationInDays = 1,
                    Status = "Pending"
                }
            }
                };

                ViewBag.EmployeeName = "Unknown";
                TempData["ErrorMessage"] = "An error occurred while loading the time plan.";

                return View(safeModel);
            }
        }

        // Also add the POST method for handling the edit:
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTimePlan(int id, TimePlanViewModel model)
        {
            Console.WriteLine($"[DEBUG] EditTimePlan POST called for ID: {id}");

            if (id != model.Id)
            {
                Console.WriteLine($"[ERROR] ID mismatch: URL ID {id}, Model ID {model.Id}");
                return BadRequest("ID mismatch");
            }

            var employeeId = await GetCurrentEmployeeId();
            if (employeeId == null)
            {
                Console.WriteLine($"[ERROR] EmployeeId is null");
                return RedirectToAction("Login", "Account");
            }

            // Override the EmployeeId with the logged-in user (security measure)
            model.EmployeeId = employeeId;

            if (!ModelState.IsValid)
            {
                Console.WriteLine($"[DEBUG] ModelState is invalid for edit");
                var employee = await _context.Employees.FindAsync(employeeId);
                ViewBag.EmployeeName = employee?.Name ?? "Unknown";
                ViewBag.IsEditing = true;

                // Ensure Items is not null
                if (model.Items == null)
                {
                    model.Items = new List<TimePlanItemViewModel>();
                }

                return View(model);
            }

            try
            {
                var timeplan = await _context.Timeplans
                    .Include(t => t.Items)
                    .FirstOrDefaultAsync(t => t.Id == id && t.EmployeeId == employeeId);

                if (timeplan == null)
                {
                    Console.WriteLine($"[ERROR] Time plan not found for update, ID: {id}");
                    return NotFound("Time plan not found or access denied");
                }

                Console.WriteLine($"[DEBUG] Updating time plan: {timeplan.Title}");

                // Update timeplan properties
                timeplan.Title = model.Title?.Trim() ?? "";

                // Remove existing items
                if (timeplan.Items != null)
                {
                    _context.TimePlanItems.RemoveRange(timeplan.Items);
                    timeplan.Items.Clear();
                }
                else
                {
                    timeplan.Items = new List<TimePlanItem>();
                }

                // Add updated items
                if (model.Items != null)
                {
                    foreach (var item in model.Items.Where(i => !string.IsNullOrWhiteSpace(i.TaskName)))
                    {
                        // Always calculate duration from start and end dates
                        var duration = 1; // Default to 1 day
                        if (item.EndDate >= item.StartDate)
                        {
                            duration = (int)(item.EndDate - item.StartDate).TotalDays + 1;
                        }

                        timeplan.Items.Add(new TimePlanItem
                        {
                            TaskName = item.TaskName.Trim(),
                            StartDate = item.StartDate,
                            EndDate = item.EndDate,
                            DurationInDays = duration,
                            Status = item.Status ?? "Pending"
                        });
                    }
                }

                if (!timeplan.Items.Any())
                {
                    ModelState.AddModelError("", "Please add at least one task to the time plan.");
                    var employee = await _context.Employees.FindAsync(employeeId);
                    ViewBag.EmployeeName = employee?.Name ?? "Unknown";
                    ViewBag.IsEditing = true;
                    return View(model);
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"[DEBUG] Successfully updated time plan");

                TempData["SuccessMessage"] = $"Time plan '{timeplan.Title}' updated successfully with {timeplan.Items.Count} task(s)!";
                return RedirectToAction("TimePlanDetails", new { id = timeplan.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception updating time plan: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while updating the time plan: " + ex.Message);

                var employee = await _context.Employees.FindAsync(employeeId);
                ViewBag.EmployeeName = employee?.Name ?? "Unknown";
                ViewBag.IsEditing = true;

                // Ensure Items is not null
                if (model.Items == null)
                {
                    model.Items = new List<TimePlanItemViewModel>();
                }

                return View(model);
            }
        }
        [HttpPost("Employee/DeleteTimePlan/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTimePlan(int id)
        {
            var timeplan = await _context.Timeplans
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (timeplan == null)
                return NotFound();

            _context.Timeplans.Remove(timeplan);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Time plan deleted successfully.";
            return RedirectToAction("MyTimePlans");
        }
    }

}