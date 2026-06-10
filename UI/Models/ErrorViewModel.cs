namespace UI.Models
{
    public class ErrorViewModel
    {

        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string? ErrorMessage { get; set; }
        public int? StatusCode { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? StackTrace { get; set; }

        //public string? RequestId { get; set; }

        //public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
