using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
[CreateAssetMenu(fileName = "Store Level", menuName = "Data/Test/Store Level")]
public class DataStoreLevel : SerializedScriptableObject
{
    public StoreLevel level;
    public LevelGenerateConfig levelConfig;
    [Button()]
    void GenerateLevel()
    {
        CommonBoxConfig commonBoxConfig = levelConfig.commonBoxConfig;
        LockedBoxConfig lockedBoxConfig = levelConfig.lockedBoxConfig;
        PendingBoxConfig pendingBoxConfig = levelConfig.pendingBoxConfig;
        SinglestackBoxConfig singlestackBoxConfig = levelConfig.singlestackBoxConfig;
        if (commonBoxConfig.boxCount <= 3)
        {
            Debug.LogError("Box count must be greater than 3 to generate a valid level.");
            return;
        }
        if (levelConfig.itemTypeCount < 0 || levelConfig.itemTypeCount > commonBoxConfig.boxCount)
        {
            Debug.LogError("Item type count must be between 0 and box count.");
            return;
        }
        level = new StoreLevel();
        List<int> lstItemType = new List<int>();
        for (int i = 1; i <= levelConfig.itemTypeCount; i++)
        {
            lstItemType.Add(i);
        }
        for (int i = 0; i < commonBoxConfig.boxCount; i++)
        {
            CommonBoxStack boxStack = new CommonBoxStack(commonBoxConfig.boxDepth, 0);
            level.dicBox.Add(i, boxStack);
        }
        for (int i = 0; i < singlestackBoxConfig.boxCount; i++)
        {
            SingleStackBox singleStack = new SingleStackBox(singlestackBoxConfig.boxDepth, singlestackBoxConfig.locked);
            level.dicBox.Add(commonBoxConfig.boxCount + i, singleStack);
        }
        HashSet<int> visitedHashes = new HashSet<int>();
        visitedHashes.Add(level.GetHashCode());
        List<LevelAction> lstLevelActions = new List<LevelAction>();
        int stepCount = 0;
        while (stepCount < levelConfig.stepCount)
        {
            List<int> lstValidBoxFromId = new List<int>();
            foreach (var iBox in level.dicBox)
            {
                if (iBox.Value.CanTakeItemReverse())
                {
                    lstValidBoxFromId.Add(iBox.Key);
                }
            }
            if (lstValidBoxFromId.Count == 0)
            {
                Debug.LogError("No valid box to take item from!");
                break;
            }
            int boxFromId = lstValidBoxFromId.GetRandom();
            IStoreBox boxFrom = level.dicBox[boxFromId];
            if (boxFrom.IsEmptyBox())
            {
                int newItem = lstItemType.GetRandom();
                lstItemType.Remove(newItem);
                if (lstItemType.Count == 0)
                {
                    for (int i = 1; i <= levelConfig.itemTypeCount; i++)
                    {
                        lstItemType.Add(i);
                    }
                }
                for (int i = 0; i < 3; i++)
                {
                    boxFrom.AddItem(newItem);
                }
            }
            if (boxFrom is CommonBoxStack)
            {
                CommonBoxStack boxStack = boxFrom as CommonBoxStack;
                if (boxStack.IsMixedItem())
                {
                    if (boxStack.lstBoxes.Count < commonBoxConfig.boxDepth)
                    {
                        boxStack.AddBox(new CommonBox());
                        continue;
                    }
                    else if (level.pendingBoxs.Count < pendingBoxConfig.boxCount)
                    {
                        level.pendingBoxs.Enqueue(new PendingBox(boxStack.GetLastBox().lstItems));
                        boxStack.GetLastBox().ClearBox();
                        continue;
                    }
                }
            }
            int item = boxFrom.GetLastItem();

            List<int> lstValidBoxToId = new List<int>();
            foreach (var iBox in level.dicBox)
            {
                if (iBox.Key != boxFromId && iBox.Value.CanAddItemReverse(boxFrom))
                {
                    lstValidBoxToId.Add(iBox.Key);
                }
            }
            if (lstValidBoxToId.Count == 0)
            {
                Debug.LogError("No valid box to add item to!");
                break;
            }

            int boxToId = lstValidBoxToId.GetRandom();
            List<int> lstValidEmptyBoxToId = lstValidBoxFromId.Where(id => level.dicBox[id].IsEmptyBox()).ToList();
            if (lstValidEmptyBoxToId.Count > 0)
                boxToId = lstValidEmptyBoxToId.GetRandom();
            IStoreBox boxTo = level.dicBox[boxToId];

            boxFrom.TakeItem();
            if (boxTo.IsFullBox() && boxTo is CommonBoxStack)
            {
                ((CommonBoxStack)boxTo).AddBox(new CommonBox());
            }
            boxTo.AddItem(item);
            int hash = level.GetHashCode();
            if (visitedHashes.Contains(hash))
                Debug.LogWarning($"Duplicate level state detected, retrying step generation. Step: {stepCount}");
            else{
                visitedHashes.Add(level.GetHashCode());
                LevelAction action = new LevelAction(boxFromId, boxToId);
                lstLevelActions.Add(action);
            }
            stepCount++;
        }
        int reversedId = 0;
        List<int> lstValidId = new List<int>();
        foreach (var iBox in level.dicBox)
        {
            if (iBox.Value is CommonBoxStack)
            {
                lstValidId.Add(iBox.Key);
            }
        }
        int lockBoxCount = 0;
        for (int i = 0; i < lockedBoxConfig.boxCount; i++)
        {
            for (int j = 0; j < lockedBoxConfig.lockCount; j++)
            {
                reversedId++;
                LevelAction action = lstLevelActions[lstLevelActions.Count - reversedId];
                if (lstValidId.Contains(action.boxIdFrom))
                    lstValidId.Remove(action.boxIdFrom);
                if (lstValidId.Contains(action.boxIdTo))
                    lstValidId.Remove(action.boxIdTo);
            }
            if (lstValidId.Count > 0)
            {
                int lockId = lstValidId.GetRandom();
                ((CommonBoxStack)level.dicBox[lockId]).locked = lockedBoxConfig.lockCount;
                lstValidId.Remove(lockId);
                lockBoxCount++;
            }
        }
        Debug.Log($"Level generated with {level.dicBox.Count} boxes, {lockBoxCount} Locked Box and {level.pendingBoxs.Count} pending boxes.");
        Debug.Log($"Valid step: {visitedHashes.Count}, Total step: {stepCount}");
    }
}
public class LevelGenerateConfig
{
    public CommonBoxConfig commonBoxConfig;
    public LockedBoxConfig lockedBoxConfig;
    public PendingBoxConfig pendingBoxConfig;
    public SinglestackBoxConfig singlestackBoxConfig;
    public int itemTypeCount;
    public int stepCount;
}
public class CommonBoxConfig
{
    public int boxCount;
    public int boxDepth;
}
public class LockedBoxConfig
{
    public int boxCount;
    public int lockCount;
}
public class PendingBoxConfig
{
    public int boxCount;
}
public class SinglestackBoxConfig
{
    public int boxCount;
    public int boxDepth;
    public bool locked = false;
}
public class StoreLevel
{
    public Dictionary<int, IStoreBox> dicBox;
    public Queue<PendingBox> pendingBoxs;
    public StoreLevel()
    {
        dicBox = new Dictionary<int, IStoreBox>();
        pendingBoxs = new Queue<PendingBox>();
    }
    public StoreLevel(StoreLevel level)
    {
        dicBox = new Dictionary<int, IStoreBox>();
        foreach (var item in level.dicBox)
        {
            dicBox.Add(item.Key, item.Value.GetClone());
        }
        pendingBoxs = new Queue<PendingBox>(level.pendingBoxs.Select(box => new PendingBox(box)));
    }
    public StoreLevel(StoreLevel levelBase, List<LevelAction> lstAction)
    {
        dicBox = new Dictionary<int, IStoreBox>();
        foreach (var item in levelBase.dicBox)
        {
            dicBox.Add(item.Key, item.Value.GetClone());
        }
        foreach (var action in lstAction)
        {
            if (dicBox.ContainsKey(action.boxIdFrom) && dicBox.ContainsKey(action.boxIdTo))
            {
                bool completeBox = dicBox[action.boxIdTo].ApplyAction(dicBox[action.boxIdFrom]);
                if (completeBox && pendingBoxs.Count > 0)
                {
                    PendingBox box = pendingBoxs.Dequeue();
                    foreach (var item in box.lstItems)
                    {
                        if (item != 0)
                            dicBox[action.boxIdTo].AddItem(item);
                    }
                }
            }
        }
    }
    public bool CompleteLevel()
    {
        foreach (var item in dicBox)
        {
            if (!item.Value.IsComplete() && !item.Value.IsEmptyBox())
                return false;
        }
        return true;
    }
    public override bool Equals(object obj)
    {
        if (obj is StoreLevel otherLevel)
        {
            if (dicBox.Count != otherLevel.dicBox.Count)
                return false;
            foreach (var item in dicBox)
            {
                if (!otherLevel.dicBox.ContainsKey(item.Key))
                    return false;
                IStoreBox box = item.Value;
                IStoreBox ortherBox = otherLevel.dicBox[item.Key];
                if (!box.Equals(ortherBox))
                    return false;
            }
            return true;
        }
        return false;
    }
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            List<int> lstHash = new List<int>();
            foreach (var item in dicBox)
            {
                lstHash.Add(item.Value.GetHashCode());
            }
            lstHash.Sort();
            foreach (var item in lstHash)
            {
                hash = hash * 31 + item;
            }
            return hash;
        }
    }
}
public interface IStoreBox
{
    public IStoreBox GetClone();
    public bool Equals(object obj);
    public int GetHashCode();
    public bool IsEmptyBox();
    public bool IsFullBox();
    public int GetItemCount();
    public bool ValidSlot(int item);
    public bool ValidSlotReverse(int item);
    public int GetLastItem();
    public bool IsMixedItem();
    public int TakeItem();
    public void AddItem(int item);
    public bool CanTakeItem();
    public bool CanTakeItemReverse();
    public bool CanAddItem(IStoreBox boxFrom);
    public bool CanAddItemReverse(IStoreBox boxFrom);
    public bool ApplyAction(IStoreBox boxFrom);
    public bool IsComplete();
    public void ClearBox();
}
public class CommonBox : IStoreBox
{
    public List<int> lstItems;
    public CommonBox()
    {
        lstItems = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            lstItems.Add(0);
        }
    }
    public CommonBox(CommonBox storeBox)
    {
        lstItems = new List<int>(storeBox.lstItems);
    }
    public IStoreBox GetClone()
    {
        return new CommonBox(this);
    }
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 1;
            for (int i = 0; i < lstItems.Count; i++)
            {
                hash = hash * 30 + lstItems[i];
            }
            return hash;
        }
    }
    public bool IsEmptyBox()
    {
        return lstItems[0] == 0;
    }
    public bool EmptySlot()
    {
        return lstItems[2] == 0;
    }
    public bool ValidSlot(int item)
    {
        return IsEmptyBox() || GetLastItem() == item;
    }
    public bool IsFullBox()
    {
        return lstItems[2] != 0;
    }
    public int GetItemCount()
    {
        for (int i = 0; i < lstItems.Count; i++)
        {
            if (i == 0)
                return i;
        }
        return lstItems.Count;
    }
    public bool ValidSlotReverse(int item)
    {
        bool dup = false;
        if (lstItems[0] == item && lstItems[1] == item)
            dup = true;
        return IsEmptyBox() || (!IsFullBox() && !dup);
    }
    public int GetLastItem()
    {
        for (int i = 2; i >= 0; i--)
        {
            if (lstItems[i] != 0)
                return lstItems[i];
        }
        return 0;
    }
    public bool IsMixedItem()
    {
        return lstItems[1] != 0 && lstItems[0] != lstItems[1];
    }
    public void AddItem(int item)
    {
        for (int i = 0; i < 3; i++)
        {
            if (lstItems[i] == 0)
            {
                lstItems[i] = item;
                break;
            }
        }
    }
    public int TakeItem()
    {
        for (int i = 2; i >= 0; i--)
        {
            if (lstItems[i] != 0)
            {
                int item = lstItems[i];
                lstItems[i] = 0;
                return item;
            }
        }
        return -1;
    }
    public bool IsComplete()
    {
        return lstItems[0] != 0 && lstItems[0] == lstItems[1] && lstItems[0] == lstItems[2];
    }
    public void ClearBox()
    {
        lstItems.Clear();
        for (int i = 0; i < 3; i++)
        {
            lstItems.Add(0);
        }
    }
    public bool CanTakeItem()
    {
        return !IsEmptyBox();
    }
    public bool CanTakeItemReverse()
    {
        return !IsEmptyBox();
    }
    public bool CanAddItem(IStoreBox boxFrom)
    {
        if (IsFullBox())
            return false;
        if (boxFrom.IsEmptyBox())
            return false;
        return GetLastItem() == boxFrom.GetLastItem();
    }
    public bool CanAddItemReverse(IStoreBox boxFrom)
    {
        if (IsFullBox())
            return false;
        if (boxFrom.IsEmptyBox())
            return false;
        int lastItem = boxFrom.GetLastItem();
        if (lstItems[0] == lastItem && lstItems[1] == lastItem)
            return false;
        return true;
    }
    public bool ApplyAction(IStoreBox boxFrom)
    {
        int item = boxFrom.TakeItem();
        AddItem(item);
        if (IsComplete())
        {
            ClearBox();
            return true;
        }
        return false;
    }
    public override bool Equals(object obj)
    {
        if (obj is CommonBox)
        {
            CommonBox commonBox = obj as CommonBox;
            for (int i = 0; i < lstItems.Count; i++)
            {
                if (commonBox.lstItems[i] != lstItems[i])
                    return false;
            }
            return true;
        }
        return false;
    }
}
public class CommonBoxStack : IStoreBox
{
    public List<CommonBox> lstBoxes;
    public int boxDepth;
    public int locked = 0;
    public CommonBoxStack(int boxDepth, int locked)
    {
        lstBoxes = new List<CommonBox>();
        this.boxDepth = boxDepth;
        lstBoxes.Add(new CommonBox());
        this.locked = locked;
    }
    public CommonBoxStack()
    {
        lstBoxes = new List<CommonBox>();
        lstBoxes.Add(new CommonBox());
    }
    public IStoreBox GetClone()
    {
        CommonBoxStack clone = new CommonBoxStack();
        clone.lstBoxes = new List<CommonBox>(lstBoxes.Select(box => box.GetClone() as CommonBox));
        clone.boxDepth = boxDepth;
        clone.locked = locked;
        return clone;
    }
    public override bool Equals(object obj)
    {
        if (obj is CommonBoxStack otherStack)
        {
            if (lstBoxes.Count != otherStack.lstBoxes.Count)
                return false;
            for (int i = 0; i < lstBoxes.Count; i++)
            {
                if (!lstBoxes[i].Equals(otherStack.lstBoxes[i]))
                    return false;
            }
            return true;
        }
        return false;
    }
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 2 + locked;
            foreach (var box in lstBoxes)
            {
                hash = hash * 31 + box.GetHashCode();
            }
            return hash;
        }
    }
    public CommonBox GetLastBox()
    {
        return lstBoxes[0];
    }
    public bool IsEmptyBox()
    {
        return GetLastBox().IsEmptyBox();
    }
    public bool IsFullBox()
    {
        return GetLastBox().IsFullBox();
    }
    public bool ValidSlot(int item)
    {
        return GetLastBox().ValidSlot(item);
    }
    public bool ValidSlotReverse(int item)
    {
        return GetLastBox().ValidSlotReverse(item);
    }
    public int GetLastItem()
    {
        return GetLastBox().GetLastItem();
    }
    public int GetItemCount()
    {
        return GetLastBox().GetItemCount();
    }
    public bool IsMixedItem()
    {
        return GetLastBox().IsMixedItem();
    }
    public int TakeItem()
    {
        return GetLastBox().TakeItem();
    }
    public void AddItem(int item)
    {
        GetLastBox().AddItem(item);
    }
    public bool CanTakeItem()
    {
        return GetLastBox().CanTakeItem();
    }
    public bool CanTakeItemReverse()
    {
        return true;
    }
    public bool CanAddItem(IStoreBox boxFrom)
    {
        return GetLastBox().CanAddItem(boxFrom);
    }
    public bool CanAddItemReverse(IStoreBox boxFrom)
    {
        return lstBoxes.Count < boxDepth || GetLastBox().CanAddItemReverse(boxFrom);
    }
    public bool ApplyAction(IStoreBox boxFrom)
    {
        GetLastBox().ApplyAction(boxFrom);
        if (GetLastBox().IsComplete() && lstBoxes.Count > 1)
        {
            lstBoxes.RemoveAt(0);
        }
        if (lstBoxes.Count == 0 && GetLastBox().IsEmptyBox())
        {
            return true;
        }
        return false;
    }
    public bool IsComplete()
    {
        return lstBoxes.Count == 0 && GetLastBox().IsComplete();
    }
    public void ClearBox()
    {
        lstBoxes.Clear();
        lstBoxes.Add(new CommonBox());
    }
    public void AddBox(CommonBox box)
    {
        lstBoxes.Insert(0, box);
    }
}
public class PendingBox
{
    public List<int> lstItems;
    public PendingBox()
    {
        lstItems = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            lstItems.Add(0);
        }
    }
    public PendingBox(List<int> items)
    {
        lstItems = new List<int>(items);
    }
    public PendingBox(PendingBox storeBox)
    {
        lstItems = new List<int>(storeBox.lstItems);
    }
    public void AddItem(int item)
    {
        for (int i = 0; i < 3; i++)
        {
            if (lstItems[i] == 0)
            {
                lstItems[i] = item;
                break;
            }
        }
    }
    public override bool Equals(object obj)
    {
        if (obj is PendingBox otherBox)
        {
            if (lstItems.Count != otherBox.lstItems.Count)
                return false;
            for (int i = 0; i < lstItems.Count; i++)
            {
                if (lstItems[i] != otherBox.lstItems[i])
                    return false;
            }
            return true;
        }
        return false;
    }
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 3;
            for (int i = 0; i < lstItems.Count; i++)
            {
                hash = hash * 32 + lstItems[i];
            }
            return hash;
        }
    }
}
public class SingleStackBox : IStoreBox
{
    public Stack<int> stackItems;
    public int boxDepth = 3;
    public bool locked = false;
    public SingleStackBox()
    {
        stackItems = new Stack<int>();
    }
    public SingleStackBox(int boxDepth, bool locked)
    {
        stackItems = new Stack<int>();
        this.boxDepth = boxDepth;
        this.locked = locked;
    }
    public SingleStackBox(SingleStackBox storeBox)
    {
        stackItems = new Stack<int>(storeBox.stackItems.Reverse()); // Reverse to maintain order
        boxDepth = storeBox.boxDepth;
        locked = storeBox.locked;

    }

    public IStoreBox GetClone()
    {
        return new SingleStackBox(this);
    }

    public override bool Equals(object obj)
    {
        if (obj is SingleStackBox otherBox)
        {
            if (stackItems.Count != otherBox.stackItems.Count)
                return false;
            return stackItems.SequenceEqual(otherBox.stackItems);
        }
        return false;
    }
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = locked ? 4 : 5;
            foreach (var item in stackItems)
            {
                hash = hash * (locked ? 33 : 34) + item.GetHashCode();
            }
            return hash;
        }
    }
    public void AddItem(int item)
    {
        stackItems.Push(item);
    }

    public bool ApplyAction(IStoreBox boxFrom)
    {
        AddItem(boxFrom.TakeItem());
        return false;
    }

    public bool CanAddItem(IStoreBox boxFrom)
    {
        return !locked && stackItems.Count == 0;
    }

    public bool CanAddItemReverse(IStoreBox boxFrom)
    {
        return stackItems.Count < boxDepth;
    }

    public bool CanTakeItem()
    {
        return stackItems.Count > 0;
    }

    public bool CanTakeItemReverse()
    {
        return !locked && stackItems.Count == 0;
    }

    public void ClearBox()
    {
        stackItems.Clear();
    }

    public int GetItemCount()
    {
        return stackItems.Count;
    }

    public int GetLastItem()
    {
        return stackItems.Count > 0 ? stackItems.Peek() : 0;
    }

    public bool IsComplete()
    {
        return stackItems.Count == 0;
    }

    public bool IsEmptyBox()
    {
        return stackItems.Count == 0;
    }

    public bool IsFullBox()
    {
        return stackItems.Count >= boxDepth;
    }

    public bool IsMixedItem()
    {
        return stackItems.Count > 1 && stackItems.Distinct().Count() > 1;
    }

    public int TakeItem()
    {
        if (stackItems.Count > 0)
        {
            return stackItems.Pop();
        }
        return -1; // or throw an exception
    }

    public bool ValidSlot(int item)
    {
        return !locked && stackItems.Count == 0;
    }

    public bool ValidSlotReverse(int item)
    {
        return stackItems.Count < boxDepth;
    }
}
public class LevelAction
{
    public int boxIdFrom;
    public int boxIdTo;

    public LevelAction()
    {

    }
    public LevelAction(int from, int to)
    {
        boxIdFrom = from;
        boxIdTo = to;
    }
    public LevelAction(LevelAction action)
    {
        boxIdFrom = action.boxIdFrom;
        boxIdTo = action.boxIdTo;
    }
    public override string ToString()
    {
        return $"Move from Box {boxIdFrom} to Box {boxIdTo}";
    }
}
