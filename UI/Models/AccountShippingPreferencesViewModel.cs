using Microsoft.AspNetCore.Mvc.Rendering;

namespace UI.Models
{
    public class AccountShippingPreferencesViewModel
    {
        public Guid? DefaultCountryId { get; set; }

        public Guid? DefaultCityId { get; set; }

        public Guid? DefaultCarrierId { get; set; }

        public Guid? DefaultShippingPackageId { get; set; }

        public Guid? DefaultShippingTypeId { get; set; }

        public List<SelectListItem> Countries { get; set; } = new();

        public List<SelectListItem> Cities { get; set; } = new();

        public List<SelectListItem> Carriers { get; set; } = new();

        public List<SelectListItem> ShippingPackages { get; set; } = new();

        public List<SelectListItem> ShippingTypes { get; set; } = new();
    }
}
