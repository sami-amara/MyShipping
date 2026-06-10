using Business.Contracts;
using Business.Contracts.Shipment;
using Business.Services.Shipment.ManageShipmentsState;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Models;

namespace UI.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IShipmentQuery _shipmentQuery;

        public HomeController(IShipmentQuery shipmentQuery)
        {
            _shipmentQuery = shipmentQuery;
        }
        public IActionResult Index()
        {
            //var shippingTypes = _country.GetAll();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

       
        public async Task<IActionResult> Payment(Guid? shipmentId)
        {
            // If no shipment ID provided, return to home
            if (!shipmentId.HasValue)
            {
                TempData["ErrorMessage"] = "Shipment ID is required for payment.";
                return RedirectToAction("List");
            }

            // Check if shipment exists and if it's already paid
            var shipment = await _shipmentQuery.GetByIdAsync(shipmentId.Value);
            if (shipment == null)
            {
                TempData["ErrorMessage"] = "Shipment not found.";
                return RedirectToAction("List");
            }

            // If shipment is already paid, redirect to Show page
            if (shipment.IsPaid)
            {
                TempData["InfoMessage"] = "This shipment has already been paid.";
                return RedirectToAction("Show", "Shipments", new { id = shipmentId.Value });
            }

            // If shipment is not in Created or Updated status, payment not allowed
            if (shipment.CurrentState != (int)ShipmentStatusEnum.Created && 
                shipment.CurrentState != (int)ShipmentStatusEnum.Updated)
            {
                TempData["ErrorMessage"] = "Payment is only allowed for shipments in Created or Updated status.";
                return RedirectToAction("Show", "Shipments", new { id = shipmentId.Value });
            }

            return View();
        }



        [AllowAnonymous]
        [HttpGet]
        public IActionResult SetLanguage(string culture, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(culture))
            {
                culture = "en";
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    HttpOnly = false,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax
                });

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }





        [AllowAnonymous]
        public IActionResult AboutUs()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Services()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult ContactUs()
        {
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
