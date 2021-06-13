using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnZone
{

    public Vector3[] spot = new Vector3[3];

    public SpawnZone(Vector3 pointOne, Vector3 pointTwo, Vector3 pointThree)
    {
        spot[0] = pointOne;
        spot[1] = pointTwo;
        spot[2] = pointThree;
    }

}
