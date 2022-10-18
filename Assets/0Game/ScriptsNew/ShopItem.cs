using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
    [SerializeField]
    private Image _icon;
    [SerializeField]
    private GameObject _buyLabel;
    [SerializeField]
    private GameObject _ownedLabel;
    [SerializeField]
    private TextMeshProUGUI _priceText;
    [SerializeField]
    private GameObject _equippedIcon;

    public Cosmetic ShopCosmetic;
    private ShopPage _shopPage;

    private void OnEnable()
    {
        //makes the moneyamount update when buying smth
        var screenUI = FindObjectOfType<StartScreenUI>();

        GetComponent<Button>().onClick.AddListener(delegate { screenUI.UpdateGoatBuxAmount(); });

        if (_shopPage != null)
            _shopPage.LoadEquippedItems();
    }

    public void Init(Cosmetic cosmetic, ShopPage shopPage)
    {
        ShopCosmetic = cosmetic;
        _shopPage = shopPage;

        _icon.sprite = cosmetic.Sprite;
        _priceText.text = cosmetic.Price.ToString();
        _buyLabel.SetActive(!cosmetic.Owned);
        _ownedLabel.SetActive(cosmetic.Owned);
        _equippedIcon.SetActive(false);
    }

    public void BuyEquip()
    {
        if (ShopCosmetic.GetCosmetic())
        {
            bool equip = CosmeticManager.Instance.SetCurrentCosmetic(ShopCosmetic);
            _shopPage.SetEquipped(equip ? this : null);
            _buyLabel.SetActive(false);
            _ownedLabel.SetActive(true);
        }
    }

    public void Equip()
    {
        if (ShopCosmetic.Owned)
        {
            bool equip = CosmeticManager.Instance.SetCurrentCosmetic(ShopCosmetic);
            _shopPage.SetEquipped(equip ? this : null);
        }
        else
        {
            _shopPage.SetPreview(this);
            //do it as preview
        }
    }

    public void SetEquipped(bool value)
    {
        _equippedIcon.SetActive(value);
    }
}
