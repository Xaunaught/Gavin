using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressurePlate : MonoBehaviour {
    public Vector3 endPos;
    public GameObject[] Triggerables;
    void OpenThings()
    {
        foreach (var item in Triggerables)
        {
            item.SendMessage("Trigger");
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if(collision.gameObject.tag == "Moveable" || collision.gameObject.tag == "Robot")
        {
            LeanTween.moveLocal(gameObject, endPos, 1).setEaseOutQuad().setOnComplete(OpenThings);
        }
    }
}
