using System.Security.Claims;
using Lyora.BLL.Services;
using Lyora.DAL.Entities;
using Lyora.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

public class AccountController : Controller
{
    private readonly AccountService _accountService;

    public AccountController(AccountService accountService)
    {
        _accountService = accountService;
    }

    // ---------- Login ----------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = _accountService.Authenticate(model.Email, model.Password);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Incorrect email or password.");
            return View(model);
        }

        await SignInAsync(user, model.RememberMe);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }
        return user is Administrator
            ? RedirectToAction("Index", "Admin")
            : RedirectToAction("Index", "Home");
    }

    // ---------- Register (customers only) ----------

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var customer = new Customer
        {
            FullName = model.FullName,
            Email = model.Email,
            Phone = Clean(model.Phone),
            DateOfBirth = model.DateOfBirth,
            Gender = Clean(model.Gender),
            Address = Clean(model.Address)
        };

        var result = _accountService.Register(customer, model.Password);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        await SignInAsync(customer, rememberMe: false);
        TempData["Success"] = $"Welcome to LYORA, {customer.FullName}!";
        return RedirectToAction("Index", "Home");
    }

    // ---------- Logout ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    // ---------- Profile (Manage Profile use case) ----------

    [Authorize(Roles = Roles.Customer)]
    [HttpGet]
    public IActionResult Profile()
    {
        var customer = _accountService.GetCustomer(CurrentUserId());
        if (customer == null)
        {
            return NotFound();
        }

        return View(new ProfileViewModel
        {
            Email = customer.Email,
            FullName = customer.FullName,
            Phone = customer.Phone,
            DateOfBirth = customer.DateOfBirth,
            Gender = customer.Gender,
            Address = customer.Address
        });
    }

    [Authorize(Roles = Roles.Customer)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var customer = _accountService.GetCustomer(CurrentUserId());
        if (customer == null)
        {
            return NotFound();
        }
        model.Email = customer.Email;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        customer.FullName = model.FullName;
        customer.Phone = Clean(model.Phone);
        customer.DateOfBirth = model.DateOfBirth;
        customer.Gender = Clean(model.Gender);
        customer.Address = Clean(model.Address);

        var result = _accountService.UpdateProfile(customer);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        await SignInAsync(customer, rememberMe: false);   // refresh the name shown in the header
        TempData["Success"] = "Your profile was updated.";
        return RedirectToAction(nameof(Profile));
    }

    // ---------- Change password (customers and administrators) ----------

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = _accountService.ChangePassword(CurrentUserId(), model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Your password was changed.";
        return User.IsInRole(Roles.Administrator)
            ? RedirectToAction("Index", "Admin")
            : RedirectToAction(nameof(Profile));
    }

    // ---------- helpers ----------

    private async Task SignInAsync(User user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties { IsPersistent = rememberMe };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
    }

    private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
