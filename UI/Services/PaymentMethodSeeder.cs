using DataAccessLayer.DbContext;
using Domains;
using Microsoft.EntityFrameworkCore;

namespace UI.Services
{
    public static class PaymentMethodSeeder
    {
        public static async Task SeedAsync(ShippingContext context)
        {
            var seedData = new List<TbPaymentMethod>
            {
                new TbPaymentMethod
                {
                    Id = new Guid("11111111-1111-1111-1111-111111111111"),
                    MethdAname = "سترايب",
                    MethodEname = "Stripe",
                    PaymentMethodToken = null,
                    Commission = 0,
                    CurrentState = 0,
                    CreatedDate = new DateTime(2025, 1, 1),
                    CreatedBy = Guid.Empty
                },
                new TbPaymentMethod
                {
                    Id = new Guid("22222222-2222-2222-2222-222222222222"),
                    MethdAname = "باي بال",
                    MethodEname = "PayPal",
                    PaymentMethodToken = null,
                    Commission = 0,
                    CurrentState = 1,
                    CreatedDate = new DateTime(2025, 1, 1),
                    CreatedBy = Guid.Empty
                },
                new TbPaymentMethod
                {
                    Id = new Guid("33333333-3333-3333-3333-333333333333"),
                    MethdAname = "فيزا",
                    MethodEname = "Visa",
                    PaymentMethodToken = "pm_card_visa",
                    Commission = 0,
                    CurrentState = 1,
                    CreatedDate = new DateTime(2025, 1, 1),
                    CreatedBy = Guid.Empty
                },
                new TbPaymentMethod
                {
                    Id = new Guid("44444444-4444-4444-4444-444444444444"),
                    MethdAname = "ماستركارد",
                    MethodEname = "MasterCard",
                    PaymentMethodToken = "pm_card_mastercard",
                    Commission = 0,
                    CurrentState = 1,
                    CreatedDate = new DateTime(2025, 1, 1),
                    CreatedBy = Guid.Empty
                },
                new TbPaymentMethod
                {
                    Id = new Guid("55555555-5555-5555-5555-555555555555"),
                    MethdAname = "أمريكان إكسبريس",
                    MethodEname = "American Express",
                    PaymentMethodToken = "pm_card_amex",
                    Commission = 0,
                    CurrentState = 1,
                    CreatedDate = new DateTime(2025, 1, 1),
                    CreatedBy = Guid.Empty
                },
                new TbPaymentMethod
                {
                    Id = new Guid("66666666-6666-6666-6666-666666666666"),
                    MethdAname = "ديسكفر",
                    MethodEname = "Discover",
                    PaymentMethodToken = "pm_card_discover",
                    Commission = 0,
                    CurrentState = 1,
                    CreatedDate = new DateTime(2025, 1, 1),
                    CreatedBy = Guid.Empty
                }
            };

            var existing = await context.TbPaymentMethods.ToListAsync();

            foreach (var item in seedData)
            {
                var current = existing.FirstOrDefault(x => x.Id == item.Id);
                if (current == null)
                {
                    context.TbPaymentMethods.Add(item);
                    continue;
                }

                current.MethdAname = item.MethdAname;
                current.MethodEname = item.MethodEname;
                current.PaymentMethodToken = item.PaymentMethodToken;
                current.Commission = item.Commission;
                current.CurrentState = item.CurrentState;
                current.UpdatedDate = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }
    }
}
