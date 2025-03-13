using Microsoft.AspNetCore.Mvc;

namespace E_Learning.Controllers
{
	public class RoadmapController : Controller
	{
		[HttpGet("/Roadmap")]
		public IActionResult Index()
		{
			return View();
		}
	}
}
