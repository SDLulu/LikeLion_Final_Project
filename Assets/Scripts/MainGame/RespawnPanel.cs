using Fusion;
using TMPro;
using UnityEngine;

public class RespawnPanel : NetworkBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TextMeshProUGUI respawnAmountText;
    [SerializeField] private GameObject childObj;

    public override void Spawned()
    {
        Runner.SetIsSimulated(Object, true);
    }

    public override void FixedUpdateNetwork()
    {
        if (Utils.IsLocalPlayer(Object))
        {
            var timerIsRunning = playerController.RespawnTimer.IsRunning;
            childObj.SetActive(timerIsRunning);

            if (timerIsRunning && playerController.RespawnTimer.RemainingTime(Runner).HasValue)
            {
                var time = playerController.RespawnTimer.RemainingTime(Runner).Value;
                var roundInt = Mathf.RoundToInt(time);
                respawnAmountText.text = roundInt.ToString();
            }
        }
    }
}
