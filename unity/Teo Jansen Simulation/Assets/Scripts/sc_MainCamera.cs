using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sc_MainCamera : MonoBehaviour
{
    public Vector3 offset = new Vector3 (0.3f, 0.0f, -10.0f);
    Transform robot_body;

    // Start is called before the first frame update
    void Start()
    {
        robot_body = GameObject.Find("./body").transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = robot_body.position + offset;
    }
}
