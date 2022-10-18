using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopPage : MonoBehaviour
{
    [SerializeField]
    private Cosmetic.CosmeticType _type;

    [SerializeField]
    private GameObject _shopItemPrefab;

    [SerializeField]
    private Transform _shopContainer;

    private ShopItem _equipped;

    private ShopUI _shopUi;

    private void Start()
    {
        LoadCosmeticsOnPlayer();
    }

    private void LoadCosmeticsOnPlayer()
    {
        List<Cosmetic> cosmetics = CosmeticManager.Instance.GetCosmetics(_type);
        int id = -1;
        if (CosmeticManager.Instance.CurrentCosmetics.TryGetValue(_type, out Cosmetic equipped))
        {
            id = equipped.Id;
        }

        foreach (Cosmetic cosmetic in cosmetics)
        {
            ShopItem shopItem = Instantiate(_shopItemPrefab, _shopContainer).GetComponent<ShopItem>();
            shopItem.Init(cosmetic, this);
            if (id == cosmetic.Id)
            {
                SetEquipped(shopItem);
            }
        }
    }

    public void LoadEquippedItems()
    {
        var shopitems = FindObjectsOfType<ShopItem>().ToList();

        foreach (Cosmetic c in CosmeticManager.Instance.CurrentCosmetics.Values)
        {
            foreach (ShopItem si in shopitems)
            {
                if (c == si.ShopCosmetic)
                {
                    _shopUi.ApplyCosmetic(c.Type, si == null ? null : si.ShopCosmetic);
                }
            }
        }
    }

    public void Init(ShopUI shopUI)
    {
        _shopUi = shopUI;
    }

    public void SetEquipped(ShopItem shopItem)
    {
        if (_equipped != null)
        {
            _equipped.SetEquipped(false);
        }

        _equipped = shopItem;
        if (_equipped != null)
        {
            _equipped.SetEquipped(true);
        }

        _shopUi.ApplyCosmetic(_type, shopItem == null ? null : shopItem.ShopCosmetic);
    }

    public void SetPreview(ShopItem shopItem)
    {
        _shopUi.ApplyCosmetic(_type, shopItem == null ? null : shopItem.ShopCosmetic);
    }
}
