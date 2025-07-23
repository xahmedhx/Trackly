using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trackly.Data;
using Trackly.Models;

namespace Trackly.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;

    public AccountController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (User?.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("MainAdmin"))
            {
                ViewBag.Departments = await _context.Departments.ToListAsync();
            }
            else if (User.IsInRole("Admin"))
            {
                var username = User.FindFirstValue(ClaimTypes.Name);
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
                if (admin != null)
                {
                    ViewBag.DepartmentEmployees = await _context.Employees
                        .Where(e => e.DepartmentId == admin.DepartmentId)
                        .ToListAsync();
                }
            }
        }
        else
        {
            return View("Login");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            // Re-populate ViewBag data in case of validation errors
            await PopulateViewBagData();
            return View(model);
        }

        // Check MainAdmin table
        var mainAdmin = await _context.MainAdmins
            .FirstOrDefaultAsync(a => a.Username == model.Username && a.Password == model.Password);
        if (mainAdmin != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, mainAdmin.Username),
                new Claim(ClaimTypes.Role, "MainAdmin")
            };
            await SignInUser(claims);
            return RedirectToLocal(returnUrl, "Index", "Home");
        }

        // Check Admin table
        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.Username == model.Username && a.Password == model.Password);
        if (admin != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Role, "Admin")
            };
            await SignInUser(claims);
            return RedirectToLocal(returnUrl, "Index", "Home");
        }

        // Check Employee table
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Username == model.Username && e.Password == model.Password);
        if (employee != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, employee.Username),
                new Claim(ClaimTypes.Role, "Employee")
            };
            await SignInUser(claims);
            return RedirectToLocal(returnUrl, "CreateTimePlan", "Employee");
        }

        ModelState.AddModelError("", "Invalid username or password");

        // Re-populate ViewBag data in case of login failure
        await PopulateViewBagData();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("MyCookieAuth");
        return RedirectToAction("Login", "Account");
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Admin()
    {
        return View();
    }

    [Authorize(Roles = "Employee")]
    public IActionResult Employee()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult SetPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> SetPassword(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _context.Employees.FirstOrDefaultAsync(e => e.Username == model.Username);
        if (user != null)
        {
            user.Password = model.Password;
            await _context.SaveChangesAsync();
            return RedirectToAction("Login");
        }

        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == model.Username);
        if (admin != null)
        {
            admin.Password = model.Password;
            await _context.SaveChangesAsync();
            return RedirectToAction("Login");
        }

        var mainAdmin = await _context.MainAdmins.FirstOrDefaultAsync(m => m.Username == model.Username);
        if (mainAdmin != null)
        {
            mainAdmin.Password = model.Password;
            await _context.SaveChangesAsync();
            return RedirectToAction("Login");
        }

        ModelState.AddModelError("", "Username not found.");
        return View(model);
    }

    private async Task SignInUser(List<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, "MyCookieAuth");
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync("MyCookieAuth", principal);
    }

    private IActionResult RedirectToLocal(string? returnUrl, string defaultAction, string defaultController)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(defaultAction, defaultController);
    }

    private async Task PopulateViewBagData()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("MainAdmin"))
            {
                ViewBag.Departments = await _context.Departments.ToListAsync();
            }
            else if (User.IsInRole("Admin"))
            {
                var username = User.FindFirstValue(ClaimTypes.Name);
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
                if (admin != null)
                {
                    ViewBag.DepartmentEmployees = await _context.Employees
                        .Where(e => e.DepartmentId == admin.DepartmentId)
                        .ToListAsync();
                }
            }
        }
    }
    [Authorize(Roles = "Employee")]
[HttpGet]
public async Task<IActionResult> ChangePassword()
{
    return View();
}

[Authorize(Roles = "Employee")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    var username = User.Identity?.Name;
    if (string.IsNullOrEmpty(username))
        return Unauthorized();

    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Username == username);
    if (employee == null)
        return NotFound();

    if (employee.Password != model.CurrentPassword)
    {
        ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
        return View(model);
    }

    employee.Password = model.NewPassword;
    await _context.SaveChangesAsync();

    TempData["Success"] = "Password changed successfully!";
    return RedirectToAction("CreateTimePlan", "Employee");
}

}