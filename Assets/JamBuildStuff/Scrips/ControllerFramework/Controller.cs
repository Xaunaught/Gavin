using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour {
    public Pawn posessedPawn;
    public float posessTime = 0.3f;
    public float FOVStart = 90;
    public float FOVEnd = 30;
    public void PosessPawn(Pawn target)
    {
        if (target.cam)
            target.cam.fieldOfView = FOVEnd;
        StartCoroutine(posessPawn(target));
    }

    public void UnposessPawn()
    {
        if (posessedPawn)
        {
            posessedPawn.UpdateMoveVector(Vector3.zero);
            posessedPawn.UpdateCamVector(Vector3.zero);
        }
        LeanTween.value(gameObject, delegate(float f) { if(posessedPawn) if (posessedPawn.cam) posessedPawn.cam.fieldOfView = f; },FOVStart, FOVEnd, posessTime).setOnComplete(unposessPawn);
    }

    protected virtual IEnumerator posessPawn(Pawn target)
    {
        UnposessPawn();
        if(target.controller != null)
        {
            target.controller.UnposessPawn();
        }
        yield return new WaitForSeconds(posessTime);
        posessedPawn = target;
        target.controller = this;
        LeanTween.value(gameObject, delegate (float f) { if (posessedPawn.cam) posessedPawn.cam.fieldOfView = f; }, FOVEnd, FOVStart, posessTime);
    }

    protected virtual void unposessPawn()
    {
        posessedPawn.controller = null;
        posessedPawn = null;
    }
}
