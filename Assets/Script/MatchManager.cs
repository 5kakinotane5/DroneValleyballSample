using UnityEngine;
public enum Team { Ally,Enemy }

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance;
    public Team currentPossesion = Team.Ally;
    public int touchCount = 0;

    public enum GamePhase { Waiting,Receiving,Striking,Returning}
    public GamePhase currentPhase=GamePhase.Waiting;
    void Awake() => Instance=this;

    public void ChangePossesion(Team nextTeam)
    {
        currentPossesion=nextTeam;
        touchCount=0;
        currentPhase =GamePhase.Receiving;
        Debug.Log($"Turn Switched to :{nextTeam}");        
    }
}