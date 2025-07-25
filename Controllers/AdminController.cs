using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trackly.Data;
using Trackly.Models;
using Microsoft.AspNetCore.Authorization;

namespace Trackly.Controllers
{
    [Route("api/[controller]")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("/api/admin/my-department-timeplans")]
        public async Task<IActionResult> GetDepartmentTimeplans([FromQuery] int employeeId)
        {
            var deptClaim = User.Claims.FirstOrDefault(c => c.Type == "DepartmentId");
            if (deptClaim == null)
                return Unauthorized("Missing department claim");

            var departmentId = int.Parse(deptClaim.Value);

            var plans = await _context.Timeplans
                .Include(t => t.Employee)
                .Where(t => t.Employee.DepartmentId == departmentId && t.EmployeeId == employeeId)
                .ToListAsync();

            return View("EmployeeTimePlans",plans);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("timeplans-by-employee/{employeeId}")]
        public async Task<IActionResult> GetTimeplansByEmployee(int employeeId)
        {
            var plans = await _context.Timeplans
                .Where(t => t.EmployeeId == employeeId)
                .ToListAsync();

            return Ok(plans);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("/admin/employee-timeplans")]
        public async Task<IActionResult> EmployeeTimePlans(int employeeId)
        {
            var employee = await _context.Employees
                .Include(e => e.Timeplans)
                    .ThenInclude(tp => tp.Items)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
                return NotFound();

            ViewBag.EmployeeName = employee.Name;
            return View("EmployeeTimePlans", employee.Timeplans);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("edit-timeplan/{id}")]
        public async Task<IActionResult> EditTimePlan(int id)
        {
            var plan = await _context.Timeplans
                .Include(tp => tp.Items)
                .FirstOrDefaultAsync(tp => tp.Id == id);

            if (plan == null)
                return NotFound();

            return View("EditTimePlan", plan);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("edit-timeplan/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTimePlan(int id, Timeplan updatedPlan)
        {
            if (id != updatedPlan.Id)
                return BadRequest();

            var existingPlan = await _context.Timeplans
                .Include(tp => tp.Items)
                .FirstOrDefaultAsync(tp => tp.Id == id);

            if (existingPlan == null)
                return NotFound();

            existingPlan.Title = updatedPlan.Title;
            existingPlan.CreatedDate = updatedPlan.CreatedDate;

            // Remove old items and re-add updated ones
            _context.TimePlanItems.RemoveRange(existingPlan.Items);

            // Reattach new items (which are bound from form)
            foreach (var item in updatedPlan.Items)
            {
                item.TimeplanId = existingPlan.Id; // Make sure FK is set
                _context.TimePlanItems.Add(item);
            }

            await _context.SaveChangesAsync();

            // Redirect to a suitable page after editing, e.g., back to the timeplan list
            return RedirectToAction("EditTimePlan", new { id = existingPlan.Id });
        }
    }
}