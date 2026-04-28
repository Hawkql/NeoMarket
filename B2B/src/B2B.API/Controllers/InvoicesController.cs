using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{
    public class InvoicesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
