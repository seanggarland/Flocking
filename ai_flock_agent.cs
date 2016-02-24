using UnityEngine;
using System.Collections;

public class ai_flock_agent : MonoBehaviour
{

    public float maxSpeed { get; set; }
    public float safeRadius { get; set; }
    [HideInInspector]
    public Rigidbody _rigidbody;

    // Use this for initialization
    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    public void Update()
    {
        // Clamp the velocity of the rigidbody:
        if(_rigidbody.velocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            _rigidbody.velocity = _rigidbody.velocity.normalized;
            _rigidbody.velocity *= maxSpeed;
        }
    }
}
