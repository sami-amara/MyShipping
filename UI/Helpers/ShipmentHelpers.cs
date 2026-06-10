using Business.DTOS;
using Business.Services.Shipment.ManageShipmentsState;
using Microsoft.AspNetCore.Html;
using System.Text.Encodings.Web;

namespace UI.Helpers
{
    /// <summary>
    /// Helper methods for rendering shipment-related UI elements
    /// </summary>
    public static class ShipmentHelpers
    {
        /// <summary>
        /// Renders a Bootstrap badge for shipment status
        /// </summary>
        /// <param name="shipment">The shipment to display status for</param>
        /// <returns>HTML span element with badge styling</returns>
        public static IHtmlContent StatusBadge(ShippmentDto? shipment)
        {
            if (shipment == null)
            {
                return new HtmlString("<span class=\"badge bg-secondary\" style=\"display: inline-flex; align-items: center; gap: 4px;\"><i class=\"mdi mdi-help-circle\"></i>-</span>");
            }

            var (cssClass, icon, text) = GetStatusBadge(shipment);
            var encodedText = HtmlEncoder.Default.Encode(text);
            return new HtmlString(
                $"<span class=\"{cssClass}\" style=\"display: inline-flex; align-items: center; gap: 4px; justify-content: center;\">" +
                $"<i class=\"mdi {icon}\"></i>{encodedText}</span>");
        }

        /// <summary>
        /// Maps shipment status to badge CSS class and display text
        /// </summary>
        private static (string cls, string icon, string text) GetStatusBadge(ShippmentDto shipment)
        {
            var state = shipment.CurrentState;

            // Check if state is a valid enum value
            if (Enum.IsDefined(typeof(ShipmentStatusEnum), state))
            {
                var status = (ShipmentStatusEnum)state;
                return status switch
                {
                    ShipmentStatusEnum.Deleted          => ("badge bg-secondary",          "mdi-delete",              AppResource.Labels.Deleted          ?? "Deleted"),
                    ShipmentStatusEnum.Created          => ("badge bg-warning text-dark",  "mdi-file-document-outline", AppResource.Labels.Created        ?? "Created"),
                    ShipmentStatusEnum.Updated          => ("badge bg-info text-dark",     "mdi-update",              AppResource.Labels.Updated          ?? "Updated"),
                    ShipmentStatusEnum.Approved         => ("badge bg-primary",            "mdi-check-circle",        AppResource.Labels.Approved         ?? "Approved"),
                    ShipmentStatusEnum.ReadyForShipping => ("badge bg-info text-dark",     "mdi-package-variant",     AppResource.Labels.ReadyForShipping  ?? "Ready for Shipping"),
                    ShipmentStatusEnum.Shipped          => ("badge bg-primary",            "mdi-truck-delivery",      AppResource.Labels.Shipped           ?? "Shipped"),
                    ShipmentStatusEnum.Delivered        => ("badge bg-success",            "mdi-inbox-arrow-down",   AppResource.Labels.Delivered         ?? "Delivered"),
                    ShipmentStatusEnum.Cancelled        => ("badge bg-danger",             "mdi-close-circle",        AppResource.Labels.Cancelled         ?? "Cancelled"),
                    ShipmentStatusEnum.Returned         => ("badge bg-dark",               "mdi-keyboard-return",     AppResource.Labels.Returned          ?? "Returned"),
                    ShipmentStatusEnum.Refunded         => ("badge bg-secondary",          "mdi-cash-refund",         AppResource.Labels.Refunded          ?? "Refunded"),
                    _                                   => ("badge bg-info text-dark",     "mdi-help-circle",         status.ToString())
                };
            }

            // Fallback to Status property if CurrentState is not a valid enum
            var raw = (shipment.Status ?? string.Empty).Trim();
            return string.IsNullOrEmpty(raw)
                ? ("badge bg-secondary", "mdi-help-circle", "-")
                : ("badge bg-info text-dark", "mdi-help-circle", raw);
        }
    }
}