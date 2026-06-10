using Business.Contracts;
using Business.DTOS;
using DataAccessLayer.Exceptions;
using Domains;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    

    public class CitiesController : Controller
    {

        private readonly ILogger<CitiesController> _logger;
        private readonly ICity _city;
        public CitiesController(ICity city, ILogger<CitiesController> logger)
        {
            _city = city;
            _logger = logger;
        }


        [HttpGet]
       
        public async Task<ActionResult<ApiResponse<List<CityDto>>>> Get()
        {
            try
            {
                var countries = await _city.GetAllCitites();
                if (countries == null || countries.Count == 0)
                {
                    var error = new Error("E003", "Countries not found");
                    return ApiResponse<List<CityDto>>.FailureResponse("Countries not found", new List<Error> { error });
                }
                return Ok(ApiResponse<List<CityDto>>.SuccessResponse(countries));
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
                return ApiResponse<List<CityDto>>.FailureResponse("An error occurred", new List<Error> { error });
            }
        }
       
        [HttpGet("{id}")]
       
        public async Task<ActionResult<ApiResponse<CityDto>>> GetById(Guid id)
        {
            try
            {
                var city = await _city.GetById(id);
                if (city == null)
                {
                    var error = new Error("E003", "City not found");
                    return ApiResponse<CityDto>.FailureResponse("City not found", new List<Error> { error });
                }
                return Ok(ApiResponse<CityDto>.SuccessResponse(city));
            }
            catch (DataAccessExceptions ex)
            {
                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<CityDto>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting city with ID {id}", id);
                var error = new Error("E001", "An error occurred");
                return ApiResponse<CityDto>.FailureResponse("An error occurred", new List<Error> { error });
            }
        }


        [HttpGet("GetByCountryId/{id}")]
        
        public async Task<ActionResult<ApiResponse<List<CityDto>>>> GetByCountryId(Guid id)
        {
            try
            {
                var cities = await _city.GetByCountryId(id);
                if (cities == null || cities.Count == 0)
                {
                    var error = new Error("E003", "City not found");
                    return ApiResponse<List<CityDto>>.FailureResponse("City not found", new List<Error> { error });
                }

                return Ok(ApiResponse<List<CityDto>>.SuccessResponse(cities));
            }
            catch (DataAccessExceptions ex)
            {
                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<List<CityDto>>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting city with ID {id}", id);
                var error = new Error("E001", "An error occurred");
                return ApiResponse<List<CityDto>>.FailureResponse("An error occurred", new List<Error> { error });
            }
        }
    }
}
