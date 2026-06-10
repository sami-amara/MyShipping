using Business.Contracts;
using Business.DTOS;
using Domains;
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
    

    public class SubscriptionPackageController : Controller
    {
        private readonly ISubscriptionPackage _subscriptionPackage;
        public SubscriptionPackageController(ISubscriptionPackage subscriptionPackage)
        {
            this._subscriptionPackage = subscriptionPackage;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _subscriptionPackage.GetAll().ConfigureAwait(false);
            return View(list);
        }
        // GET: ShippingTypesController/Edit/5
        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task<ActionResult> Edit(Guid? Id)
        {
            TempData["MessageType"] = null;
            var subscriptionPackage = new SubscriptionPackageDto();
            if (Id != null)
            {
                subscriptionPackage = await _subscriptionPackage.GetById((Guid)Id);
            }
            return View(subscriptionPackage);
        }

        // POST: ShippingTypesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task <ActionResult> Save(SubscriptionPackageDto subscriptionPackage)
        {
            TempData["MessageType"] = null;
            if (!ModelState.IsValid)
            {
                return View("Edit", subscriptionPackage);
            }
            try
            {
                if(subscriptionPackage.Id == Guid.Empty)
                {
                    await _subscriptionPackage.Add(subscriptionPackage);
                    TempData["MessageType"] = MessageType.SaveSuccess;
                }
                else
                {
                    await _subscriptionPackage.UpdateAsync(subscriptionPackage);
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
                await _subscriptionPackage.Delete(Id);
                //await _subscriptionPackage.ChangeStatus(Id, 0);
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
