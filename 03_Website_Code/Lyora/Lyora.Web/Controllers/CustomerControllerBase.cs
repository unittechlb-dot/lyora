using System.Security.Claims;
using Lyora.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>Base for pages that only logged-in customers can open.</summary>
[Authorize(Roles = Roles.Customer)]
public abstract class CustomerControllerBase : Controller
{
    protected int CustomerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
