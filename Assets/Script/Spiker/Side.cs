using UnityEngine;

public static class Side
{
    public static bool IsBallOnMySide(Team myTeam, Vector3 ballPos, float netX)
    {
        return myTeam == Team.Ally ? ballPos.x > netX : ballPos.x < netX;
    }
}
