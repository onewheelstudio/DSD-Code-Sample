using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Rigidbody))]
public class FlightPath : MonoBehaviour
{
    public float speed;
    public float maxAngularSpeed;
    public float angularSpeed;
    public Transform target1;
    public Transform target2;
    private Rigidbody rb;
    public float radius;
    public float angle;
    public float threshold = 0.5f;
    public int direction = -1;
    public float turnDistance = 5f;
    public float distance;
    public Transform target;
    public int passes = 3;
    private int currentPasses = 0;

    public bool gotNearTarget = false;

    private void Awake()
    {
        rb = this.GetComponent<Rigidbody>();
        target = target1;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position += this.transform.forward * speed * Time.deltaTime;
        float timeToCircle = 2f * Mathf.PI / maxAngularSpeed;
        float circumference = timeToCircle * speed;
        radius = circumference / (2f * Mathf.PI);

        distance = Distance();

        if (distance < threshold)
            gotNearTarget = true;

        if (gotNearTarget && distance > turnDistance * 2f)
            SwapTarget();

            angle = AngleBetween();

        if(distance > turnDistance)
            this.transform.Rotate(maxAngularSpeed *Vector3.up * Time.deltaTime);
    }

    private float AngleBetween()
    {
        Vector3 vectorToTarget = (target.position - this.transform.position);
        return Vector3.SignedAngle(this.transform.forward, vectorToTarget, Vector3.up);
    }

    private float Distance()
    {
        return (target.position - this.transform.position).magnitude;
    }

    private void SwapTarget()
    {
        currentPasses++;

        if (currentPasses >= passes)
        {

            if (target == target1)
                target = target2;
            else
                target = target1;
        }

        direction *= Mathf.RoundToInt(Mathf.Pow(-1, Random.Range(1,3)));

        gotNearTarget = false;
    }

    private float GetAngularSpeed()
    {
        float speed = maxAngularSpeed * (AngleBetween()) / 180f + maxAngularSpeed * 0.25f * Mathf.Sign(AngleBetween());
        angularSpeed = speed;
        return speed;
    }
}
