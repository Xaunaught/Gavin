using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
public class Crane : Pawn
{
    public GameObject cranePart;
    public Transform grabLocation;
    CranePath path;
    public float tollerance = .1f;
    public float speed = 10;
    public float high, low;
    GameObject holding;
    bool loading = false;
    // Use this for initialization
    protected override void Start()
    {
        base.Start();
        path = GetComponent<CranePath>();
        cranePart.transform.position = path.nodes[0].position;
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        if (loading)
            return;
        float distanceToClosest = Mathf.Infinity;
        CraneNode closest = null;
        foreach (var node in path.nodes)
        {
            if (Vector3.Distance(cranePart.transform.position, node.position) < distanceToClosest)
            {
                distanceToClosest = Vector3.Distance(cranePart.transform.position, node.position);
                closest = node;
            }
        }
        bool movedX = false, movedY = false;
        foreach (var node in closest.connections)
        {
            if (MoveVector.x != 0 && !movedX)
            {
                if (Mathf.Abs(cranePart.transform.position.z - closest.position.z) < tollerance && Vector3.Angle(Vector3.right * MoveVector.x, node.direction) < 10)
                {
                    cranePart.transform.position += Vector3.right * MoveVector.x * speed * Time.deltaTime;
                    movedX = true;
                }
                else if (Mathf.Abs(cranePart.transform.position.z - closest.position.z) < tollerance && Vector3.Angle(Vector3.right * MoveVector.x, closest.position - cranePart.transform.position) < 30)
                {
                    cranePart.transform.position += Vector3.right * MoveVector.x * speed * Time.deltaTime;
                    movedX = true;
                }
            }
            if (MoveVector.z != 0 && !movedY)
            {
                if (Mathf.Abs(cranePart.transform.position.x - closest.position.x) < tollerance && Vector3.Angle(Vector3.forward * MoveVector.z, node.direction) < 10)
                {
                    cranePart.transform.position += Vector3.forward * MoveVector.z * speed * Time.deltaTime;
                    movedY = true;
                }
                else if (Mathf.Abs(cranePart.transform.position.x - closest.position.x) < tollerance && Vector3.Angle(Vector3.forward * MoveVector.z, (closest.position - cranePart.transform.position).normalized) < 30)
                {
                    cranePart.transform.position += Vector3.forward * MoveVector.z * speed * Time.deltaTime;
                    movedY = true;
                }
            }
        }
    }
    public override void OnFire1Pressed()
    {
        if (!holding && !loading)
        {
            RaycastHit hit;
            if (Physics.SphereCast(cranePart.transform.position, .5f, -cranePart.transform.up * 1000, out hit))
            {
                if (hit.rigidbody)
                {
                    holding = hit.transform.gameObject;
                    Grab();
                }
            }
        }
        else if (holding)
        {
            holding.GetComponent<Rigidbody>().isKinematic = false;
            holding.transform.parent = null;
            holding = null;
        }
    }
    void Grab()
    {
        grabLocation.localPosition = Vector3.up * ((high + low) / 2);
        holding.GetComponent<Rigidbody>().isKinematic = true;
        LeanTween.move(holding, grabLocation.position, 0.5f).setOnComplete(TweenDone);
        LeanTween.rotate(holding, grabLocation.eulerAngles, 0.2f);
        loading = true;
    }
    void TweenDone()
    {
        loading = false;
        if (holding)
            holding.transform.parent = grabLocation;
    }
}

