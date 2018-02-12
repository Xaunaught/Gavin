using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeLookCamera : MonoBehaviour
{

    public float minViewX, maxViewX, minViewY, maxViewY, sensitivity;
    public Pawn pawn;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        float rotation = pawn.CamVector.x * sensitivity * Time.deltaTime;
        if (WrapAngle(transform.eulerAngles.x - rotation) > minViewX && WrapAngle(transform.eulerAngles.x - rotation) < maxViewX)
            transform.Rotate(Vector3.right, -rotation, Space.Self);

        rotation = pawn.CamVector.y * sensitivity * Time.deltaTime;
        if (WrapAngle(transform.eulerAngles.y + rotation) > minViewY && WrapAngle(transform.eulerAngles.y + rotation) < maxViewY)
            transform.Rotate(Vector3.up, rotation, Space.Self);
        transform.eulerAngles = Vector3.Scale(transform.eulerAngles, new Vector3(1, 1, 0));
    }
    private static float WrapAngle(float angle)
    {
        angle %= 360;
        if (angle > 180)
            return angle - 360;

        return angle;
    }
}
