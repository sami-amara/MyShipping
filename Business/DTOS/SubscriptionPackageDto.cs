using AppResource;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOS
{
    public partial class SubscriptionPackageDto : BaseDto
    {
        [Required(ErrorMessageResourceName = "NameArRequired", ErrorMessageResourceType = typeof(message), AllowEmptyStrings = false)]
        [StringLength(100, MinimumLength = 4, ErrorMessageResourceName = "NameLenght", ErrorMessageResourceType = typeof(message))]
        [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessageResourceName = "InvalidPackageName", ErrorMessageResourceType = typeof(message))]
        public string PackageName { get; set; } = null!;


        [Required(ErrorMessageResourceName = "FactorRequired", ErrorMessageResourceType = typeof(Shipping), AllowEmptyStrings = false)]
        [Range(0.25, 10000, ErrorMessageResourceName = "hippimentCount", ErrorMessageResourceType = typeof(Shipping))]
        public int ShippimentCount { get; set; }


        [Required(ErrorMessageResourceName = "FactorRequired", ErrorMessageResourceType = typeof(Shipping), AllowEmptyStrings = false)]
        [Range(0.25, 10000, ErrorMessageResourceName = "NumberOfKiloMeters", ErrorMessageResourceType = typeof(Shipping))]
        public double NumberOfKiloMeters { get; set; }

        [Required(ErrorMessageResourceName = "FactorRequired", ErrorMessageResourceType = typeof(Shipping), AllowEmptyStrings = false)]
        [Range(0.25, 10000, ErrorMessageResourceName = "TotalWeight", ErrorMessageResourceType = typeof(Shipping))]

        public double TotalWeight { get; set; }
    }
}
