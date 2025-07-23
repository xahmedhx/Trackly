// File: Controllers/MainAdminController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Trackly.Data;
using Trackly.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackly.Controllers;

[Route("api/mainadmin")]
[Authorize(Roles = "MainAdmin")]
public class MainAdminController : Controller
{
    private readonly AppDbContext _context;

    public MainAdminController(AppDbContext context)
    {
        _context = context;
    }

 [HttpPost("CreateAdmin")]
public async Task<IActionResult> CreateAdmin(
    [FromForm] string username,
    [FromForm] string password,
    [FromForm] int departmentId)
{
    // ✅ Check if department exists
    var departmentExists = await _context.Departments.AnyAsync(d => d.Id == departmentId);
    if (!departmentExists)
    {
        TempData["Error"] = "Invalid department selected.";
        return RedirectToAction("Login", "Account");
    }

    // ✅ Check if an admin already exists for this department
    var adminExists = await _context.Admins.AnyAsync(a => a.DepartmentId == departmentId);
    if (adminExists)
    {
        TempData["Error"] = "This department already has an admin.";
        return RedirectToAction("Login", "Account");
    }

    // ✅ Proceed to create admin
    var newAdmin = new Admin
    {
        Username = username,
        Password = password,
        DepartmentId = departmentId
    };

    _context.Admins.Add(newAdmin);
    await _context.SaveChangesAsync();

    TempData["Success"] = "Admin created successfully!";
    return RedirectToAction("Login", "Account");
}



    [HttpPost("CreateEmployee")]
public async Task<IActionResult> CreateEmployee(
    [FromForm] string username,
    [FromForm] int departmentId)
{
    if (string.IsNullOrWhiteSpace(username))
    {
        TempData["Error"] = "Username is required.";
        return RedirectToAction("Login", "Account");
    }

    var department = await _context.Departments.FindAsync(departmentId);
    if (department == null)
    {
        TempData["Error"] = "Invalid department selected.";
        return RedirectToAction("Login", "Account");
    }

    _context.Employees.Add(new Employee
    {
        Username = username,
        DepartmentId = departmentId,
        Department = department
    });

    await _context.SaveChangesAsync();

    TempData["Success"] = "Employee created successfully!";
    return RedirectToAction("Login", "Account");
}

}
