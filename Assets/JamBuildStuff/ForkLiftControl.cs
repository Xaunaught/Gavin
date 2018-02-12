using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ForkLiftControl : RoombaControl
{

    public Transform centerOfMass;
    List<Collider> ProngsTouching = new List<Collider>();
    public Transform parentingPoint;
    Rigidbody pickUpAble;
    GameObject holding;
    float holdingMass;
    public GameObject fork;
    public float forkLow = 0.379f, forkHigh = 1.203f;
    public float forkSpeed = 1f;
    bool loading = false;
    protected override void Start()
    {
        base.Start();     
        rb.centerOfMass = centerOfMass.localPosition;
    }

    protected override void Update()
    {
        if (loading)
            return;
        base.Update();
        float newY = fork.transform.localPosition.y + scrollAmount * forkSpeed * Time.deltaTime;
        if (newY > forkLow && newY < forkHigh)
        {
            fork.transform.localPosition = new Vector3(fork.transform.localPosition.x, newY, fork.transform.localPosition.z);
        }
        base.Update();
        scrollAmount = 0;
    }
    void OnTriggerEnter(Collider other)
    {
        if (ProngsTouching.Contains(other))
        {
            if (other.GetComponent<Rigidbody>())
            {
                pickUpAble = other.GetComponent<Rigidbody>();
            }
        }
        ProngsTouching.Add(other);
    }
    void OnTriggerExit(Collider other)
    {
        ProngsTouching.Remove(other);
        if (pickUpAble == other.GetComponent<Rigidbody>())
        {
            pickUpAble = null;
        }
    }
    public override void OnFire1Pressed()
    {
        base.OnFire1Pressed();
        if (pickUpAble && !holding)
        {
            ProngsTouching.Clear();
            holding = pickUpAble.gameObject;
            holdingMass = holding.GetComponent<Rigidbody>().mass;
            Destroy(holding.GetComponent<Rigidbody>());
            //holding.transform.position = parentingPoint.position;
            LeanTween.move(holding, parentingPoint.position, 0.5f).setOnComplete(TweenDone);
            LeanTween.rotate(holding, parentingPoint.eulerAngles, 0.2f);
            loading = true;
            //holding.transform.rotation = parentingPoint.rotation;
        }
        else if (holding)
        {
            ProngsTouching.Clear();
            holding.AddComponent(typeof(Rigidbody));
            holding.GetComponent<Rigidbody>().mass = holdingMass;
            holdingMass = 0;
            holding.transform.parent = null;
            holding = null;

        }
    }
    void TweenDone()
    {
        loading = false;
        holding.transform.parent = parentingPoint;
    }
}