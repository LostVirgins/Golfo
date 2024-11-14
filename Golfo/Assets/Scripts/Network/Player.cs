using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player
{
    public int Id { get; set; }
    public Vector3 Position { get; set; }

    public Player(int id, Vector3 position)
    {
        Id = id;
        Position = position;
    }

    public void UpdatePosition(Vector3 newPosition)
    {
        Position = newPosition;
    }
}
