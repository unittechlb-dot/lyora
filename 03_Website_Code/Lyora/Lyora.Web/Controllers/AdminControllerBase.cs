using Lyora.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>Base for every administrator page – only users with the Administrator role can open them.</summary>
[Authorize(Roles = Roles.Administrator)]
public abstract class AdminControllerBase : Controller
{
    protected void Success(string message) => TempData["Success"] = message;

    protected void Error(string message) => TempData["Error"] = message;
}
