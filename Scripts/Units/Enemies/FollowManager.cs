using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowManager : MonoBehaviour
{
    private List<Follower> followers = new List<Follower>();
    [SerializeField] private float lerpSpeed = 0.01f;

    public void RegisterFollower(Transform follower)
    {
        Follower _follower = new Follower(lerpSpeed * UnityEngine.Random.Range(0.5f, 1.5f), follower);
        followers.Add(_follower);
    }

    private void FixedUpdate()
    {
        foreach (var follower in followers)
        {
            follower.MoveToTarget();
        }
    }

}

public class Follower
{
    public Transform parent;
    public Vector3 localPosition;
    public float lerpSpeed = 0.1f;
    public Transform self;

    public Follower(float lerpSpeed, Transform self)
    {
        this.parent = self.parent;
        this.localPosition = self.localPosition;
        this.lerpSpeed = lerpSpeed;
        this.self = self;
    }

    public void MoveToTarget()
    {
        //Vector3 directionVector = this.parent.position + this.localPosition - this.transform.position;
        this.self.position = Vector3.Lerp(this.self.position, this.parent.position + this.localPosition, lerpSpeed);
        this.self.rotation = Quaternion.Lerp(this.self.rotation, this.parent.rotation, lerpSpeed);
    }

}
