using Business.Contracts;
using Business.DTOS;
using Domains;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Serilog;
using System.Security.Principal;
using System.Threading.Tasks;
using UI.Constants;
using UI.Helpers;



namespace UI.Areas.admin.Controllers
{

    [Area("admin")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Reviewer},{RoleNames.Operation},{RoleNames.OperationManager}")]
   

    public class CitiesController : Controller
    {
        private readonly ICity _city;
        private readonly ICountry _country;
        public CitiesController(ICity city, ICountry country)
        {
            this._city = city;
            _country = country;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _city.GetAllCitites().ConfigureAwait(false);
            return View(list);
        }


        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task<IActionResult> Edit(Guid? Id)
        {
            await LoadCountries();
            var city = new CityDto();
            if (Id != null)
            {
                city = await _city.GetById((Guid)Id);
            }
            return View(city);
        }


        //// POST: ShippingTypesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task<ActionResult> Save(CityDto cityDto)
        {
            
            TempData["MessageType"] = null;
            if (!ModelState.IsValid)
            {
                await LoadCountries();
                return View("Edit", cityDto);
            }
            try
            {
                if (cityDto.Id == Guid.Empty)
                {
                    await _city.Add(cityDto);
                    TempData["MessageType"] = MessageType.SaveSuccess;
                }
                else
                {
                    await _city.UpdateAsync(cityDto);
                    TempData["MessageType"] = MessageType.UpdateSuccess;
                }
            }
            catch (Exception)
            {
                TempData["MessageType"] = MessageType.SaveFailed;
            }
            return RedirectToAction(nameof(Index));
        }


        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task <ActionResult> Delete(Guid id)
        {
            TempData["MessageType"] = null;
            try
            {
                await _city.Delete(id);
                //await _city.ChangeStatus(id, 0);
                TempData["MessageType"] = MessageType.DeleteSuccess;
            }
            catch (Exception)
            {
                TempData["MessageType"] = MessageType.DeleteFailed;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task LoadCountries()
        {
            var countries = await _country.GetAll().ConfigureAwait(false) ;
            ViewBag.Countries = countries;
        }
      


    }
}
