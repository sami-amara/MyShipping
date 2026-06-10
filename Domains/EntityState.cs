namespace Domains
{
    /// <summary>
    /// Represents the lifecycle state of an entity in the system.
    /// Note: This is NOT the same as shipment business status (Created, Approved, etc.)
    /// which is tracked separately in ShipmentStatusEnum.
    /// </summary>
    public enum EntityState
    {
        /// <summary>
        /// Entity is marked as deleted (soft delete).
        /// Should use IsDeleted field instead of this value in CurrentState.
        /// </summary>
        Deleted = 0,

        /// <summary>
        /// Entity is active and available for normal operations.
        /// This is the default state for new entities.
        /// </summary>
        Active = 1,

        /// <summary>
        /// Entity is archived but not deleted.
        /// Can be used for entities that are no longer in active use
        /// but should be preserved for historical/audit purposes.
        /// </summary>
        Archived = 2
    }
}
