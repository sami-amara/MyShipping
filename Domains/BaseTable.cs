using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domains
{
    public class BaseTable
    {


        public Guid Id { get; set; }

        public Guid? UpdatedBy { get; set; }

        /// <summary>
        /// Represents the workflow/business state of the entity.
        /// For shipments: Created, Approved, Shipped, etc. (see ShipmentStatusEnum)
        /// Use IsDeleted for soft-delete tracking instead of CurrentState.
        /// </summary>
        public int  CurrentState{ get; set; }

        public DateTime CreatedDate { get; set; }

        public Guid CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// Soft delete flag. When true, the entity is considered deleted.
        /// Use this instead of CurrentState = 0 for delete operations.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// UTC timestamp when the entity was soft-deleted.
        /// </summary>
        public DateTime? DeletedDate { get; set; }

        /// <summary>
        /// User ID who performed the soft delete operation.
        /// </summary>
        public Guid? DeletedBy { get; set; }

        /// <summary>
        /// Concurrency token for optimistic concurrency control.
        /// EF Core will automatically manage this field when configured with [Timestamp] or fluent API.
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }
}
