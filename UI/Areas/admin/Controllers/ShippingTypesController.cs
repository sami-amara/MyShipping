using Business.Contracts;
using Business.DTOS;
using Domains;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using UI.Constants;
using UI.Helpers;


namespace UI.Areas.admin.Controllers
{
  
    [Area("admin")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Reviewer},{RoleNames.Operation},{RoleNames.OperationManager}")]
    

    public class ShippingTypesController : Controller
    {
        private readonly IShippingTypes _shippingType;
        public ShippingTypesController(IShippingTypes shippingType)
        {
            _shippingType = shippingType;
        }

        
        public async Task<IActionResult> Index()
        {
            var list = await _shippingType.GetAll().ConfigureAwait(false);
            return View(list);
        }
        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task<IActionResult> Edit(Guid? Id)
        {
            var shippingType = new ShippingTypeDto();
            if (Id != null)
            {
                shippingType =  await _shippingType.GetById((Guid)Id);
            }
            return View(shippingType);
        }


        //// POST: ShippingTypesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task <ActionResult> Save(ShippingTypeDto shippingType)
        {
            TempData["MessageType"] = null;
            if (!ModelState.IsValid)
            {
                return View("Edit", shippingType);
            }
            try
            {
                if (shippingType.Id == Guid.Empty)
                {
                    await _shippingType.Add(shippingType);
                    TempData["MessageType"] = MessageType.SaveSuccess;
                }
                else
                {
                   await _shippingType.UpdateAsync(shippingType);
                    TempData["MessageType"] = MessageType.UpdateSuccess;
                }
            }
            catch (Exception)
            {
                TempData["MessageType"] = MessageType.SaveFailed;
            }
            return RedirectToAction(nameof(Index));
        }


        // GET: ShippingTypesController/Delete/5
        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task <ActionResult> Delete(Guid Id)
        {
            TempData["MessageType"] = null;
            try
            {
                await _shippingType.Delete(Id);
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
        public ActionResult Delete(int id, IFormCollection collection)
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
