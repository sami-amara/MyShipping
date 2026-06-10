using Business.Contracts;
using Business.DTOS;
using DataAccessLayer.Exceptions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApi.Models;


namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    

    public class CarrierController : ControllerBase
    {
        private readonly ILogger<CarrierController> _logger;
        private readonly ICarrier _carrier;
        public CarrierController(ICarrier carrier, ILogger<CarrierController> logger)
        {
             _carrier = carrier;
            _logger = logger;
        }


        [HttpGet]
        
        public async Task<ActionResult<ApiResponse<List<CarrierDto>>>> Get()
        {
            try
            {
                var countries = await _carrier.GetAll();
                if (countries == null || countries.Count == 0)
                {
                    var error = new Error("E003", "Countries not found");
                    return ApiResponse<List<CarrierDto>>.FailureResponse("Countries not found", new List<Error> { error });
                }
                return Ok(ApiResponse<List<CarrierDto>>.SuccessResponse(countries));
            }
            catch (DataAccessExceptions ex)
            {
                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<List<CarrierDto>>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting countries");
                var error = new Error("E001", "An error occurred");
                return ApiResponse<List<CarrierDto>>.FailureResponse("An error occurred", new List<Error> { error });
            }
        }

        [HttpGet("{id}")]
       
        public async Task<ActionResult<ApiResponse<CarrierDto>>> GetById(Guid id)
        {
            try
            {
                var city = await _carrier.GetById(id);
                if (city == null)
                {
                    var error = new Error("E003", "ShippingType not found");
                    return ApiResponse<CarrierDto>.FailureResponse("ShippingType not found", new List<Error> { error });
                }
                return Ok(ApiResponse<CarrierDto>.SuccessResponse(city));
            }
            catch (DataAccessExceptions ex)
            {
                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<CarrierDto>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ShippingType with ID {id}", id);
                var error = new Error("E001", "An error occurred");
                return ApiResponse<CarrierDto>.FailureResponse("An error occurred", new List<Error> { error });
            }
        }



    }
}