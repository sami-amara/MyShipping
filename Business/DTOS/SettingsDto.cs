using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOS
{
    public partial class SettingsDto : BaseDto
    {
        public double? KiloMeterRate { get; set; }

        public double? KilooGramRate { get; set; }
    }
}
