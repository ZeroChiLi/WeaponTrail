using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMove : MonoBehaviour
{
    public float speed = 10f;
    public float time = 2f;
    public Vector3 forward = Vector3.right;

    private float _lastUpdateTime;
    private Vector3 _startPos;
    private void Start()
    {
        _startPos = transform.position;
        _lastUpdateTime = Time.time;
    }

    private void Update()
    {
        transform.position += Time.deltaTime * speed * forward;
        if (Time.time > _lastUpdateTime + time)
        {
            forward = -forward;
            _lastUpdateTime = Time.time;
        }
    }
}
