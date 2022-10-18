using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GraphicColorChange : Graphic
{
    private List<Graphic> _childGraphics = new List<Graphic>();

    protected override void Awake()
    {
        base.Awake();
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 1; i < graphics.Length; i++)
        {
            _childGraphics.Add(graphics[i]);
        }
        base.color = Color.clear;
    }

    public override void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha)
    {
        foreach (Graphic graphic in _childGraphics)
        {
            graphic.CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha);
        }
    }

    public override void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha, bool useRGB)
    {
        foreach (Graphic graphic in _childGraphics)
        {
            graphic.CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha, useRGB);
        }
    }

    public override void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale)
    {
        foreach (Graphic graphic in _childGraphics)
        {
            graphic.CrossFadeAlpha(alpha, duration, ignoreTimeScale);
        }
    }
}
