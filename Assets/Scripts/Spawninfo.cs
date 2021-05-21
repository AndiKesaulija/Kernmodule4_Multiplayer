using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NetworkSpawnObject
{
    PLAYER = 0,
    BULLET = 1
}
[CreateAssetMenu(menuName = "My Assets/NetworkSpanwInfo")]
public class Spawninfo : ScriptableObject
{
    public List<GameObject> prefabList = new List<GameObject>();
}
