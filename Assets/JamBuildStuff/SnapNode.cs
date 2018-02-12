using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapNode : MonoBehaviour
{
    public Transform snapLocation;
    public List<GameObject> snappedObjects;
    void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<Rigidbody>() && !other.GetComponent<Pawn>())
        {
            if (!snappedObjects.Contains(other.gameObject))
            {
                //other.transform.position = snapLocation.position;
                //other.transform.rotation = snapLocation.rotation
                snappedObjects.Add(other.gameObject);
                snappedObjects[0].GetComponent<Rigidbody>().isKinematic = true;
                LeanTween.rotate(other.gameObject, snapLocation.eulerAngles, 0.3f);
                LeanTween.move(other.gameObject, snapLocation.position, 1).setOnComplete(MakeKin);
            }
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (snappedObjects.Contains(other.gameObject))
        {
            snappedObjects.Remove(other.gameObject);
        }
    }

    void MakeKin()
    {
        snappedObjects[0].GetComponent<Rigidbody>().isKinematic = false;
    }
}
