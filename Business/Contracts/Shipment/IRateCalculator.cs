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
    public interface IRateCalculator
    {
        public  decimal CalculateRate(ShippmentDto shippmentDto);
    }
}
