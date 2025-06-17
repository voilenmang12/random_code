using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
[CreateAssetMenu(fileName = "Store Level", menuName = "Data/Test/Store Level")]
public class DataStoreLevel : SerializedScriptableObject
{
    public StoreLevel level;
    [Button()]
    public void GenerateLevelReverse(int boxCount, int itemType, int emptyBoxCount, int stepCount)
    {
        int totalBox = boxCount + emptyBoxCount;
        int totalItemCount = boxCount * 3;
        Dictionary<int, int> dicItemCount = new Dictionary<int, int>();
        List<int> lstFullBoxId = new List<int>();
        List<int> lstEmptyBoxId = new List<int>();
        List<int> lstValidIdCount = new List<int>();
        HashSet<int> visitedHashes = new HashSet<int>();
        while (totalItemCount > 0)
        {
            for (int i = 1; i <= itemType; i++)
            {
                if (!dicItemCount.ContainsKey(i))
                    dicItemCount.Add(i, 0);
                dicItemCount[i] += 3;
                totalItemCount -= 3;
                if (totalItemCount == 0)
                    break;
            }
        }
        level = new StoreLevel();
        for (int i = 0; i < totalBox; i++)
        {
            CommonBox box = new CommonBox();
            level.dicBox.Add(i, box);
            lstEmptyBoxId.Add(i);
            lstValidIdCount.Add(i);
        }
        int count = 10000;
        visitedHashes.Add(level.GetHashCode());
        while (stepCount > 0 && count > 0)
        {
            count--;
            int boxFromId = lstValidIdCount.GetRandom();
            if (lstFullBoxId.Count > 0)
                boxFromId = lstFullBoxId.GetRandom();
            if (lstEmptyBoxId.Count > 0 && dicItemCount.Count > 0)
                boxFromId = lstEmptyBoxId.GetRandom();
            CommonBox boxFrom = level.dicBox[boxFromId] as CommonBox;
            if (boxFrom.IsEmptyBox())
            {
                if (dicItemCount.Count > 0)
                {
                    int item = dicItemCount.Keys.ToList().GetRandom();
                    for (int i = 0; i < 3; i++)
                    {
                        boxFrom.lstItems[i] = item;
                    }
                    dicItemCount[item] -= 3;
                    if (dicItemCount[item] <= 0)
                    {
                        dicItemCount.Remove(item);
                    }
                    lstFullBoxId.Add(boxFromId);
                    lstEmptyBoxId.Remove(boxFromId);
                    lstValidIdCount.Remove(boxFromId);
                    visitedHashes.Add(level.GetHashCode());
                }
                else
                    continue;
            }
            else
            {
                int lastItem = boxFrom.GetLastItem();
                List<int> lstValidBoxToId = new List<int>();
                foreach (var item in level.dicBox)
                {
                    if (item.Key != boxFromId && item.Value.ValidSlotReverse(lastItem))
                        lstValidBoxToId.Add(item.Key);
                }
                if (lstValidBoxToId.Count == 0)
                {
                    Debug.LogError("No valid box to move item to!");
                    return;
                }
                int boxToId = lstValidBoxToId.GetRandom();
                CommonBox boxTo = level.dicBox[boxToId] as CommonBox;

                boxFrom.TakeItem();
                boxTo.AddItem(lastItem);
                if (lstFullBoxId.Contains(boxFromId))
                    lstFullBoxId.Remove(boxFromId);
                lstValidIdCount.Add(boxFromId);
            }
            stepCount--;
        }
        DebugCustom.LogColorJson(dicItemCount);
        DebugCustom.LogColor(stepCount);
    }
    [Button()]
    void GenerateLevel(LevelGenerateConfig config)
    {
        if (config.boxCount <= 3)
        {
            Debug.LogError("Box count must be greater than 3 to generate a valid level.");
            return;
        }
        if (config.itemTypeCount < 0 || config.itemTypeCount > config.boxCount)
        {
            Debug.LogError("Item type count must be between 0 and box count.");
            return;
        }
        level = new StoreLevel();
        List<int> lstItemType = new List<int>();
        for (int i = 1; i <= config.itemTypeCount; i++)
        {
            lstItemType.Add(i);
        }
        for (int i = 0; i < config.boxCount; i++)
        {
            CommonBoxStack boxStack = new CommonBoxStack(config.boxDepth);
            level.dicBox.Add(i, boxStack);
        }
        HashSet<int> visitedHashes = new HashSet<int>();
        visitedHashes.Add(level.GetHashCode());
        while (config.stepCount > 0)
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
                return;
            }
            int boxFromId = lstValidBoxFromId.GetRandom();
            CommonBoxStack boxFrom = level.dicBox[boxFromId] as CommonBoxStack;
            if (boxFrom.IsEmptyBox())
            {
                int newItem = lstItemType.GetRandom();
                lstItemType.Remove(newItem);
                if (lstItemType.Count == 0)
                {
                    for (int i = 1; i <= config.itemTypeCount; i++)
                    {
                        lstItemType.Add(i);
                    }
                }
                for (int i = 0; i < 3; i++)
                {
                    boxFrom.AddItem(newItem);
                }
            }
            if (boxFrom.IsMixedItem() && boxFrom.lstBoxes.Count < config.boxDepth)
            {
                boxFrom.AddBox(new CommonBox());
                continue;
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
                return;
            }

            int boxToId = lstValidBoxToId.GetRandom();
            List<int> lstValidEmptyBoxToId = lstValidBoxFromId.Where(id => level.dicBox[id].IsEmptyBox()).ToList();
            if (lstValidEmptyBoxToId.Count > 0)
                boxToId = lstValidEmptyBoxToId.GetRandom();
            CommonBoxStack boxTo = level.dicBox[boxToId] as CommonBoxStack;

            boxFrom.TakeItem();
            if (boxTo.IsFullBox())
            {
                boxTo.AddBox(new CommonBox());
            }
            boxTo.AddItem(item);
            config.stepCount--;
        }
    }
}
public class LevelGenerateConfig
{
    public int boxCount;
    public int boxDepth;
    public int itemTypeCount;

    public int stepCount;
}
public class StoreLevel
{
    public Dictionary<int, IStoreBox> dicBox;
    public StoreLevel()
    {
        dicBox = new Dictionary<int, IStoreBox>();
    }
    public StoreLevel(StoreLevel level)
    {
        dicBox = new Dictionary<int, IStoreBox>();
        foreach (var item in level.dicBox)
        {
            dicBox.Add(item.Key, item.Value.GetClone());
        }
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
                dicBox[action.boxIdTo].ApplyAction(dicBox[action.boxIdFrom]);
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
    public void ApplyAction(IStoreBox boxFrom);
    public bool IsComplete();
    public void ClearBox();
}
public class CommonBox : IStoreBox
{
    public List<int> lstItems;
    public bool locked = false;
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
            int hash = 17;
            for (int i = 0; i < lstItems.Count; i++)
            {
                hash = hash * 31 + lstItems[i];
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
    public void ApplyAction(IStoreBox boxFrom)
    {
        int item = boxFrom.TakeItem();
        AddItem(item);
        if (IsComplete())
            ClearBox();
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
    public CommonBoxStack(int boxDepth)
    {
        lstBoxes = new List<CommonBox>();
        this.boxDepth = boxDepth;
        lstBoxes.Add(new CommonBox());
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
            int hash = 17;
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
    public void ApplyAction(IStoreBox boxFrom)
    {
        GetLastBox().ApplyAction(boxFrom);
        if (GetLastBox().IsComplete() && lstBoxes.Count > 1)
        {
            lstBoxes.RemoveAt(0);
        }
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