using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drowning : MonoBehaviour
{
    [SerializeField] private float _drowningTime = 5;

    private void OnTriggerEnter(Collider other)
    {
        if (GameManagerNew.PlayState != GameManagerNew.GamePlayState.Level) return;

        if (other.gameObject.layer.Equals(6))
        {
            other.GetComponent<Player>().ProgressBar.transform.parent.gameObject.SetActive(true);
            other.GetComponent<Player>().HasDrowned = false;
            other.GetComponent<Player>().DrowningTimer = TickTimer.CreateFromSeconds(FindObjectOfType<NetworkRunner>(), _drowningTime);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (GameManagerNew.PlayState != GameManagerNew.GamePlayState.Level) return;

        if (other.gameObject.layer.Equals(6))
        {
            var player = other.GetComponent<Player>();

            if (!player.HasDrowned)
            {
                var progressbar = player.ProgressBar;

                float progress = 1 - (player.DrowningTimer.RemainingTime(FindObjectOfType<NetworkRunner>()).Value / _drowningTime);
                progressbar.UpdateProgress(progress);

                if (player.DrowningTimer.Expired(FindObjectOfType<NetworkRunner>()))
                {
                    Player.DrowningPlayerEvent.Invoke(player);

                    player.SendDrowningKillFeed();

                    player.CanMove = false;
                    other.GetComponent<GoatController>().ApplyGravity = true;

                    GameManagerNew.Instance.KillPlayer(player);
                    player.HasDrowned = true;
                    player.ProgressBar.transform.parent.gameObject.SetActive(false);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (GameManagerNew.PlayState != GameManagerNew.GamePlayState.Level) return;

        if (other.gameObject.layer.Equals(6))
        {
            other.GetComponent<Player>().DrowningTimer = TickTimer.None;
            other.GetComponent<Player>().ProgressBar.transform.parent.gameObject.SetActive(false);
        }
    }
}
