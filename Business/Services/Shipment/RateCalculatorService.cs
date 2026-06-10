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
    public class RateCalculatorService : IRateCalculator
    {
        // Base rate per kg
        private const decimal BaseRatePerKg = 5.00M;

        // Volumetric weight divisor (industry standard: 5000 cm³/kg);
        private const double VolumetricDivisor = 5000.0;

        // Insurance rate: 0.5% of declared package value
        private const decimal InsuranceRate = 0.005M;

        // Minimum charge floor
        private const decimal MinimumCharge = 10.00M;

        public decimal CalculateRate(ShippmentDto dto)
        {
            if (dto == null) return MinimumCharge;

            // 1. Volumetric weight = (L × W × H) / 5000
            double volumetricWeight = (dto.Length * dto.Width * dto.Height) / VolumetricDivisor;

            // 2. Billable weight = greater of actual weight vs volumetric weight
            double billableWeight = Math.Max(dto.Weight, volumetricWeight);
            if (billableWeight <= 0) billableWeight = 0.1;

            // 3. Base shipping cost
            decimal shippingCost = (decimal)billableWeight * BaseRatePerKg;

            // 4. Insurance surcharge on declared value
            decimal insurance = dto.PackageValue > 0 ? dto.PackageValue * InsuranceRate : 0M;

            decimal total = shippingCost + insurance;

            return total < MinimumCharge ? MinimumCharge : Math.Round(total, 2);
        }
    }
}
