using System;

public enum State {};

public class Character
{
    public float speed {get; private set;}
    public int hp {get; private set;}
    public State state {get; private set;}

    public void SetSpeed(float speed) { this.speed = Math.Clamp(this.speed + speed, 0.0f, float.MaxValue); }
    public void SetHp(int hp) { this.hp = Math.Clamp(this.hp + hp, 0, int.MaxValue); }
    public void SetState(State s) { this.state = s; }
}