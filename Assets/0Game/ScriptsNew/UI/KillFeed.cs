using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillFeed : MonoBehaviour
{
    public static KillFeed Instance;
    [SerializeField] private GameObject _killListingPrefab;

    private void Start()
    {
        Instance = this;
    }

    public void AddNewKillListingHow(string killer, string how)
    {
        GameObject tmp = Instantiate(_killListingPrefab, transform);
        tmp.transform.SetSiblingIndex(0);
        KillListing tmpListing = tmp.GetComponent<KillListing>();
        tmpListing.SetNamesAndHow(killer, how);
    }
}
