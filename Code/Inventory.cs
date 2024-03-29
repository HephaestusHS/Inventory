﻿using System.Collections;
using System.Runtime.CompilerServices;

namespace HephUnity.Inventory
{
    /// <summary>
    /// A class for storing and managing inventory items.
    /// </summary>
    public sealed class Inventory : IEnumerable<IInventoryItem>
    {
        private class InventorySlot
        {
            /// <summary>
            /// The stored item, null if the slot is empty.
            /// </summary>
            public IInventoryItem? StoredItem { get; set; }
            /// <summary>
            /// The number of items in the stack.
            /// </summary>
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

                return slots[GetItemSlotIndex(ref row, ref col)]?.StoredItem;
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

                if (item is IStackable stackableValue)
                {
                    for (i = 0u; i < slots.Length; i++) // if the inventory already contains the item and there is enough space in the stack, just increase the stack.
                    {
                        InventorySlot slot = slots[i];
                        if (slot != null)
                        {
                            if (slot.StoredItem is IStackable stackableSlotItem)
                            {
                                if (stackableSlotItem.MaxStackCount > slot.StackCount && stackableSlotItem.GetType() == stackableValue.GetType() && stackableSlotItem.CanAddToStack(stackableValue))
                                {
                                    slot.StackCount++;
                                    return;
                                }

                                i += slot.StoredItem.ColSize - 1u;
                            }
                            else if (slot.ItemSlotIndex is uint slotIndex)
                            {
                                i += slots[slotIndex].StoredItem.ColSize - 1u;
                            }
                        }
                    }
                }

                if (item.RowSize > RowCount) throw new IndexOutOfRangeException("item's row size must be lesser than or equal to RowCount");
                if (item.ColSize > ColCount) throw new IndexOutOfRangeException("item's col size must be lesser than or equal to ColCount");

                uint rowEnd = RowCount - item.RowSize;
                uint colEnd = ColCount - item.ColSize;

                for (i = 0u; i <= rowEnd; i++)
                {
                    for (uint j = 0u; j <= colEnd; j++)
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
                    InventorySlot slot = slots[i];
                    if (slot != null && slot.StoredItem != null && slot.StoredItem.Equals(item))
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
            IInventoryItem removedItem = slots[index]?.StoredItem;
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
        public override string ToString()
        {
            string result = "";
            for (uint i = 0; i < RowCount; i++)
            {
                for (uint j = 0; j < ColCount; j++)
                {
                    result += this[i, j] != null ? "+" : "-";
                }
                result += "\n";
            }
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Suggest the compiler to make this method inline. You can think of this as the inline keyword in C++.
        private uint ToIndex(ref uint row, ref uint col) => row * ColCount + col;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint GetItemSlotIndex(ref uint index) => slots[index]?.ItemSlotIndex ?? index;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint GetItemSlotIndex(ref uint row, ref uint col)
        {
            uint index = ToIndex(ref row, ref col);
            return slots[index]?.ItemSlotIndex ?? index;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint IndexToRow(ref uint index) => index / ColCount;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint IndexToCol(ref uint index) => index % ColCount;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool InternalAdd(ref IInventoryItem value, ref uint row, ref uint col)
        {
            if (value != null)
            {
                uint rowEnd = row + value.RowSize;
                uint colEnd = col + value.ColSize;

                if (rowEnd > RowCount) throw new IndexOutOfRangeException("row + item.RowSize must be lesser than or equal to RowCount");
                if (colEnd > ColCount) throw new IndexOutOfRangeException("col + item.ColSize must be lesser than or equal to ColCount");

                uint i, j;
                if (value is IStackable stackableValue)
                {
                    for (i = row; i < rowEnd; i++)
                    {
                        for (j = col; j < colEnd; j++)
                        {
                            uint slotIndex = GetItemSlotIndex(ref i, ref j);
                            InventorySlot slot = slots[slotIndex];
                            // if there is an item in the target slot and that item is not the item we are trying to add.
                            if (slot != null && slot.StoredItem is IStackable stackableSlotItem)
                            {
                                // if the item we are trying to add is same type as the current item in the slot and there is enough space in the stack, add to the stack.
                                if (stackableSlotItem.MaxStackCount > slot.StackCount && stackableSlotItem.GetType() == stackableValue.GetType() && stackableSlotItem.CanAddToStack(stackableValue))
                                {
                                    slot.StackCount++;
                                    ItemCount++;
                                    return true;
                                }
                                return false;
                            }
                        }
                    }
                }

                // all slots that we need are empty, add the item to the targeted slot.
                uint index = ToIndex(ref row, ref col);
                slots[index] = new InventorySlot()
                {
                    StoredItem = value,
                    StackCount = 1u
                };

                uint currentIndex;
                for (i = row; i < rowEnd; i++) // set the other slots' ItemSlotIndex property.
                {
                    for (j = col; j < colEnd; j++)
                    {
                        currentIndex = GetItemSlotIndex(ref i, ref j);

                        if (slots[currentIndex] == null)
                        {
                            slots[currentIndex] = new InventorySlot()
                            {
                                ItemSlotIndex = index
                            };
                        }

                        slots[currentIndex].ItemSlotIndex = index;
                    }
                }

                ItemCount++;
                return true;
            }
            return false;
        }
        private void InternalRemove(ref uint index, bool removeAll)
        {
            InventorySlot slot = slots[index];
            if (slot != null && slot.StoredItem != null)
            {
                uint row = IndexToRow(ref index);
                uint col = IndexToCol(ref index);

                if (removeAll || slot.StackCount <= 1u)
                {
                    uint rowEnd = row + slot.StoredItem.RowSize;
                    uint colEnd = col + slot.StoredItem.ColSize;

                    if (rowEnd > RowCount) throw new IndexOutOfRangeException("row + item.RowSize must be lesser than or equal to RowCount");
                    if (colEnd > ColCount) throw new IndexOutOfRangeException("col + item.ColSize must be lesser than or equal to ColCount");

                    slots[index] = null;
                    for (uint i = row; i < rowEnd; i++)
                    {
                        for (uint j = col; j < colEnd; j++)
                        {
                            slots[ToIndex(ref i, ref j)] = null;
                        }
                    }
                }
                else
                {
                    slot.StackCount--;
                }
                ItemCount--;
            }
        }
        /// <inheritdoc/>
        public IEnumerator<IInventoryItem> GetEnumerator()
        {
            for (uint i = 0u; i < slots.Length; i++)
            {
                InventorySlot slot = slots[i];
                if (slot != null)
                {
                    if (slot.StoredItem != null)
                    {
                        yield return slot.StoredItem;
                        i += slot.StoredItem.ColSize - 1u;
                    }
                    else if (slot.ItemSlotIndex is uint slotIndex)
                    {
                        i += slots[slotIndex].StoredItem.ColSize - 1u; // skip the occupied slots with the items we have already returned.
                    }
                }
            }
        }
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}