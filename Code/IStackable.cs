namespace HephUnity.Inventory
{
    /// <summary>
    /// Provides fields and methods for an item to be stackable in an <seealso cref="Inventory"/>.
    /// </summary>
    public interface IStackable : IInventoryItem
    {
        /// <summary>
        /// Specifies the maximum number of items a stack can contain.
        /// </summary>
        public int MaxStackCount { get; }
        /// <summary>
        /// Checks whether the <paramref name="itemToAdd"/> can be added to the current stack.
        /// </summary>
        /// <param name="itemToAdd">The item which will be added to the current stack.</param>
        /// <returns>true if the <paramref name="itemToAdd"/> can be added to the stack; otherwise, false.</returns>
        public bool CanAddToStack(IStackable itemToAdd);
    }
}