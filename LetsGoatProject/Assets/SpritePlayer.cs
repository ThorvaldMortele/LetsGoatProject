using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpritePlayer : MonoBehaviour
{

    public List<Sprite> Speedlines;

    public void StartAnimation()
    {
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        int i;
        i = 0;
        while (i < Speedlines.Count)
        {
            GetComponent<Image>().sprite = Speedlines[i];
            i++;
            yield return new WaitForSeconds(0.07f);
            yield return 0;

        }
        StartCoroutine(Play());
    }
}
