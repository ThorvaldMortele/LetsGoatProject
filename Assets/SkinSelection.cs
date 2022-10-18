using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkinSelection : MonoBehaviour
{
    [SerializeField] private List<Sprite> _skins = new List<Sprite>();
    private Image _image;

    private int index;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void Start()
    {
        _image.sprite = _skins[0];
    }

    public void UpdateSkinRight()
    {
        FindObjectOfType<AudioManager>().Play("Click");
        index++;
        if (index + 1 > _skins.Count)
        {
            index = 0;
        }

        if (index <= _skins.Count - 1 && index >= 0)
        {
            _image.sprite = _skins[index];
        }
    }

    public void UpdateSkinLeft()
    {
        FindObjectOfType<AudioManager>().Play("Click");
        index--;
        if (index < 0)
        {
            index = _skins.Count - 1;
        }

        if (index <= _skins.Count - 1 && index >= 0)
        {
            _image.sprite = _skins[index];
        }
    }
}
