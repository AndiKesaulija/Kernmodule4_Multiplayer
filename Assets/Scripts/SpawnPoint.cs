using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint
{
    public SpawnZone[] zones =
    {
       new SpawnZone(new Vector3(-12, 2.5f, 25), new Vector3(0, 2.5f, 25), new Vector3(12, 2.5f, 25)),
       new SpawnZone(new Vector3(-12, 2.5f, 15), new Vector3(0, 2.5f, 15), new Vector3(12, 2.5f, 15)),
       new SpawnZone(new Vector3(-12, 2.5f, 5), new Vector3(0, 2.5f, 5), new Vector3(12, 2.5f, 5)),

       new SpawnZone(new Vector3(-12, 2.5f, -5), new Vector3(0, 2.5f, -5), new Vector3(12, 2.5f, -5)),
       new SpawnZone(new Vector3(-12, 2.5f, -15), new Vector3(0, 2.5f, -15), new Vector3(12, 2.5f, -15)),
       new SpawnZone(new Vector3(-12, 2.5f, -25), new Vector3(0, 2.5f, -25), new Vector3(12, 2.5f, -25)),


    };
}
