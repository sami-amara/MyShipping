using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;



namespace Business.Services.Logging
{
    

    public class MaskingFormatProvider : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type formatType)
        {
            return formatType == typeof(ICustomFormatter) ? this : null;
        }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg is string str && str.Contains("@"))
            {
                var parts = str.Split('@');
                if (parts.Length == 2)
                {
                    var local = parts[0];
                    var domain = parts[1];
                    // Mask everything after first 2 chars
                    var maskedLocal = local.Length > 2 ? local.Substring(0, 2) + "***" : "***";
                   
                    return $"{maskedLocal}@{domain}";
                }
            }
            return arg?.ToString() ?? string.Empty;
        }
    }
}
