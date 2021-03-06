using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace FBLARoverAgenda_Backend.Web.Controllers;

[AllowAnonymous]
public class ErrorController : Controller
{
    [Route("error/{code}")]
    [HttpGet]
    public IActionResult Index(int? code = null)
    {
        int[] available = { 401, 404, 500 };

        if (code.HasValue)
        {
            if (available.Contains(code.Value))
            {
                var viewName = code.ToString();
                return View(viewName);
            }
        }
        return View();
    }

}