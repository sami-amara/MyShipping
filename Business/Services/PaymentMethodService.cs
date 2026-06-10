using Business.Contracts;
using Business.Services;
using Business.DTOS;
using DataAccessLayer.Contracts;
using DataAccessLayer.Repositories;
using Domains;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Business.Services
{
    public class PaymentMethodService : BaseService<TbPaymentMethod, PaymentMethodDto>, IPaymentMethods
    {
        private readonly IGenericRepository<TbPaymentMethod> _repository;
        private readonly IMapper _mapper;

        public PaymentMethodService(IGenericRepository<TbPaymentMethod> repository, IMapper mapper, IUserService userService) : base(repository, mapper, userService)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<PaymentMethodDto>> GetActivePaymentMethods()
        {
            var activeMethods = await _repository.GetList(
                filter: p => p.CurrentState > 0,
                selector: p => p,
                orderBy: p => p.MethodEname ?? p.MethdAname,
                isDescending: false).ConfigureAwait(false);

            return _mapper.Map<List<PaymentMethodDto>>(activeMethods ?? new List<TbPaymentMethod>());
        }

        public async Task<bool> IsPaymentMethodActive(Guid paymentMethodId)
        {
            if (paymentMethodId == Guid.Empty) return false;

            var paymentMethod = await _repository.GetById(paymentMethodId).ConfigureAwait(false);
            return paymentMethod != null && paymentMethod.CurrentState > 0;
        }

        public async Task<decimal> CalculateTotalWithCommission(Guid paymentMethodId, decimal shippingRate)
        {
            var paymentMethod = await _repository.GetById(paymentMethodId).ConfigureAwait(false);
            if (paymentMethod == null)
            {
                throw new ArgumentException($"Payment method with ID {paymentMethodId} not found.");
            }

            if (paymentMethod.CurrentState <= 0)
            {
                throw new ArgumentException($"Payment method with ID {paymentMethodId} is inactive.");
            }

            // Calculate commission amount
            var commissionRate = paymentMethod.Commission ?? 0;
            var commissionAmount = shippingRate * (decimal)(commissionRate / 100);
            var totalAmount = shippingRate + commissionAmount;

            return totalAmount;
        }

        public async Task<PaymentMethodDto> GetPaymentMethodById(Guid id)
        {
            var paymentMethod = await _repository.GetById(id).ConfigureAwait(false);
            if (paymentMethod == null)
            {
                return null;
            }

            return _mapper.Map<PaymentMethodDto>(paymentMethod);
        }
    }
}
