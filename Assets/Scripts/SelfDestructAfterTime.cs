using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDestructAfterTime : MonoBehaviour
{
    // Start is called before the first frame update
    public float lifetime = 1.5f;
    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
