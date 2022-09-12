namespace HephUnity.Inventory
{
    /// <summary>
    /// Specifies the maximum number of elements that can be stacked in an <seealso cref="IInventoryItem"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class MaxStackCountAttribute : Attribute
    {
        public int MaxStackCount { get; }
        public MaxStackCountAttribute(int maxStackCount) => MaxStackCount = maxStackCount;
    }
}