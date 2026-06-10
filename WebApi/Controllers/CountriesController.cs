using Business.Contracts;
using Business.DTOS;
using DataAccessLayer.Contracts;
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
  

    public class CountriesController : ControllerBase
    {

        private readonly ILogger<CountriesController> _logger;
        private readonly ICountry _country;
        public CountriesController(ICountry country, ILogger<CountriesController> logger)
        {
             _country = country;
             _logger = logger;
        }


        [HttpGet]
       
        public ActionResult<ApiResponse<List<CountryDto>>> Get()
        {
            try
            {
                var countries = _country.GetAll().Result; // Await or .Result to get List<CountryDto> from Task<List<CountryDto>>
                if (countries == null || countries.Count == 0)
                {
                    var error = new Error("E003", "Countries not found");
                    return ApiResponse<List<CountryDto>>.FailureResponse("Countries not found", new List<Error> { error });
                }
                return Ok(ApiResponse<List<CountryDto>>.SuccessResponse(countries));
            }
            catch (DataAccessExceptions ex)
            {
                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<List<CountryDto>>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting countries");
                var error = new Error("E001", "An error occurred");
                return ApiResponse<List<CountryDto>>.FailureResponse("An error occurred", new List<Error> { error });
            }
        }

        [HttpGet("{id}")]
       
        public ActionResult<ApiResponse<CountryDto>> GetById(Guid id)
        {
            try
            {
                var city = _country.GetById(id).Result; // Await or .Result to get CountryDto from Task<CountryDto>
                if (city == null)
                {
                    var error = new Error("E003", "Country not found");
                    return ApiResponse<CountryDto>.FailureResponse("Country not found", new List<Error> { error });
                }
                return Ok(ApiResponse<CountryDto>.SuccessResponse(city));
            }
            catch (DataAccessExceptions ex)
            {

                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<CountryDto>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting country with ID {id}", id);
                var error = new Error("E001", "An error occurred");
                return ApiResponse<CountryDto>.FailureResponse("An error occurred", new List<Error> { error });
            }
        }

    }
}









