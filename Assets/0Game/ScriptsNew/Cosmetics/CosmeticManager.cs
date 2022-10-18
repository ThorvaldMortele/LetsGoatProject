using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public class CosmeticManager : MonoBehaviour
{
    private static CosmeticManager _instance;
    public static CosmeticManager Instance => _instance;

    private static string _currentCosmeticKey = "CurrentCosmetic";

    [SerializeField]
    [ReadOnly]
    private List<Cosmetic> _cosmetics = new List<Cosmetic>();
    private Dictionary<int, Cosmetic> _cosmeticsDictionary = new Dictionary<int, Cosmetic>();

    public Dictionary<Cosmetic.CosmeticType, Cosmetic> CurrentCosmetics = new Dictionary<Cosmetic.CosmeticType, Cosmetic>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }

        foreach (Cosmetic cosmetic in _cosmetics)
        {
            cosmetic.Load();
            _cosmeticsDictionary[cosmetic.Id] = cosmetic;
        }

        foreach (Cosmetic.CosmeticType type in Enum.GetValues(typeof(Cosmetic.CosmeticType)))
        {
            int id = PlayerPrefs.GetInt($"{_currentCosmeticKey}{type}", -1);
            if (id >= 0)
            {
                CurrentCosmetics[type] = _cosmetics.Find(cosmetic => cosmetic.Id == id);
            }
        }
    }

    public List<Cosmetic> GetCosmetics(Cosmetic.CosmeticType type)
    {
        return _cosmetics.Where(cosmetic => cosmetic.Type == type).ToList();
    }

    public bool SetCurrentCosmetic(Cosmetic cosmetic)
    {
        if (CurrentCosmetics.TryGetValue(cosmetic.Type, out Cosmetic c) && c.Id == cosmetic.Id)
        {
            CurrentCosmetics.Remove(cosmetic.Type);
            return false;
        }
        CurrentCosmetics[cosmetic.Type] = cosmetic;
        return true;
    }

    public void SaveCosmetics()
    {
        foreach (Cosmetic cosmetic in _cosmetics)
        {
            cosmetic.UpdateData();
        }
        Cosmetic.Save();

        foreach (KeyValuePair<Cosmetic.CosmeticType, Cosmetic> pair in CurrentCosmetics)
        {
            PlayerPrefs.SetInt($"{_currentCosmeticKey}{pair.Key}", pair.Value.Id);
        }

        foreach (Cosmetic.CosmeticType type in Enum.GetValues(typeof(Cosmetic.CosmeticType)))
        {
            if (CurrentCosmetics.ContainsKey(type))
            {
                PlayerPrefs.SetInt($"{_currentCosmeticKey}{type}", CurrentCosmetics[type].Id);
            }
            else
            {
                PlayerPrefs.SetInt($"{_currentCosmeticKey}{type}", -1);
            }
        }

        PlayerPrefs.Save();
    }

    public Cosmetic GetCosmetic(int id)
    {
        if (_cosmeticsDictionary.TryGetValue(id, out Cosmetic cosmetic))
        {
            return cosmetic;
        }

        return null;
    }

#if UNITY_EDITOR
    [Button]
    private void UpdateCosmetics()
    {
        _cosmetics.Clear();

        List<Cosmetic> cosmetics = new List<Cosmetic>();
        int lastId = -1;
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(Cosmetic)}");
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (assetPath != "")
            {
                Cosmetic cosmetic = (Cosmetic)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Cosmetic));
                if (cosmetic.Id < 0)
                {
                    cosmetics.Add(cosmetic);
                }
                else if (cosmetic.Id > lastId)
                {
                    lastId = cosmetic.Id;
                }

                _cosmetics.Add(cosmetic);
            }
        }

        foreach (Cosmetic cosmetic in cosmetics)
        {
            ++lastId;
            cosmetic.Id = lastId;
        }

        _cosmetics.Sort((cosmeticL, cosmeticR) => cosmeticL.Id.CompareTo(cosmeticR.Id));
    }
#endif
}
