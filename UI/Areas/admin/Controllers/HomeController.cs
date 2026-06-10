
using Business.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI.Constants;
using UI.Services;

namespace UI.Areas.admin.Controllers
{
    /// <summary>  
    /// Controller for handling administrative home-related actions.  
    /// </summary>  


    [Area("admin")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Reviewer},{RoleNames.Operation},{RoleNames.OperationManager}")]


    public class HomeController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<HomeController> _logger;
       

        public HomeController(
            IDashboardService dashboardService,
            ILogger<HomeController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;

        }

        /// <summary>  
        /// Displays the index view for the admin home area with dashboard statistics.  
        /// </summary>  
        /// <returns>The index view with dashboard data.</returns>  
        public async Task<IActionResult> Index()
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardDataAsync();
                return View(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load dashboard");
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "An error occurred while loading the dashboard.";
                return RedirectToAction("Index", "Home", new { area = "" });
            }
        }





    }
}
