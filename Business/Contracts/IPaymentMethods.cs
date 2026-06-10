using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Business.DTOS;
using DataAccessLayer.Contracts;
using Domains;

namespace Business.Contracts
{
    public interface IPaymentMethods : IBaseService<TbPaymentMethod, PaymentMethodDto>
    {
        //Now add
        Task<List<PaymentMethodDto>> GetActivePaymentMethods();
        Task<bool> IsPaymentMethodActive(Guid paymentMethodId);


        Task<decimal> CalculateTotalWithCommission(Guid paymentMethodId, decimal shippingRate);
        Task<PaymentMethodDto> GetPaymentMethodById(Guid id);
    }
}
