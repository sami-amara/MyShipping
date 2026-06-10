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
    

    public class ShippingPackagController : ControllerBase
    {
        private readonly ILogger<ShippingPackagController> _logger;
        private readonly IShippingPackage _shipingPackage;
        public ShippingPackagController(IShippingPackage shippingTypes, ILogger<ShippingPackagController> logger)
        {
             _shipingPackage = shippingTypes;
            _logger = logger;
        }


        [HttpGet]
     
        public async Task<ActionResult<ApiResponse<List<ShipingPackgingDto>>>> Get()
        {
            try
            {
                var countries = await _shipingPackage.GetAll();
                if (countries == null || countries.Count == 0)
                {
                    var error = new Error("E003", "Countries not found");
                    return ApiResponse<List<ShipingPackgingDto>>.FailureResponse("Countries not found", new List<Error> { error });
                }
                return Ok(ApiResponse<List<ShipingPackgingDto>>.SuccessResponse(countries));
            }
            catch (DataAccessExceptions ex)
            {
                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<List<ShipingPackgingDto>>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting countries");
                var error = new Error("E001", "An error occurred");
                return ApiResponse<List<ShipingPackgingDto>>.FailureResponse("An error occurred", new List<Error> { error });
            }
        }

        [HttpGet("{id}")]
        
        public async Task<ActionResult<ApiResponse<ShipingPackgingDto>>> GetById(Guid id)
        {
            try
            {
                var city = await _shipingPackage.GetById(id);
                if (city == null)
                {
                    var error = new Error("E003", "ShippingType not found");
                    return ApiResponse<ShipingPackgingDto>.FailureResponse("ShippingType not found", new List<Error> { error });
                }
                return Ok(ApiResponse<ShipingPackgingDto>.SuccessResponse(city));
            }
            catch (DataAccessExceptions ex)
            {
                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<ShipingPackgingDto>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ShippingType with ID {id}", id);
                var error = new Error("E001", "An error occurred");
                return ApiResponse<ShipingPackgingDto>.FailureResponse("An error occurred", new List<Error> { error });
            }
        }



    }
}