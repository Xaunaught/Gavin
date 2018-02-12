using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : Controller
{
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (posessedPawn != null)
        {
            posessedPawn.UpdateMoveVector(new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")));
            posessedPawn.UpdateCamVector(new Vector3(Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"), 0));
            if (Input.GetButtonDown("Fire2"))
            {
                SendMessage("DoPosess");
            }
            if (Input.GetButtonDown("Fire1"))
            {
                posessedPawn.OnFire1Pressed();
            }
            if (Input.GetButtonUp("Fire1"))
            {
                posessedPawn.OnFire1Release();
            }
            if (Input.GetAxisRaw("Mouse ScrollWheel") != 0)
            {
                posessedPawn.OnMouseWheel(Input.GetAxisRaw("Mouse ScrollWheel"));
            }
        }
    }

    protected override IEnumerator posessPawn(Pawn target)
    {
        yield return base.posessPawn(target);
        if (target.cam)
        {
            target.cam.enabled = true;
            //target.cam.GetComponent<AudioListener>().enabled = true;
        }

    }
    protected override void unposessPawn()
    {
        if (posessedPawn)
        {
            if (posessedPawn.cam)
            {
                posessedPawn.cam.enabled = false;
                //posessedPawn.cam.GetComponent<AudioListener>().enabled = false;
            }
        }
        base.unposessPawn();

    }
}
