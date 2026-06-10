using Business.Contracts;
using DataAccessLayer.Contracts;
using DataAccessLayer.Repositories;
using Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Business.DTOS;
using AutoMapper;
using Business.Contracts.Shipment;


namespace Business.Services.Shipment
{
    public class TrackingNumberCreatorService : ITrackingNumberCreator
    {
        // Prefix: year (2 digits) + day-of-year (3 digits) = 5-digit date stamp
        // Suffix: 7 random digits
        // Total: 12-digit numeric tracking number e.g. 25031_4829371
        private static readonly Random _random = new Random();
        public double GenerateTrackingNumber(ShippmentDto shippment)
        {
            var now = DateTime.UtcNow;

            // Date component: YY + DDD (e.g. 25 + 031 ? 25031);
            int year        = now.Year % 100;               // 2-digit year
            int dayOfYear   = now.DayOfYear;                // 1–366
            long datePart   = (year * 1000L) + dayOfYear;  // e.g. 25031

            // Random component: 7 digits (1000000 – 9999999);
            int randomPart = _random.Next(1_000_000, 9_999_999);
            // Combine: datePart (5 digits) × 10^7 + randomPart (7 digits) = 12 digits
            long trackingNumber = (datePart * 10_000_000L) + randomPart;

            return (double)trackingNumber;
        }
    }
}
