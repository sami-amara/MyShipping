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




//public class CountryDto : BaseDto
//{

//    [Required(ErrorMessageResourceName = "NameArRequired", ErrorMessageResourceType = typeof(message))]
//    [StringLength(100, MinimumLength = 3, ErrorMessage = "Arabic name must be between 3 and 100 characters.")]
//    [RegularExpression(@"^[\u0600-\u065F\u066A-\u06EF\u06FA-\u06FF\s]+$", ErrorMessage = "Only valid Arabic characters allowed.")]
//    public string CountryAname { get; set; }

//    [Required(ErrorMessage = "English name is required.")]
//    [StringLength(100, MinimumLength = 3, ErrorMessage = "English name must be between 3 and 100 characters.")]
//    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Only valid English characters allowed.")]
//    public string CountryEname { get; set; }
//}
//namespace Business.DTOS
//{
//    public partial class CountryDto : BaseDto
//    {
//        //[Required(ErrorMessageResourceName = "NameArRequired", ErrorMessageResourceType = typeof(message), AllowEmptyStrings = false)]
//        [StringLength(100, MinimumLength = 3, ErrorMessageResourceName = "NameLenght", ErrorMessageResourceType = typeof(message))]
//        [RegularExpression(@"^[\u0600-\u065F\u066A-\u06EF\u06FA-\u06FF\s]{3,70}$", ErrorMessageResourceName = "OnlyValidArabicNamesAllowed", ErrorMessageResourceType = typeof(message))]
//        public string? CountryAname { get; set; }

//        //[Required(ErrorMessageResourceName = "NameEnRequired", ErrorMessageResourceType = typeof(message), AllowEmptyStrings = false)]
//        [StringLength(100, MinimumLength = 3, ErrorMessageResourceName = "NameLenght", ErrorMessageResourceType = typeof(message))]
//        [RegularExpression(@"^[a-zA-Z\s]{3,70}$", ErrorMessageResourceName = "OnlyValidEnglishWordsAllowed", ErrorMessageResourceType = typeof(message))]
//        public string? CountryEname { get; set; }


//    }
//}
