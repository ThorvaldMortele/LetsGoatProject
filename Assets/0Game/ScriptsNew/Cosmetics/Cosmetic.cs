using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
[CreateAssetMenu(fileName = "Cosmetic", menuName = "ScriptableObjects/Cosmetic")]
#endif
public class Cosmetic : ScriptableObject
{
    private static string _cosmeticKey = "Cosmetic";
    private static Dictionary<int, int> _cache = new Dictionary<int, int>();

    public enum CosmeticType
    {
        Hat,
        Pattern,
        Tattoo,
        Trail
    }

    public enum HatType
    {
        Head,
        Mouth,
        Back
    }

    public CosmeticType Type;
    [ShowIf("Type", CosmeticType.Hat)]
    public HatType HatPosition;
    public GameObject GameObject;
    public Material Material;
    [Min(1)]
    public int Price;
    public Sprite Sprite;
    public int Id = -1;
    public bool Owned = false;

    private bool _oldOwned = false;

    [Button]
    public void Load()
    {
        if (Id < 0) return;

        int key = Id / 32;
        int data;
        if (_cache.ContainsKey(key))
        {
            data = _cache[key];
        }
        else
        {
            data = PlayerPrefs.GetInt($"{_cosmeticKey}{key}", 0);
            _cache[key] = data;
        }
        
        int mask = 1 << (Id % 32);
        Owned = (data & mask) == mask;
        _oldOwned = Owned;
    }

    public static void Save()
    {
        foreach (KeyValuePair<int, int> pair in _cache)
        {
            PlayerPrefs.SetInt($"{_cosmeticKey}{pair.Key}", pair.Value);
        }
    }

    public void UpdateData()
    {
        if (Owned == _oldOwned || Id < 0) return;

        int key = Id / 32;
        int data = _cache[key];
        int mask = 1 << (Id % 32);
        if (Owned)
        {
            data |= mask;
        }
        else
        {
            data &= ~mask;
        }

        _cache[key] = data;
        _oldOwned = Owned;
    }

    public bool GetCosmetic()
    {
        if (Owned) return true;
  
        GoatbuxManager goatbuxManager = GoatbuxManager.Instance;
        if (goatbuxManager.Goatbux >= Price)
        {
            goatbuxManager.SubtractGoatbux(Price);
            Owned = true;

            //update money visual
        }

        return Owned;
    }
}
