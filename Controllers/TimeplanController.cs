using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Trackly.Models;
using Trackly.Data;
using Microsoft.AspNetCore.Authorization;

namespace Trackly.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeplansController : Controller
    {
        private readonly AppDbContext _context;

        public TimeplansController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Timeplan>>> GetTimeplans()
        {
            var employeeUsername = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(employeeUsername))
            {
                return Unauthorized("Employee username not found in claims.");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Username == employeeUsername);
            if (employee == null)
            {
                return NotFound("Employee not found.");
            }

            return await _context.Timeplans
                                 .Where(t => t.EmployeeId == employee.Id)
                                 .Include(t => t.Employee)
                                 .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Timeplan>> GetTimeplan(int id)
        {
            var timeplan = await _context.Timeplans.Include(t => t.Employee)
                                                   .FirstOrDefaultAsync(t => t.Id == id);

            if (timeplan == null)
                return NotFound();

            return timeplan;
        }

        [HttpPost]
        [Authorize(Roles = "Employee")]
        public async Task<ActionResult<Timeplan>> CreateTimeplan(Timeplan timeplan)
        {
            // The employee ID should be set from the logged-in user
            var employeeId = User.FindFirstValue(ClaimTypes.Name);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Username == employeeId);
            if (employee == null)
                return BadRequest("Employee not found");

            timeplan.EmployeeId = employee.Id;
            _context.Timeplans.Add(timeplan);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTimeplan), new { id = timeplan.Id }, timeplan);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTimeplan(int id, Timeplan timeplan)
        {
            if (id != timeplan.Id)
                return BadRequest();

            var exists = await _context.Timeplans.AnyAsync(t => t.Id == id);
            if (!exists)
                return NotFound();

            _context.Entry(timeplan).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteTimeplan(int id)
{
    var timeplan = await _context.Timeplans.FindAsync(id);
    if (timeplan == null)
        return NotFound();

    _context.Timeplans.Remove(timeplan);
    await _context.SaveChangesAsync();

    TempData["SuccessMessage"] = "Time plan deleted successfully.";
    return RedirectToAction("MyTimePlans"); // or another relevant page
}

    }
}
