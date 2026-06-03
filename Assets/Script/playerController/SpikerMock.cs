// PlayerControllerからSpike()を受け取るスパイカーのインターフェース定義（モック）
public class SpikerMock
{
    public bool isReady;
    public float maxVelocity;

    public void Spike(float course, float velocity) { }
}
