using Business.Contracts;
using Business.DTOS;
using Domains;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UI.Models;

namespace UI.Services
{
    /// <summary>
    /// ✅ AccountHelper: Extracts business logic from AccountController
    /// 
    /// Responsibilities:
    /// - Building dropdown options for settings pages (countries, cities, carriers, ShippingTypes, ShippingPackages.)
    /// - Creating view models from user DTOs
    /// - Localizing text based on current culture
    /// - Parsing and formatting validation errors
    /// </summary>
    public class AccountHelper
    {
        private readonly ICountry _countryService;
        private readonly ICity _cityService;
        private readonly ICarrier _carrierService;
        private readonly IShippingPackage _shippingPackageService;
        private readonly IShippingTypes _shippingTypeService;
        private readonly ILogger<AccountHelper> _logger;

        public AccountHelper(
            ICountry countryService,
            ICity cityService,
            ICarrier carrierService,
            IShippingPackage shippingPackageService,
            IShippingTypes shippingTypeService,
            ILogger<AccountHelper> logger)
        {
            _countryService = countryService;
            _cityService = cityService;
            _carrierService = carrierService;
            _shippingPackageService = shippingPackageService;
            _shippingTypeService = shippingTypeService;
            _logger = logger;
        }

        /// <summary>
        /// ✅ Populates all shipping preference options (countries, cities, carriers, packages, types)
        /// This is the main entry point for Settings page initialization
        /// </summary>
        public async Task PopulateShippingPreferenceOptionsAsync(AccountShippingPreferencesViewModel shippingPreferences)
        {
            _logger.LogInformation("📦 Populating shipping preferences options...");

            shippingPreferences.Countries = await BuildCountryOptionsAsync(shippingPreferences.DefaultCountryId);
            shippingPreferences.Cities = await BuildCityOptionsAsync(shippingPreferences.DefaultCountryId, shippingPreferences.DefaultCityId);
            shippingPreferences.Carriers = await BuildCarrierOptionsAsync(shippingPreferences.DefaultCarrierId);
            shippingPreferences.ShippingPackages = await BuildShippingPackageOptionsAsync(shippingPreferences.DefaultShippingPackageId);
            shippingPreferences.ShippingTypes = await BuildShippingTypeOptionsAsync(shippingPreferences.DefaultShippingTypeId);

            _logger.LogInformation("✅ Shipping preferences options populated successfully");
        }

        /// <summary>
        /// ✅ Creates a complete AccountSettingsPageViewModel from a UserDto
        /// Maps user data to settings page structure
        /// </summary>
        public AccountSettingsPageViewModel CreateSettingsPageModelFromUser(UserDto user)
        {
            _logger.LogInformation("🔄 Creating settings page model from user: {UserId}", user.Id);

            var currentLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return new AccountSettingsPageViewModel
            {
                Profile = new AccountSettingsViewModel
                {
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    Phone = user.Phone ?? string.Empty,
                    Language = (currentLang == "ar") ? "ar" : "en"
                },
                Security = new AccountSecuritySettingsViewModel(),
                Notifications = new AccountNotificationSettingsViewModel
                {
                    NotifyByEmail = user.NotifyByEmail,
                    NotifyBySms = user.NotifyBySms,
                    NotifyShipmentStatusUpdates = user.NotifyShipmentStatusUpdates,
                    NotifyMarketing = user.NotifyMarketing
                },
                ShippingPreferences = new AccountShippingPreferencesViewModel
                {
                    DefaultCountryId = user.DefaultCountryId,
                    DefaultCityId = user.DefaultCityId,
                    DefaultCarrierId = user.DefaultCarrierId,
                    DefaultShippingPackageId = user.DefaultShippingPackageId,
                    DefaultShippingTypeId = user.DefaultShippingTypeId
                },
                Privacy = new AccountPrivacySettingsViewModel()
            };
        }

        /// <summary>
        /// ✅ Generic method to build SelectListItem options from any enumerable collection
        /// Reduces code duplication across all Build*OptionsAsync methods
        /// </summary>
        private static List<SelectListItem> BuildSelectOptions<T>(
            IEnumerable<T> items,
            Guid? selectedId,
            Func<T, Guid> idSelector,
            Func<T, string> textSelector)
        {
            return items
                .Select(item =>
                {
                    var id = idSelector(item);
                    return new SelectListItem
                    {
                        Value = id.ToString(),
                        Text = textSelector(item),
                        Selected = selectedId.HasValue && selectedId.Value == id
                    };
                })
                .ToList();
        }

        /// <summary>
        /// ✅ Resolves localized text based on current UI culture
        /// Prefers current language, falls back to other language, then ID
        /// </summary>
        private static string ResolveLocalizedText(bool isArabic, string? arabicText, string? englishText, Guid fallbackId)
        {
            if (isArabic)
            {
                return arabicText ?? englishText ?? fallbackId.ToString();
            }

            return englishText ?? arabicText ?? fallbackId.ToString();
        }

        /// <summary>
        /// ✅ Builds carrier dropdown options
        /// Filters active carriers and sorts by name
        /// </summary>
        public async Task<List<SelectListItem>> BuildCarrierOptionsAsync(Guid? selectedId)
        {
            try
            {
                var items = await _carrierService.GetAll();
                return BuildSelectOptions(
                    items.Where(c => c.CurrentState > 0).OrderBy(c => c.CarrierName),
                    selectedId,
                    c => c.Id,
                    c => c.CarrierName ?? c.Id.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error building carrier options");
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// ✅ Builds shipping package dropdown options
        /// Respects localization (Arabic/English) and filters active packages
        /// </summary>
        public async Task<List<SelectListItem>> BuildShippingPackageOptionsAsync(Guid? selectedId)
        {
            try
            {
                var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
                var items = await _shippingPackageService.GetAll();
                return BuildSelectOptions(
                    items.Where(p => p.CurrentState > 0).OrderBy(p => isArabic ? p.TbShipingPackginAname : p.TbShipingPackginEname),
                    selectedId,
                    p => p.Id,
                    p => ResolveLocalizedText(isArabic, p.TbShipingPackginAname, p.TbShipingPackginEname, p.Id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error building shipping package options");
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// ✅ Builds shipping type dropdown options
        /// Respects localization (Arabic/English) and filters active types
        /// </summary>
        public async Task<List<SelectListItem>> BuildShippingTypeOptionsAsync(Guid? selectedId)
        {
            try
            {
                var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
                var items = await _shippingTypeService.GetAll();
                return BuildSelectOptions(
                    items.Where(t => t.CurrentState > 0).OrderBy(t => isArabic ? t.ShippingTypeAname : t.ShippingTypeEname),
                    selectedId,
                    t => t.Id,
                    t => ResolveLocalizedText(isArabic, t.ShippingTypeAname, t.ShippingTypeEname, t.Id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error building shipping type options");
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// ✅ Builds country dropdown options
        /// Sorts by localized country name (Arabic/English)
        /// </summary>
        public async Task<List<SelectListItem>> BuildCountryOptionsAsync(Guid? selectedId)
        {
            try
            {
                var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
                var items = await _countryService.GetAll();
                return BuildSelectOptions(
                    items.OrderBy(c => isArabic ? c.CountryAname : c.CountryEname),
                    selectedId,
                    c => c.Id,
                    c => ResolveLocalizedText(isArabic, c.CountryAname, c.CountryEname, c.Id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error building country options");
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// ✅ Builds city dropdown options (dependent on country selection)
        /// Returns empty list if no country is selected
        /// Sorts by localized city name (Arabic/English)
        /// </summary>
        public async Task<List<SelectListItem>> BuildCityOptionsAsync(Guid? countryId, Guid? selectedId)
        {
            try
            {
                var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

                if (!countryId.HasValue || countryId.Value == Guid.Empty)
                {
                    _logger.LogWarning("⚠️ BuildCityOptionsAsync called without valid countryId");
                    return new List<SelectListItem>();
                }

                var items = await _cityService.GetByCountryId(countryId.Value);
                return BuildSelectOptions(
                    items.OrderBy(c => isArabic ? c.CityAname : c.CityEname),
                    selectedId,
                    c => c.Id,
                    c => ResolveLocalizedText(isArabic, c.CityAname, c.CityEname, c.Id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error building city options for country: {CountryId}", countryId);
                return new List<SelectListItem>();
            }
        }

        /// <summary>
        /// ✅ Parses validation errors from service response and adds them to ModelState
        /// Expected format: "FieldName: Error message" or just "Error message"
        /// </summary>
        public void AddErrorsToModelState(ModelStateDictionary modelState, IEnumerable<string> errors)
        {
            if (errors == null) return;

            foreach (var error in errors)
            {
                var parts = error.Split(':', 2);
                if (parts.Length == 2)
                    modelState.AddModelError(parts[0].Trim(), parts[1].Trim());
                else
                    modelState.AddModelError(string.Empty, error);
            }
        }
        

    }
}