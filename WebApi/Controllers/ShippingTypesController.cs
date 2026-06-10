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
    

    public class ShippingTypesController : ControllerBase
    {
        private readonly ILogger<ShippingTypesController> _logger;
        private readonly IShippingTypes _shippingTypes;
        public ShippingTypesController(IShippingTypes shippingTypes, ILogger<ShippingTypesController> logger)
        {
             _shippingTypes = shippingTypes;
            _logger = logger;
        }


        [HttpGet]
      
        public async Task<ActionResult<ApiResponse<List<ShippingTypeDto>>>> Get()
        {
            try
            {
                var countries = await _shippingTypes.GetAll();
                if (countries == null || countries.Count == 0)
                {
                    var error = new Error("E003", "Countries not found");
                    return ApiResponse<List<ShippingTypeDto>>.FailureResponse("Countries not found", new List<Error> { error });
                }
                return Ok(ApiResponse<List<ShippingTypeDto>>.SuccessResponse(countries));
            }
            catch (DataAccessExceptions ex)
            {
                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<List<ShippingTypeDto>>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting countries");
                var error = new Error("E001", "An error occurred");
                return ApiResponse<List<ShippingTypeDto>>.FailureResponse("An error occurred", new List<Error> { error });
            }
        }

        [HttpGet("{id}")]
   
        public async Task<ActionResult<ApiResponse<ShippingTypeDto>>> GetById(Guid id)
        {
            try
            {
                var city = await _shippingTypes.GetById(id);
                if (city == null)
                {
                    var error = new Error("E003", "ShippingType not found");
                    return ApiResponse<ShippingTypeDto>.FailureResponse("ShippingType not found", new List<Error> { error });
                }
                return Ok(ApiResponse<ShippingTypeDto>.SuccessResponse(city));
            }
            catch (DataAccessExceptions ex)
            {

                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<ShippingTypeDto>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ShippingType with ID {id}", id);
                var error = new Error("E001", "An error occurred");
                return ApiResponse<ShippingTypeDto>.FailureResponse("An error occurred", new List<Error> { error });
            }
        }
    }
}