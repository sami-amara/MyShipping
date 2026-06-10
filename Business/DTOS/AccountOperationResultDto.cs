using System.Collections.Generic;

namespace Business.DTOS
{
    public class AccountOperationResultDto
    {
        public bool Success { get; set; }
        public bool RequiresLogin { get; set; }
        public IEnumerable<string> Errors { get; set; } = new List<string>();
    }
}
