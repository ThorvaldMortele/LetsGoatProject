using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField]
    private List<Button> _buttons = new List<Button>();

    private List<Transform> _shopPages = new List<Transform>();

    [SerializeField]
    private Transform _active;
    [SerializeField]
    private Transform _inactive;

    [SerializeField]
    private Transform _hatSlot;
    [SerializeField]
    private Transform _mouthSlot;
    [SerializeField]
    private Transform _backSlot;
    private GameObject _hat;

    [SerializeField] private Material _defaultTattooMat;
    [SerializeField] private Material _defaultGoatMat;

    [SerializeField] private SkinnedMeshRenderer _goatSkinnedMeshRenderer;

    private void Awake()
    {
        foreach (Button button in _buttons)
        {
            ShopPage shopPage = button.GetComponentInParent<ShopPage>();
            if (shopPage != null)
            {
                _shopPages.Add(shopPage.transform);
                button.onClick.AddListener(() => shopPage.transform.SetParent(_active));
                shopPage.Init(this);
            }
            button.onClick.AddListener(() => button.interactable = false);
        }

        if (_buttons.Count != _shopPages.Count)
            return;
        for (int i = 0; i < _buttons.Count; i++)
        {
            Button button = _buttons[i];
            for (int j = 0; j < _shopPages.Count; j++)
            {
                if (i != j)
                {
                    Button otherButton = _buttons[j];
                    Transform shopPage = _shopPages[j];
                    button.onClick.AddListener(() => otherButton.interactable = true);
                    button.onClick.AddListener(() => shopPage.SetParent(_inactive));
                }
            }
        }
    }

    private void OnDisable()
    {
        CosmeticManager.Instance.SaveCosmetics();
    }

    public void ApplyCosmetic(Cosmetic.CosmeticType type, Cosmetic cosmetic)
    {
        switch (type)
        {
            case Cosmetic.CosmeticType.Hat:
                if (_hat != null)
                {
                    Destroy(_hat);
                }
                if (cosmetic != null)
                {
                    switch (cosmetic.HatPosition)
                    {
                        case Cosmetic.HatType.Head:
                            _hat = Instantiate(cosmetic.GameObject, _hatSlot);
                            break;
                        case Cosmetic.HatType.Mouth:
                            _hat = Instantiate(cosmetic.GameObject, _mouthSlot);
                            break;
                        case Cosmetic.HatType.Back:
                            _hat = Instantiate(cosmetic.GameObject, _backSlot);
                            break;
                    }
                }
                break;
            case Cosmetic.CosmeticType.Pattern:
                if (cosmetic == null)
                {
                    Material[] mats = _goatSkinnedMeshRenderer.materials;

                    mats[0] = _defaultGoatMat;

                    _goatSkinnedMeshRenderer.materials = mats;
                }
                if (cosmetic != null)
                {
                    Material[] mats = _goatSkinnedMeshRenderer.materials;

                    mats[0] = cosmetic.Material;

                    _goatSkinnedMeshRenderer.materials = mats;
                }
                break;
            case Cosmetic.CosmeticType.Tattoo:
                if (cosmetic == null)
                {
                    Material[] mats = _goatSkinnedMeshRenderer.materials;

                    mats[1] = _defaultTattooMat;

                    _goatSkinnedMeshRenderer.materials = mats;
                }
                if (cosmetic != null)
                {
                    Material[] mats = _goatSkinnedMeshRenderer.materials;

                    mats[1] = cosmetic.Material;

                    _goatSkinnedMeshRenderer.materials = mats;
                }
                break;
            case Cosmetic.CosmeticType.Trail:
                break;
        }
    }
}
