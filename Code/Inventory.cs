using System.Collections.Generic;

namespace Heph.Unity.Inventory
{
    /// <summary>
    /// A simple one-dimensional inventory.
    /// </summary>
    public class Inventory
    {
        /// <summary>
        /// Capacity of the inventory.
        /// </summary>
        public virtual int SlotCount { get; protected set; }
        /// <summary>
        /// Do not modify (add or remove items) the list directly, it might cause some items to share the same slots. Use the "Add", "AddTo", "Remove", and "Move" methods instead.<br/>
        /// Do not modify the items' "SlotIndex" and "OccupiedSlotCount" properties after they are added to the list.
        /// </summary>
        public virtual List<IInventoryItem> Items { get; }
        public Inventory(int slotCount)
        {
            SlotCount = slotCount;
            Items = new List<IInventoryItem>();
        }
        /// <summary>
        /// Gets the item by its <paramref name="slot"/> from the inventory.
        /// </summary>
        /// <returns>If found, the item that occupies the <paramref name="slot"/>; otherwise, null.</returns>
        public virtual IInventoryItem this[int slot]
        {
            get
            {
                if (slot < SlotCount)
                {
                    for (int i = 0; i < Items.Count; i++)
                    {
                        IInventoryItem inventoryItem = Items[i];
                        if (inventoryItem.SlotIndex <= slot && inventoryItem.SlotIndex + inventoryItem.OccupiedSlotCount > slot) // the slot is occupied by an item
                        {
                            return inventoryItem;
                        }
                    }
                }
                return null;
            }
        }
        /// <summary>
        /// <inheritdoc cref="this[int]"/>
        /// </summary>
        /// <returns><inheritdoc cref="this[int]"/></returns>
        public virtual IInventoryItem At(int slot) => this[slot];
        /// <summary>
        /// Sets the slot count (capacity) of the inventory to the <paramref name="newSlotCount"/>
        /// </summary>
        /// <returns>The removed inventory items. Items are removed due to the one or more of the occupied slots by that item no longer exists.</returns>
        public virtual List<IInventoryItem> SetSlotCount(int newSlotCount)
        {
            List<IInventoryItem> removedItems = new List<IInventoryItem>();
            if (newSlotCount >= 0)
            {
                if (newSlotCount < SlotCount)
                {
                    for (int i = 0; i < Items.Count; i++)
                    {
                        IInventoryItem inventoryItem = Items[i];
                        if (inventoryItem.SlotIndex + inventoryItem.OccupiedSlotCount > newSlotCount) // one or more of the occupied slots by the item no longer exists, thus remove the item.
                        {
                            Remove(inventoryItem);
                            removedItems.Add(inventoryItem);
                            i--; // Since the item is removed from the list item count of the list and the indices of the items are decreased.
                        }
                    }
                }
                SlotCount = newSlotCount;
            }
            return removedItems;
        }
        /// <summary>
        /// Checkes if the <paramref name="item"/> is already stored in the inventory.
        /// </summary>
        /// <returns>true if the <paramref name="item"/> is already stored in the inventory; othrewise, false.</returns>
        public virtual bool Exists(IInventoryItem item) => item != null && item == this[item.SlotIndex];
        /// <summary>
        /// Adds the item to the first available slot.
        /// </summary>
        /// <returns>True if added successfully, false if the function has failed due to insufficient capacity.</returns>
        public virtual bool Add(IInventoryItem item)
        {
            if (!Exists(item)) // if the item is already stored in the inventory, fail the function.
            {
                int i = 0;
                while (i + item.OccupiedSlotCount <= SlotCount)
                {
                    for (int j = 0; j < item.OccupiedSlotCount; j++)
                    {
                        if (this[i + j] is IInventoryItem inventoryItem) // slot is occupied by an item
                        {
                            i = inventoryItem.SlotIndex + inventoryItem.OccupiedSlotCount; // get to the next slot that is not occupied by this item
                            break;
                        }
                        if (j + 1 == item.OccupiedSlotCount) // All of the necessary slots are empty, we can add the item.
                        {
                            item.SlotIndex = i;
                            Items.Add(item);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Adds the item to the specified slot. Slot is specified by the <paramref name="item"/>.SlotIndex property.
        /// </summary>
        /// <returns>
        /// true if the <paramref name="item"/> is added successfully; otherwise false. 
        /// This method also returns false if the item is already stored in the inventory, to safely change an items slot index call the 'Move' method.
        /// </returns>
        public virtual bool AddTo(IInventoryItem item)
        {
            if (Exists(item) || item.SlotIndex + item.OccupiedSlotCount > SlotCount)
            {
                return false;
            }
            for (int i = item.SlotIndex; i < item.SlotIndex + item.OccupiedSlotCount; i++)
            {
                if (this[i] != null)
                {
                    return false;
                }
            }
            Items.Add(item);
            return true;
        }
        /// <summary>
        /// Removes the <paramref name="item"/> from the inventory.
        /// </summary>
        /// <returns>true if the item is successfully found and removed; otherwise, false.</returns>
        public virtual bool Remove(IInventoryItem item) => Items.Remove(item);
        /// <summary>
        /// Removes the item that occupies the <paramref name="slot"/> from the inventory.
        /// </summary>
        /// <returns><inheritdoc cref="Remove(IInventoryItem)"/></returns>
        public virtual bool Remove(int slot)
        {
            if (this[slot] is IInventoryItem item)
            {
                return Remove(item);
            }
            return false;
        }
        /// <summary>
        /// Moves the <paramref name="item"/> to the <paramref name="targetSlot"/>.
        /// If the <paramref name="targetSlot"/> is occupied, switches the slots of the items. If the item switch is not possible the function fails.
        /// </summary>
        /// <returns>true if found and moved the item successfully; otherwise, false.</returns>
        public virtual bool Move(IInventoryItem item, int targetSlot)
        {
            if (Exists(item) && targetSlot + item.OccupiedSlotCount <= SlotCount)
            {
                // Check if an item occupies the targeted slots, if there is more than one item fail the function.
                IInventoryItem targetItem = this[targetSlot];
                for (int i = 1; i < item.OccupiedSlotCount; i++)
                {
                    IInventoryItem tempItem = this[targetSlot + i];
                    if (tempItem != null && tempItem != targetItem)
                    {
                        if (targetItem != null)
                        {
                            return false; // there are more than one item that occupies the necessary slots.
                        }
                        else
                        {
                            targetItem = tempItem;
                        }
                    }
                }
                Remove(item);
                if (targetItem == null) // if the targeted slotes are empty, simply add the item.
                {
                    item.SlotIndex = targetSlot;
                    Items.Add(item);
                    return true;
                }
                // if there is an item occupying one or more of the targeted slots move it to the items (the parameters) slot.
                Remove(targetItem);
                int oldTargetItemSlotIndex = targetItem.SlotIndex;
                targetItem.SlotIndex = item.SlotIndex;
                if (!AddTo(targetItem)) // if cannot move the target item to the items (the parameters) slot, revert changes and return false. 
                {
                    targetItem.SlotIndex = oldTargetItemSlotIndex;
                    Items.Add(targetItem);
                    Items.Add(item);
                    return false;
                }
                item.SlotIndex = targetSlot; // if target item is successfully moved, finish the the process.
                Items.Add(item);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Finds the item by its <paramref name="currentSlot"/> and moves it to the <paramref name="targetSlot"/>.
        /// If the <paramref name="targetSlot"/> is occupied, switches the slots of the items. If the item switch is not possible the function fails.
        /// </summary>
        /// <returns>true if found and moved the item successfully; otherwise, false.</returns>
        public virtual bool Move(int currentSlot, int targetSlot) => Move(this[currentSlot], targetSlot);
        /// <summary>
        /// <inheritdoc cref="MoveToInventory(Inventory, int, int, bool)"/>
        /// </summary>
        /// <returns><inheritdoc cref="MoveToInventory(Inventory, int, int, bool)"/></returns>
        public virtual bool MoveToInventory(Inventory targetInventory, int currentSlot, int targetSlot) => MoveToInventory(targetInventory, this[currentSlot], targetSlot, true);
        /// <summary>
        /// Moves the item at the <paramref name="currentSlot"/> of this inventory to the <paramref name="targetSlot"/> of the <paramref name="targetInventory"/>.
        /// </summary>
        /// <returns>true if the item is successfully moved. Otherwise, false.</returns>
        public virtual bool MoveToInventory(Inventory targetInventory, int currentSlot, int targetSlot, bool switchItems) => MoveToInventory(targetInventory, this[currentSlot], targetSlot, switchItems);
        /// <summary>
        /// <inheritdoc cref="MoveToInventory(Inventory, IInventoryItem, int, bool)"/>
        /// </summary>
        /// <returns><inheritdoc cref="MoveToInventory(Inventory, IInventoryItem, int, bool)"/></returns>
        public virtual bool MoveToInventory(Inventory targetInventory, IInventoryItem item, int targetSlot) => MoveToInventory(targetInventory, item, targetSlot, true);
        /// <summary>
        /// Moves the <paramref name="item"/> from this inventory to the <paramref name="targetSlot"/> of the <paramref name="targetInventory"/>.
        /// </summary>
        /// <returns><inheritdoc cref="MoveToInventory(Inventory, int, int, bool)"/></returns>
        public virtual bool MoveToInventory(Inventory targetInventory, IInventoryItem item, int targetSlot, bool switchItems)
        {
            if (Exists(item))
            {
                if (targetInventory == this)
                {
                    return Move(item, targetSlot);
                }
                else
                {
                    int currentSlot = item.SlotIndex;
                    IInventoryItem targetInventoryItem = targetInventory[targetSlot];
                    if (targetInventoryItem == null)
                    {
                        Remove(item);
                        item.SlotIndex = targetSlot;
                        if (!targetInventory.AddTo(item)) // Add to the target slot.
                        {
                            item.SlotIndex = currentSlot; // Couldn't add the item to the target inventory, add it back to its original inventory and slot.
                            AddTo(item);
                            return false;
                        }
                        return true;
                    }
                    else if (switchItems)
                    {
                        Remove(item);
                        item.SlotIndex = targetSlot;
                        targetInventory.Remove(targetInventoryItem);
                        targetInventoryItem.SlotIndex = currentSlot;
                        if (!targetInventory.AddTo(item) || !AddTo(targetInventoryItem)) // switch failed, Add the items back to their original inventory and slot.
                        {
                            item.SlotIndex = currentSlot;
                            targetInventoryItem.SlotIndex = targetSlot;
                            AddTo(item);
                            targetInventory.AddTo(targetInventoryItem);
                            return false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }
}