using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Exceptions
{

    //csharp DataAccessLayer\Exceptions\DataAccessExceptions.cs
    public class DataAccessExceptions : Exception
    {
        public DataAccessExceptions(Exception ex, string customMessage, ILogger logger)
            : base(customMessage, ex) // preserve inner exception
        {
            logger.LogError(ex, $"main exception {ex.Message} developer custom exception {customMessage}");
        }
    }

    //public class DataAccessExceptions : Exception
    //{

    //    public DataAccessExceptions(Exception ex, string customMessage, ILogger logger)
    //    {
    //        logger.LogError($"main exception {ex.Message} developer custom exception " +
    //            $"{customMessage}");
    //    }
    //}
}
