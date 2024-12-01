using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightRay : MonoBehaviour
{    
    void OnDrawGizmos()
    {
        Transform transform = GetComponent<Transform>();

        Gizmos.DrawRay(transform.position, transform.forward * 1000.0f);
    }
}
