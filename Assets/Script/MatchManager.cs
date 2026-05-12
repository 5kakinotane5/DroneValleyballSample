using Unity.VisualScripting;
using UnityEngine;
public enum Team { Ally,Enemy }

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance;
    public Team currentPossesion = Team.Ally;
    public int touchCount = 0;

    public enum GamePhase { Waiting,Receiving,Spiking}
    public GamePhase currentPhase;
    void Awake() => Instance=this;

    public void ChangePossesion(Team nextTeam)
    {
        currentPossesion=nextTeam;
        touchCount=0;
        currentPhase =GamePhase.Receiving;
        Debug.Log($"Turn Switched to :{nextTeam}");
        Debug.Log($"currentphase:{MatchManager.Instance.currentPhase},currentPossesion:{MatchManager.Instance.currentPossesion}");
  
    }
    public void StartPlay()
    {
        currentPhase=GamePhase.Receiving;
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