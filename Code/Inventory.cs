using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HephUnity.Inventory
{
    /// <summary>
    /// A class for storing and managing inventory items.
    /// </summary>
    public sealed class Inventory : IEnumerable<IInventoryItem>
    {
        private struct InventorySlot
        {
            public IInventoryItem? StoredItem { get; set; }
            public uint StackCount { get; set; }
            /// <summary>
            /// The index of the slot which the item is stored.
            /// </summary>
            public uint? ItemSlotIndex { get; set; }
        }
        /// <summary>
        /// The number of rows that the inventory contains.
        /// </summary>
        public uint RowCount { get; private set; }
        /// <summary>
        /// The number of columns that the inventory contains.
        /// </summary>
        public uint ColCount { get; private set; }
        /// <summary>
        /// The number of items the inventory consists of.
        /// </summary>
        public uint ItemCount { get; private set; }
        private InventorySlot[] slots;
        /// <summary>
        /// Initializes a new instance of the <seealso cref="Inventory"/> class that is empty and has no capacity.
        /// </summary>
        public Inventory() : this(0u, 0u) { }
        /// <summary>
        /// Initializes a new instance of the <seealso cref="Inventory"/> class that is empty and has the capacity of <paramref name="rowCount"/> * <paramref name="colCount"/>.
        /// </summary>
        /// <param name="rowCount">The number of rows that the inventory contains.</param>
        /// <param name="colCount">The number of columns that the inventory contains.</param>
        public Inventory(uint rowCount, uint colCount)
        {
            RowCount = rowCount;
            ColCount = colCount;
            slots = new InventorySlot[RowCount * ColCount];
        }
        public IInventoryItem this[uint row, uint col]
        {
            get
            {
                if (row > RowCount) throw new IndexOutOfRangeException("row must be lesser than or equal to RowCount");
                if (col > ColCount) throw new IndexOutOfRangeException("col must be lesser than or equal to ColCount");
                return slots[GetItemSlotIndex(ref row, ref col)].StoredItem;
            }
            set => InternalAdd(ref value, ref row, ref col);
        }
        /// <summary>
        /// Adds a new item if there is available space.
        /// </summary>
        /// <param name="item">The item which will be added.</param>
        public void Add(IInventoryItem item)
        {
            if (item != null)
            {
                uint i;
                for (i = 0u; i < slots.Length; i++) // if the inventory already contains the item and there is enough space in the stack, just increase the stack.
                {
                    if (slots[i].StoredItem != null)
                    {
                        if (CanIncreaseStack(ref item, ref slots[i]))
                        {
                            slots[i].StackCount++;
                            return;
                        }
                        i += slots[i].StoredItem.ColSize - 1u;
                    }
                    else if (slots[i].ItemSlotIndex is uint slotIndex)
                    {
                        i += slots[slotIndex].StoredItem.ColSize - 1u;
                    }
                }
                if (item.RowSize > RowCount) throw new IndexOutOfRangeException("item's row size must be lesser than or equal to RowCount");
                if (item.ColSize > ColCount) throw new IndexOutOfRangeException("item's col size must be lesser than or equal to ColCount");
                uint rowEnd = RowCount - item.RowSize;
                uint colEnd = ColCount - item.ColSize;
                for (i = 0u; i < rowEnd; i++)
                {
                    for (uint j = 0u; j < colEnd; j++)
                    {
                        if (InternalAdd(ref item, ref i, ref j))
                        {
                            return;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Adds a new item to the desired row and column.
        /// </summary>
        /// <param name="item">The item which will be added.</param>
        /// <param name="row">A zero-based number that specifies at which row the item will be added to.</param>
        /// <param name="col">A zero-based number that specifies at which column the item will be added to.</param>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public void Add(IInventoryItem item, uint row, uint col) => InternalAdd(ref item, ref row, ref col);
        /// <summary>
        /// Finds and removes the <paramref name="item"/>.
        /// </summary>
        /// <param name="item">The item which will be removed.</param>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public void Remove(IInventoryItem item)
        {
            if (item != null)
            {
                for (uint i = 0u; i < slots.Length; i++)
                {
                    if (slots[i].StoredItem != null && slots[i].StoredItem.Equals(item))
                    {
                        InternalRemove(ref i, false);
                        return;
                    }
                }
            }
        }
        /// <summary>
        /// Removes the item at the desired row and column.
        /// </summary>
        /// <param name="row">A zero-based number that specifies at which row the item to remove is.</param>
        /// <param name="col">A zero-based number that specifies at which column the item to remove is.</param>
        /// <returns>If any item is found and removed, the item; otherwise, null.</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public IInventoryItem Remove(uint row, uint col)
        {
            if (row > RowCount) throw new IndexOutOfRangeException("row must be lesser than or equal to RowCount");
            if (col > ColCount) throw new IndexOutOfRangeException("col must be lesser than or equal to ColCount");
            uint index = GetItemSlotIndex(ref row, ref col);
            IInventoryItem removedItem = slots[index].StoredItem;
            InternalRemove(ref index, false);
            return removedItem;
        }
        /// <summary>
        /// Changes the capacity of the inventory to <paramref name="newRowCount"/> * <paramref name="newColCount"/>
        /// </summary>
        /// <param name="newRowCount">The number of rows that the inventory will contain.</param>
        /// <param name="newColCount">The number of columns that the inventory will contain.</param>
        public void Resize(uint newRowCount, uint newColCount)
        {
            uint newSize = newRowCount * newColCount;
            uint oldSize = RowCount * ColCount;
            uint minSize = oldSize;
            uint i;
            if (newSize < oldSize) // new size is smaller than the old size, hence we need to remove every item that occupies a slot that no longer exists.
            {
                minSize = newSize;
                uint index;
                for (i = newSize; i < oldSize; i++)
                {
                    index = GetItemSlotIndex(ref i);
                    InternalRemove(ref index, true);
                }
            }
            RowCount = newRowCount;
            ColCount = newColCount;
            InventorySlot[] tempSlots = new InventorySlot[newSize];
            for (i = 0u; i < minSize; i++) // Copy the slots to the temp slots.
            {
                tempSlots[i] = slots[i];
            }
            slots = tempSlots;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Suggest the compiler to make this method inline. You can think of this as the inline keyword in C++.
        private uint ToIndex(ref uint row, ref uint col) => row * ColCount + col;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint GetItemSlotIndex(ref uint index) => slots[index].ItemSlotIndex ?? index;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint GetItemSlotIndex(ref uint row, ref uint col)
        {
            uint index = ToIndex(ref row, ref col);
            return slots[index].ItemSlotIndex ?? index;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint IndexToRow(ref uint index) => index / ColCount;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint IndexToCol(ref uint index) => index % ColCount;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanIncreaseStack(ref IInventoryItem value, ref InventorySlot slot) => 
                     slot.StoredItem.GetType() == value.GetType()
                     && slot.StoredItem.Equals(value)
                     && slot.StoredItem.GetType().GetCustomAttribute<MaxStackCountAttribute>() is MaxStackCountAttribute attribute
                     && attribute.MaxStackCount > slot.StackCount;
        private bool InternalAdd(ref IInventoryItem value, ref uint row, ref uint col)
        {
            if (value != null)
            {
                uint rowEnd = row + value.RowSize;
                uint colEnd = col + value.ColSize;
                if (rowEnd > RowCount) throw new IndexOutOfRangeException("row + item.RowSize must be lesser than or equal to RowCount");
                if (colEnd > ColCount) throw new IndexOutOfRangeException("col + item.ColSize must be lesser than or equal to ColCount");
                uint i, j;
                for (i = row; i < rowEnd; i++)
                {
                    for (j = col; j < colEnd; j++)
                    {
                        uint slotIndex = GetItemSlotIndex(ref i, ref j);
                        // if there is an item in the target slot and that item is not the item we are trying to add.
                        if (slots[slotIndex].StoredItem != null)
                        {
                            // if the item we are trying to add is same type as the current item in the slot and there is enough space in the stack, add to the stack.
                            if (CanIncreaseStack(ref value, ref slots[slotIndex]))
                            {
                                slots[slotIndex].StackCount++;
                                ItemCount++;
                                return true;
                            }
                            return false;
                        }
                    }
                }
                // all slots that we need are empty, add the item to the targeted slot.
                uint index = ToIndex(ref row, ref col);
                slots[index].StoredItem = value;
                slots[index].StackCount = 1u;
                for (i = row; i < rowEnd; i++) // set the other slots' ItemSlotIndex property.
                {
                    for (j = col; j < colEnd; j++)
                    {
                        slots[GetItemSlotIndex(ref i, ref j)].ItemSlotIndex = index;
                    }
                }
                ItemCount++;
                return true;
            }
            return false;
        }
        private void InternalRemove(ref uint index, bool removeAll)
        {
            if (slots[index].StoredItem != null)
            {
                uint row = IndexToRow(ref index);
                uint col = IndexToCol(ref index);
                if (removeAll || slots[index].StackCount == 0u || slots[index].StackCount == 1u)
                {
                    uint rowEnd = row + slots[index].StoredItem.RowSize;
                    uint colEnd = col + slots[index].StoredItem.ColSize;
                    if (rowEnd > RowCount) throw new IndexOutOfRangeException("row + item.RowSize must be lesser than or equal to RowCount");
                    if (colEnd > ColCount) throw new IndexOutOfRangeException("col + item.ColSize must be lesser than or equal to ColCount");
                    slots[index].StoredItem = null;
                    slots[index].StackCount = 0u;
                    for (uint i = row; i < rowEnd; i++)
                    {
                        for (uint j = col; j < colEnd; j++)
                        {
                            slots[ToIndex(ref i, ref j)].ItemSlotIndex = null;
                        }
                    }
                }
                else
                {
                    slots[index].StackCount--;
                }
                ItemCount--;
            }
        }
        /// <inheritdoc/>
        public IEnumerator<IInventoryItem> GetEnumerator()
        {
            for (uint i = 0u; i < slots.Length; i++)
            {
                if (slots[i].StoredItem != null)
                {
                    yield return slots[i].StoredItem;
                    i += slots[i].StoredItem.ColSize - 1u;
                }
                else if (slots[i].ItemSlotIndex is uint slotIndex)
                {
                    i += slots[slotIndex].StoredItem.ColSize - 1u; // skip the occupied slots with the items we have already returned.
                }
            }
        }
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}