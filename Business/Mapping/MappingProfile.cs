using AutoMapper;
using Business.DTOS;
using Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Business.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<TbCarrier, CarrierDto>().ReverseMap();
            CreateMap<TbCity, CityDto>().ReverseMap();
            CreateMap<VwCities, CityDto>().ReverseMap();
            CreateMap<TbCountry, CountryDto>().ReverseMap();
            CreateMap<TbPaymentMethod, PaymentMethodDto>().ReverseMap();

            CreateMap<TbPaymentTransaction, PaymentTransactionDto>();
            CreateMap<PaymentTransactionDto, TbPaymentTransaction>()
                .ForMember(dest => dest.Shipment, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore());

            CreateMap<TbRefreshToken, RefreshTokenDto>().ReverseMap();
            CreateMap<TbSetting, SettingsDto>().ReverseMap();
            CreateMap<TbShippingType, ShippingTypeDto>().ReverseMap();

            // Explicitly customize reverse mapping so DTO->Entity ignores navigations
            CreateMap<TbShippment, ShippmentDto>()
                .ReverseMap()
                .ForMember(dest => dest.Sender, opt => opt.Ignore())
                .ForMember(dest => dest.Receiver, opt => opt.Ignore());

            CreateMap<TbShippmentStatus, ShippmentStatusDto>().ReverseMap();
            CreateMap<TbSubscriptionPackage, SubscriptionPackageDto>().ReverseMap();
            CreateMap<TbUserSender, UserSenderDto>().ReverseMap();
            CreateMap<TbUserReceiver, UserReceiverDto>().ReverseMap();
            CreateMap<TbUserSubscription, UserSubscriptionDto>().ReverseMap();
            CreateMap<TbShipingPackging, ShipingPackgingDto>().ReverseMap();
            CreateMap<TbCarrier, CarrierDto>().ReverseMap();
        }
    }
}


















//using AutoMapper;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Business.DTOS;
//using Domains;

//namespace Business.Mapping
//{
//    public class MappingProfile : Profile
//    {
//        public MappingProfile() 
//        {
//            CreateMap<TbCarrier, CarrierDto>().ReverseMap();
//            CreateMap<TbCity, CityDto>().ReverseMap();
//            CreateMap<VwCities, CityDto>().ReverseMap();
//            CreateMap<TbCountry, CountryDto>().ReverseMap();
//            CreateMap<TbPaymentMethod, PaymentMethodDto>().ReverseMap();
//            CreateMap<TbRefreshToken, RefreshTokenDto>().ReverseMap();
//            CreateMap<TbSetting, SettingsDto>().ReverseMap();
//            CreateMap<TbShippingType, ShippingTypeDto>().ReverseMap();
//            CreateMap<TbShippment, ShippmentDto>().ReverseMap();
//            CreateMap<TbShippmentStatus, ShippmentStatusDto>().ReverseMap();
//            CreateMap<TbSubscriptionPackage, SubscriptionPackageDto>().ReverseMap();
//            CreateMap<TbUserSender, UserSenderDto>().ReverseMap();
//            CreateMap<TbUserReceiver, UserReceiverDto>().ReverseMap();
//            CreateMap<TbUserSubscription, UserSubscriptionDto>().ReverseMap();
//            CreateMap<TbShipingPackging, ShipingPackgingDto>().ReverseMap();
//            CreateMap<TbCarrier, CarrierDto>().ReverseMap();

//            //CreateMap<TbUserSender, UserSenderDto>();
//            //CreateMap<TbUserReceiver, UserReceiverDto>();

//            //// Map TbShippment -> ShippmentDto including numeric + human-friendly status.
//            //CreateMap<TbShippment, ShippmentDto>()
//            //    .ForMember(dest => dest.UserSender, opt => opt.MapFrom(src => src.Sender))
//            //    .ForMember(dest => dest.UserReceiver, opt => opt.MapFrom(src => src.Receiver))
//            //    // numeric current state from most recent status record (or 0)
//            //    .ForMember(dest => dest.CurrentState, opt => opt.MapFrom(src =>
//            //        src.TbShippmentStatuses
//            //           .OrderByDescending(st => st.CreatedDate)
//            //           .Select(st => (int?)st.CurrentState)
//            //           .FirstOrDefault() ?? 0))
//            //    // human friendly status string
//            //    .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
//            //        src.TbShippmentStatuses
//            //           .OrderByDescending(st => st.CreatedDate)
//            //           .Select(st => st.CurrentState == 1 ? "ActiveFROMMAP" : "InactiveFROMMAP")
//            //           .FirstOrDefault()));

//        }
//    }
//}

