using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawSpawn : MonoBehaviour
{
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
