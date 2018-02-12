using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoombaMove : MonoBehaviour
{
    public float minView, maxView, sensitivity;
    public Pawn roomba;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        float rotation = roomba.CamVector.x * sensitivity;
        if (WrapAngle(transform.eulerAngles.x - rotation) > minView && WrapAngle(transform.eulerAngles.x - rotation) < maxView)
            transform.Rotate(Vector3.right, -rotation, Space.Self);

    }
    private static float WrapAngle(float angle)
    {
        angle %= 360;
        if (angle > 180)
            return angle - 360;

        return angle;
    }
}
