using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPawn : Pawn {

    public float maxPitch, minPitch, maxYaw, minYaw, sensitivity;
    Vector3 baseAng, addAng;

    protected override void Start()
    {
        base.Start();
        baseAng = transform.rotation.eulerAngles;
        addAng = Vector3.zero;
    }


    // Update is called once per frame
    protected override void Update () {
        CamVector.x *= -1;

        addAng += CamVector * Time.deltaTime * sensitivity;

        addAng.x = Mathf.Clamp(addAng.x, minPitch, maxPitch);
        addAng.y = Mathf.Clamp(addAng.y, minYaw, maxYaw);
        transform.rotation = Quaternion.Euler(baseAng + addAng);
	}
}
