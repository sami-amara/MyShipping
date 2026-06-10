using System;

namespace Business.DTOS
{
    /// <summary>
    /// DTO for payment method data transfer.
    /// </summary>
    public partial class PaymentMethodDto : Business.DTOS.BaseDto
    {
        public string? MethdAname { get; set; }
        public string? MethodEname { get; set; }
        public string? PaymentMethodToken { get; set; }
        public double? Commission { get; set; }
    }
}
