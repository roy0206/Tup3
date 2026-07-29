using System;

public interface IHealthUIEvent
{
    public event Action<float, float> OnHealthChanged;
}
