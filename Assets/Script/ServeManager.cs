using Unity.VisualScripting;
using UnityEngine;
public enum Serve { Ally,Enemy }

public class ServeManager : MonoBehaviour
{
    public static ServeManager Instance;
    public Serve currentPossesion = Serve.Enemy;

    public void ChangePossesion(Serve nextTeam)
    {
        currentPossesion=nextTeam;
        Debug.Log($"Turn Switched to :{nextTeam}");
        Debug.Log($"currentphase:{MatchManager.Instance.currentPhase},currentPossesion:{MatchManager.Instance.currentPossesion}");
  
    }
    /*
    public void OnResetButtonClicked()
    {
        currentPhase=GamePhase.Waiting;
        currentPossesion=Team.Ally;
        touchCount=0;
        ReceiverAllyEnemy[] receivers = FindObjectsByType<ReceiverAllyEnemy>(FindObjectsSortMode.None);
    }*/
}