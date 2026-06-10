using Business.Contracts.Shipment;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services.Shipment.ManageShipmentsState
{
    public class ShipmentStateHandlerFactory : IShipmentStateHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ShipmentStateHandlerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IShipmentStateHandler GetHandler(ShipmentStatusEnum status)
        {
            return status switch
            {
                ShipmentStatusEnum.Created          => _serviceProvider.GetRequiredService<CreatedShipment>(),
                ShipmentStatusEnum.Approved         => _serviceProvider.GetRequiredService<ApproveShipment>(),
                ShipmentStatusEnum.Updated          => _serviceProvider.GetRequiredService<UpdateShipment>(),
                ShipmentStatusEnum.ReadyForShipping => _serviceProvider.GetRequiredService<ReadyForShippingShipment>(),
                ShipmentStatusEnum.Shipped          => _serviceProvider.GetRequiredService<ShippedShipment>(),
                ShipmentStatusEnum.Delivered        => _serviceProvider.GetRequiredService<DelivredShipment>(),
                ShipmentStatusEnum.Cancelled        => _serviceProvider.GetRequiredService<CancelledShipment>(),
                ShipmentStatusEnum.Returned         => _serviceProvider.GetRequiredService<ReturnedShipment>(),
                ShipmentStatusEnum.Deleted          => _serviceProvider.GetRequiredService<DeletedShipment>(),

                _ => throw new NotImplementedException($"No handler implemented for state: {status}")
            };
        }
    }
}
