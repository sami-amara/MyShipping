using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOS
{
    public class BaseDto
    {
        public Guid Id { get; set; }
        //public int?  CurrentSatus{ get; set; }
        public int  CurrentState{ get; set; }

        public Guid UpdatedBy { get; set; }
    }
}
