using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace UI.Services
{
    public class GenericApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<GenericApiClient> _logger;

        public GenericApiClient(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<GenericApiClient> logger)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;

            var baseUrl = configuration["ApiSettings:BaseUrl"];
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        // ✅ NEW: Attach token before every request
        private void AttachAuthorizationHeader()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                // Extract AccessToken from claims
                var token = httpContext.User.FindFirst("AccessToken")?.Value;

                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);

                    _logger.LogDebug("✅ AccessToken attached to request");
                }
                else
                {
                    _logger.LogDebug("⚠️ User authenticated but AccessToken claim is MISSING");
                }
            }
            else
            {
                _logger.LogDebug("No authentication - anonymous request");
            }
        }

        // Generic method for GET requests
        public async Task<T> GetAsync<T>(string endpoint)
        {
            AttachAuthorizationHeader();  // ✅ Attach token

            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("📥 API GET Response from {Endpoint}", endpoint);

            return JsonConvert.DeserializeObject<T>(responseData);
        }

        // Generic method for POST requests
        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            AttachAuthorizationHeader();  // ✅ Attach token

            var content = new StringContent(
                JsonConvert.SerializeObject(data),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ API Error: {StatusCode}, Response: {Response}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Error {response.StatusCode}: {errorContent}");
            }

            var responseData = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("📥 API POST Response from {Endpoint}", endpoint);

            return JsonConvert.DeserializeObject<T>(responseData);
        }

        // Generic method for PUT requests
        public async Task<T> PutAsync<T>(string endpoint, object data)
        {
            AttachAuthorizationHeader();  // ✅ Attach token

            var content = new StringContent(
                JsonConvert.SerializeObject(data),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PutAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseData);
        }

        // Generic method for DELETE requests
        public async Task DeleteAsync(string endpoint)
        {
            AttachAuthorizationHeader();  // ✅ Attach token

            var response = await _httpClient.DeleteAsync(endpoint);
            response.EnsureSuccessStatusCode();
        }
    }
}



