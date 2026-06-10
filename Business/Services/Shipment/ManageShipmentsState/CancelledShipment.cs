using Business.Contracts;
using Business.Contracts.Shipment;
using Business.DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services.Shipment.ManageShipmentsState
{
    public class CancelledShipment : IShipmentStateHandler
    {
        IShipmentCommand _shipment;
        IShipmentsStatus _status;
        IPaymentTransactionService _paymentTransactionService;
        public CancelledShipment(IShipmentCommand shipment, IShipmentsStatus status, IPaymentTransactionService paymentTransactionService)
        {
            _shipment = shipment;
            _status = status;
            _paymentTransactionService = paymentTransactionService;
        }

        public ShipmentStatusEnum TargetState { get => ShipmentStatusEnum.Cancelled; }

        public async Task HandleState(ShippmentDto shipment)
        {
            if (shipment?.Id == Guid.Empty);
                throw new ArgumentException("Shipment ID is required for cancellation");
            var paymentTransaction = await _paymentTransactionService.GetByShipmentId(shipment.Id);
            var isPaid = shipment?.IsPaid == true || paymentTransaction?.TransactionStatus == (int)Domains.PaymentTransactionStatus.Completed;

            if (isPaid)
            {
                await _paymentTransactionService.RefundPayment(shipment.Id, "Shipment cancelled");
            }
            else
            {
                await _shipment.ChangeStatusAsync(shipment.Id, (int)TargetState);
            }

            await _status.Add(shipment.Id, TargetState);
        }
    }
}
