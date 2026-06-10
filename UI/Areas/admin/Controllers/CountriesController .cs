using Business.Contracts;
using Business.DTOS;
using Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UI.Constants;
using UI.Helpers;



namespace UI.Areas.admin.Controllers
{
    
    [Area("admin")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Reviewer},{RoleNames.Operation},{RoleNames.OperationManager}")]
   

    public class CountriesController : Controller
    {
        
        private readonly ICountry _country;
        private readonly ILogger<CountriesController> _logger;

        public CountriesController(ICountry country, ILogger<CountriesController> logger)
        {
            _country = country;
            _logger = logger;
        }


        
        public async Task<IActionResult> Index()
        {
            var list = await _country.GetAll().ConfigureAwait(false);
            return View(list);
        }


        // GET: ShippingTypesController/Edit/5
        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task<ActionResult> Edit(Guid? Id)
        {
            TempData["MessageType"] = null;
            var country = new CountryDto();
            if (Id != null)
            {
                country = await _country.GetById((Guid)Id);
            }
            return View(country);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task <IActionResult> Save(CountryDto country)
        {
            TempData["MessageType"] = null;

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Validation error: {Error}", error.ErrorMessage);
                }

                return View("Edit", country);
            }

            try
            {
                if (country.Id == Guid.Empty)
                {
                    await _country.Add(country);
                    TempData["MessageType"] = MessageType.SaveSuccess;
                }
                else
                {
                    await _country.UpdateAsync(country);
                    TempData["MessageType"] = MessageType.UpdateSuccess;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save country with ID {Id}", country.Id);
                TempData["MessageType"] = MessageType.SaveFailed;
            }

            return RedirectToAction(nameof(Index));
        }


        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task <ActionResult> Delete(Guid Id)
        {
            TempData["MessageType"] = null;
            try
            {
                await _country.Delete(Id);
                //await _country.ChangeStatus(Id, 0);
                TempData["MessageType"] = MessageType.DeleteSuccess;
            }
            catch (Exception)
            {
                TempData["MessageType"] = MessageType.DeleteFailed;
            }

            return RedirectToAction(nameof(Index));
        }


        // POST: ShippingTypesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task <ActionResult> Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

    }
}
