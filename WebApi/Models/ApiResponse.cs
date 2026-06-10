namespace WebApi.Models
{


    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<Error>? Errors { get; set; }


        public ApiResponse(bool isSuccess, string message, T data = default(T), List<Error> errors = null)
        {
            IsSuccess = isSuccess;
            Message = message;
            Data = data;
            Errors = errors ?? new List<Error>();
        }

        public static ApiResponse<T> SuccessResponse(T data, string message = "Operation Successful")
        {
            return new ApiResponse<T>(true, message, data);
        }

        public static ApiResponse<T> FailureResponse(string message, List<Error> errors = null)
        {
            return new ApiResponse<T>(false, message, errors: errors);
        }
    }

    public class Error
    {
        public string Code { get; set; }
        public string Description { get; set; }

        public Error(string code, string description)
        {
            Code = code;
            Description = description;
        }
    }
}




