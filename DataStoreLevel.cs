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
    void GenerateLevel(int boxCount, int itemType)
    {
        level = new StoreLevel(boxCount, itemType);
    }
    [Button()]
    void SolveLevel()
    {
        Queue<List<LevelAction>> flowQueue = new Queue<List<LevelAction>>();
        flowQueue.Enqueue(new List<LevelAction>());

        HashSet<int> checkedLevelHashes = new HashSet<int>();
        checkedLevelHashes.Add(level.GetHashCode());

        int loopCount = 0;
        int maxQueueCount = 0;
        while (flowQueue.Count > 0 && flowQueue.Count < 100000)
        {
            loopCount++;
            List<LevelAction> levelFlow = flowQueue.Dequeue();

            StoreLevel _level = new StoreLevel(level, levelFlow);
            foreach (var item in _level.dicBox)
            {
                StoreBox box = item.Value;
                if (box.IsEmptyBox())
                    continue;

                foreach (var item2 in _level.dicBox)
                {
                    if (item.Key == item2.Key)
                        continue;
                    StoreBox box2 = item2.Value;
                    if (box2.IsFullBox())
                        continue;

                    int lastItem = box.GetLastItem();
                    if (box2.ValidSlot(lastItem))
                    {
                        LevelAction newAction = new LevelAction(item.Key, item2.Key);
                        int nextHashCode = _level.GetNextHashCode(newAction, lastItem);
                        if (!checkedLevelHashes.Contains(nextHashCode))
                        {
                            checkedLevelHashes.Add(nextHashCode);

                            StoreLevel newLevel = new StoreLevel(_level);
                            newLevel.dicBox[item.Key].TakeItem();
                            newLevel.dicBox[item2.Key].AddItem(lastItem);
                            if (newLevel.dicBox[item2.Key].IsComplete())
                            {
                                newLevel.dicBox[item2.Key].ClearBox();
                            }

                            List<LevelAction> newFlow = new List<LevelAction>(levelFlow);
                            newFlow.Add(newAction);
                            flowQueue.Enqueue(newFlow);

                            if (newLevel.CompleteLevel())
                            {
                                Debug.Log("Level Solved!");
                                Debug.Log("Loop Count: " + loopCount);
                                Debug.Log("Max Queue: " + maxQueueCount);
                                DebugCustom.LogColorJson("Solved Level:", newFlow);
                                return;
                            }
                        }
                    }
                }
            }
            if (flowQueue.Count > maxQueueCount)
                maxQueueCount = flowQueue.Count;
            //Debug.Log($"Loop {loopCount}. Queue Size: {flowQueue.Count}");
        }

        Debug.Log("Level Unsolved. Loop Count: " + loopCount + ". Queue Count: " + flowQueue.Count);
    }
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
        level.dicBox = new Dictionary<int, StoreBox>();
        for (int i = 0; i < totalBox; i++)
        {
            StoreBox box = new StoreBox();
            level.dicBox.Add(i, box);
            lstEmptyBoxId.Add(i);
            lstValidIdCount.Add(i);
        }
        int count = 1000;
        visitedHashes.Add(level.GetHashCode());
        while (stepCount > 0 && count > 0)
        {
            count--;
            int boxFromId = lstValidIdCount.GetRandom();
            if (lstFullBoxId.Count > 0)
                boxFromId = lstFullBoxId.GetRandom();
            if (lstEmptyBoxId.Count > 0 && dicItemCount.Count > 0)
                boxFromId = lstEmptyBoxId.GetRandom();
            StoreBox boxFrom = level.dicBox[boxFromId];
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
                    if (item.Key != boxFromId && item.Value.ValidSlotRevese(lastItem) && !visitedHashes.Contains(level.GetNextHashCode(new LevelAction(boxFromId, item.Key), lastItem)))
                        lstValidBoxToId.Add(item.Key);
                }
                if (lstValidBoxToId.Count == 0)
                {
                    Debug.LogError("No valid box to move item to!");
                    return;
                }
                int boxToId = lstValidBoxToId.GetRandom();
                StoreBox boxTo = level.dicBox[boxToId];

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
}
public class StoreLevel
{
    public Dictionary<int, StoreBox> dicBox;
    public StoreLevel()
    {

    }
    public StoreLevel(StoreLevel level)
    {
        dicBox = new Dictionary<int, StoreBox>();
        foreach (var item in level.dicBox)
        {
            StoreBox box = new StoreBox();
            box.lstItems = new List<int>(item.Value.lstItems);
            dicBox.Add(item.Key, box);
        }
    }
    public StoreLevel(int boxCount, int itemType)
    {
        if (itemType <= 0 || boxCount <= itemType)
        {
            Debug.LogError("itemType must > 0 and boxCount must >= itemType");
            return;
        }
        dicBox = new Dictionary<int, StoreBox>();
        List<StoreBox> lstValidBox = new List<StoreBox>();
        List<StoreBox> lstEmptyBox = new List<StoreBox>();

        int totalItemCount = boxCount * 3;
        boxCount += 2;
        for (int i = 0; i < boxCount; i++)
        {
            StoreBox box = new StoreBox();
            dicBox.Add(i, box);
            lstValidBox.Add(box);
            lstEmptyBox.Add(box);
        }
        lstValidBox.Shuffle();
        Dictionary<int, int> dicItemCount = new Dictionary<int, int>();
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
        DebugCustom.LogColorJson("DicItem:", dicItemCount);
        List<int> lstItems = new List<int>();
        foreach (var item in dicItemCount)
        {
            for (int i = 0; i < item.Value; i++)
            {
                lstItems.Add(item.Key);
            }
        }
        lstItems.Shuffle();
        Debug.Log(lstItems.Count);
        while (lstEmptyBox.Count > 0)
        {
            StoreBox box = lstEmptyBox.GetRandom();
            lstEmptyBox.Remove(box);
            int item = lstItems.GetRandom();
            lstItems.Remove(item);
            box.AddItem(item);
        }
        while (lstItems.Count > 0 && lstValidBox.Count > 0)
        {
            int item = lstItems.GetRandom();
            lstItems.Remove(item);
            StoreBox box = lstValidBox.GetRandom();
            box.AddItem(item);
            if (box.IsComplete())
            {
                box.TakeItem();
            }
            else
            {
                if (box.IsFullBox())
                    lstValidBox.Remove(box);
            }
        }
        if (lstItems.Count > 0)
            Debug.LogError("There are still items left after filling boxes! Items left: " + lstItems.Count);
    }
    public StoreLevel(StoreLevel levelBase, List<LevelAction> lstAction)
    {
        dicBox = new Dictionary<int, StoreBox>();
        foreach (var item in levelBase.dicBox)
        {
            StoreBox box = new StoreBox(item.Value);
            dicBox.Add(item.Key, box);
        }
        foreach (var action in lstAction)
        {
            if (dicBox.ContainsKey(action.boxIdFrom) && dicBox.ContainsKey(action.boxIdTo))
            {
                StoreBox boxFrom = dicBox[action.boxIdFrom];
                StoreBox boxTo = dicBox[action.boxIdTo];
                if (!boxFrom.IsEmptyBox() && !boxTo.IsFullBox())
                {
                    int item = boxFrom.TakeItem();
                    boxTo.AddItem(item);
                    if (boxTo.IsComplete())
                    {
                        boxTo.ClearBox();
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
                StoreBox box = item.Value;
                StoreBox ortherBox = otherLevel.dicBox[item.Key];
                for (int i = 0; i < box.lstItems.Count; i++)
                {
                    if (box.lstItems[i] != ortherBox.lstItems[i])
                        return false;
                }
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
    public int GetNextHashCode(LevelAction action, int _item)
    {
        unchecked
        {
            int hash = 17;
            List<int> lstHash = new List<int>();
            foreach (var item in dicBox)
            {
                if (item.Key == action.boxIdFrom)
                {
                    StoreBox box = new StoreBox(item.Value);
                    box.TakeItem();
                    lstHash.Add(box.GetHashCode());
                }
                else if (item.Key == action.boxIdTo)
                {
                    StoreBox box = new StoreBox(item.Value);
                    box.AddItem(_item);
                    lstHash.Add(box.GetHashCode());
                }
                else
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
public class StoreBox
{
    public List<int> lstItems;
    public StoreBox()
    {
        lstItems = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            lstItems.Add(0);
        }
    }
    public StoreBox(StoreBox storeBox)
    {
        lstItems = new List<int>(storeBox.lstItems);
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
    public bool ValidSlotRevese(int item)
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
    public bool IsFullBox()
    {
        return lstItems[2] != 0;
    }
    public void ClearBox()
    {
        lstItems.Clear();
        for (int i = 0; i < 3; i++)
        {
            lstItems.Add(0);
        }
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