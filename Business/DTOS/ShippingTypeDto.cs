using Domains;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppResource;
using Business.DTOS;



public partial class ShippingTypeDto : BaseDto
{
    [Required(
        AllowEmptyStrings = false,
        ErrorMessageResourceName = "NameArRequired",
        ErrorMessageResourceType = typeof(ValidationMessages)
    )]
    [StringLength(70, MinimumLength = 3,
        ErrorMessageResourceName = "NameLength",
        ErrorMessageResourceType = typeof(ValidationMessages)
    )]
    [RegularExpression(@"^[\u0600-\u065F\u066A-\u06EF\u06FA-\u06FF\s]+$",
        ErrorMessageResourceName = "OnlyValidArabicNamesAllowed",
        ErrorMessageResourceType = typeof(ValidationMessages)
    )]
    public string? ShippingTypeAname { get; set; }

    [Required(
        AllowEmptyStrings = false,
        ErrorMessageResourceName = "NameEnRequired",
        ErrorMessageResourceType = typeof(ValidationMessages)
    )]
    [StringLength(70, MinimumLength = 3,
        ErrorMessageResourceName = "NameLength",
        ErrorMessageResourceType = typeof(ValidationMessages)
    )]
    [RegularExpression(@"^[a-zA-Z\s]+$",
        ErrorMessageResourceName = "OnlyValidEnglishWordsAllowed",
        ErrorMessageResourceType = typeof(ValidationMessages)
    )]
    public string? ShippingTypeEname { get; set; }

    [Range(0.25, 10,
        ErrorMessageResourceName = "FactorRange",
        ErrorMessageResourceType = typeof(ValidationMessages)
    )]
    public double ShippingFactor { get; set; }
}






