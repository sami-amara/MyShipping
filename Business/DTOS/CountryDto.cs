using AppResource;
using Business.DTOS;
using Domains;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOS
{
    public class CountryDto : BaseDto
    {
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "NameArRequired",ErrorMessageResourceType = typeof(ValidationMessages))]
        [StringLength(100, MinimumLength = 3,ErrorMessageResourceName = "NameLength"
            ,ErrorMessageResourceType = typeof(ValidationMessages))]
        [RegularExpression(@"^[\u0600-\u065F\u066A-\u06EF\u06FA-\u06FF\s]+$", 
            ErrorMessageResourceName = "OnlyValidArabicNamesAllowed", ErrorMessageResourceType = typeof(ValidationMessages) )]
        public string? CountryAname { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "NameEnRequired",ErrorMessageResourceType = typeof(ValidationMessages))]
        [StringLength(100, MinimumLength = 3,
            ErrorMessageResourceName = "NameLength",
            ErrorMessageResourceType = typeof(ValidationMessages)
        )]
        [RegularExpression(@"^[a-zA-Z\s]+$",
            ErrorMessageResourceName = "OnlyValidEnglishWordsAllowed",
            ErrorMessageResourceType = typeof(ValidationMessages)
        )]
        public string? CountryEname { get; set; }
    }

}
