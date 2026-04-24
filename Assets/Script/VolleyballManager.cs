using UnityEngine;
public enum GamePhase{Waiting,Receiving,Spiking};

public class VolleyballManager : MonoBehaviour
{
    public static VolleyballManager Instance;
    public GamePhase currentPhase=GamePhase.Waiting;

    void Awake(){ Instance = this; }

    public void StartPlay()
    {
        currentPhase=GamePhase.Receiving;
    }

    public void OnReceiveSuccess()
    {
        currentPhase=GamePhase.Spiking;
    }

    public void ResetPhase()
    {
        currentPhase=GamePhase.Waiting;
    }

}