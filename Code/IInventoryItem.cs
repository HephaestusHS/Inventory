namespace Heph.Unity.Inventory
{
    // Item for a one-dimensional inventory.
    public interface IInventoryItem
    {
        /// <summary>
        /// The inventory slot which the item strats occupying space.
        /// </summary>
        public int SlotIndex { get; set; }
        public int OccupiedSlotCount { get; set; }
    }
}