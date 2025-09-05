using System;
using System.Diagnostics;

public class CharacterModel
{
    // 数据属性
    public int Level { get; private set; } = 1;
    public int CurrentExp { get; private set; } = 0;
    public int ExpToNextLevel { get; private set; } = 100;
    public int Attack { get; private set; } = 10;
    public int Defense { get; private set; } = 5;
    public string Name { get; private set; } = "勇者";

    // 当数据变化时，需要通知外界
    public event Action OnDataChanged;

    // 业务逻辑方法
    public bool TryLevelUp()
    {
        if (CurrentExp >= ExpToNextLevel)
        {
            Level++;
            CurrentExp = 0;
            Attack += 2;
            Defense += 2;
            ExpToNextLevel += 50;
            OnDataChanged?.Invoke();
            return true;
        }
        return false;
    }

    public void AddExperience(int amount)
    {
        CurrentExp += amount;
        if (CurrentExp > ExpToNextLevel)
        {
            CurrentExp = ExpToNextLevel;
        }
        UnityEngine.Debug.Log(CurrentExp);
        OnDataChanged?.Invoke();
    }
}