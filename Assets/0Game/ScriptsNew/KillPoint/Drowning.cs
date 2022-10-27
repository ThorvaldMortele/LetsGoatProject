using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drowning : MonoBehaviour
{
    [SerializeField] private float _drowningTime = 5;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer.Equals(6))
        {
            other.GetComponent<Goat>().ProgressBar.transform.parent.gameObject.SetActive(true);
            other.GetComponent<Goat>().HasDrowned = false;
            other.GetComponent<Goat>().DrowningTimer = TickTimer.CreateFromSeconds(FindObjectOfType<NetworkRunner>(), _drowningTime);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer.Equals(6))
        {
            var player = other.GetComponent<Goat>();

            if (!player.HasDrowned)
            {
                var progressbar = player.ProgressBar;

                float progress = 1 - (player.DrowningTimer.RemainingTime(FindObjectOfType<NetworkRunner>()).Value / _drowningTime);
                progressbar.UpdateProgress(progress);

                if (player.DrowningTimer.Expired(FindObjectOfType<NetworkRunner>()))
                {
                    Goat.DrowningPlayerEvent.Invoke(player);

                    player.SendDrowningKillFeed();

                    player.CanMove = false;
                    other.GetComponent<NetworkCharacterControllerPrototype>().gravity = -30;

                    GameManager.Instance.KillPlayer(player);
                    player.HasDrowned = true;
                    player.ProgressBar.transform.parent.gameObject.SetActive(false);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer.Equals(6))
        {
            other.GetComponent<Goat>().DrowningTimer = TickTimer.None;
            other.GetComponent<Goat>().ProgressBar.transform.parent.gameObject.SetActive(false);
        }
    }
}
