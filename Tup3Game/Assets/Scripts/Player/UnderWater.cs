using System.Collections.Generic;
using UnityEngine;

public class UnderWater : MonoBehaviour
{
    private readonly HashSet<Collider2D> playerColliders = new HashSet<Collider2D>();
    private Playermovement activePlayer;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Playermovement movement = other.GetComponentInParent<Playermovement>();
        if (movement == null)
            return;

        if (activePlayer != null && activePlayer != movement)
        {
            activePlayer.SetInWater(this, false);
            playerColliders.Clear();
        }

        activePlayer = movement;
        playerColliders.Add(other);
        activePlayer.SetInWater(this, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!playerColliders.Remove(other) || playerColliders.Count > 0)
            return;

        if (activePlayer != null)
            activePlayer.SetInWater(this, false);

        activePlayer = null;
    }

    private void OnDisable()
    {
        if (activePlayer != null)
            activePlayer.SetInWater(this, false);

        activePlayer = null;
        playerColliders.Clear();
    }
}
