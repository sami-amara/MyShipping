using System;
using System.Collections.Generic;
using Domains;

namespace Domains;

public partial class TbUserSubscription : BaseTable
{
   

    public Guid UserId { get; set; }

    public Guid PackageId { get; set; }

    public DateTime SubscriptionDate { get; set; }

 

    public virtual TbSubscriptionPackage Package { get; set; } = null!;
}
