using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Player
{
    public string sessionToken { get; private set; }
    public Vector3 position { get; set; }
    public Player()
    {
        sessionToken = "";
        position = Vector3.zero;
    }

    public Player(string sessionToken)
    {
        this.sessionToken = sessionToken;
        position = Vector3.zero;
    }

    public Player(string sessionToken, Vector3 position)
    {
        this.sessionToken = sessionToken;
        this.position = position;
    }

    public void UpdatePosition(Vector3 newPosition)
    {
        position = newPosition;
    }
}
