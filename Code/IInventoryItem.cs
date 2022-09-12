namespace HephUnity.Inventory
{
    /// <summary>
    /// Provides properties to help storing items in an inventory.
    /// </summary>
    public interface IInventoryItem 
    {
        /// <summary>
        /// Specifies the number of rows the item occupies when stored in an <seealso cref="Inventory"/>.
        /// </summary>
        public uint RowSize { get; set; }
        /// <summary>
        /// Specifies the number of columns the item occupies when stored in an <seealso cref="Inventory"/>.
        /// </summary>
        public uint ColSize { get; set; }
    }
}