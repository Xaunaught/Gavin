using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoombaControl : Pawn
{
    public float turnRate = 1;
   protected Rigidbody rb;
    public float moveSpeed = 5;
    public float slopeLimit = 15;
    public Vector3 rayNode;
    // Use this for initialization
    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.isKinematic = false;
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        RaycastHit rc = new RaycastHit();
        Vector3 diagonalForward = transform.rotation * new Vector3(0, -1, 2 * Mathf.Sign(MoveVector.z)).normalized;
        rb.MoveRotation(Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0, CamVector.y * Time.deltaTime * turnRate, 0)));
        Vector3 rn = rayNode;
        rn.z *= Mathf.Sign(MoveVector.z);
        if (Physics.Raycast(transform.TransformPoint(rn), diagonalForward , out rc, Mathf.Abs(MoveVector.z)))
        {
            if (Vector3.Dot(rc.normal, Vector3.up) > Mathf.Cos(15 * Mathf.Deg2Rad))
            {
                if (Mathf.Abs(MoveVector.z) > 0)
                {
                    transform.rotation *= Quaternion.FromToRotation(transform.up, rc.normal);
                    rb.MovePosition(transform.position + transform.forward * MoveVector.z * moveSpeed * Time.deltaTime);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 rn = rayNode;
        rn.z *= Mathf.Sign(MoveVector.z);
        Vector3 diagonalForward = transform.rotation * new Vector3(0, -1, 2 * Mathf.Sign(MoveVector.z)).normalized;
        Gizmos.DrawLine(transform.TransformPoint(rn), transform.TransformPoint(rn) + diagonalForward * Mathf.Abs(MoveVector.z));
    }
}
