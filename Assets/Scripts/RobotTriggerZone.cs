using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class RobotTriggerZone : MonoBehaviour
{
    [Header("Tag Enum")]
    [EnumToggleButtons, HideLabel]
    public RobotTypeToRecieve robotTypeToRevieve;

    public enum RobotTypeToRecieve { Roomba, Forklift }

    private RobotManager.RobotType recievedRobot;

    public GameObject objectToTween;
    // Use this for initialization
    void Start()
    {
        if(robotTypeToRevieve == RobotTypeToRecieve.Roomba)
        {
            recievedRobot = RobotManager.RobotType.Roomba;
        }
        if (robotTypeToRevieve == RobotTypeToRecieve.Forklift)
        {
            recievedRobot = RobotManager.RobotType.Forklift;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Robot")
        {
            if (other.GetComponent<RobotManager>() != null)
            {
                if(other.GetComponent<RobotManager>().robotType == recievedRobot)
                {
                    if(objectToTween.GetComponent<RobotTrigger>() != null)
                    {
                        objectToTween.GetComponent<RobotTrigger>().Tween();
                    }
                    
                }
                
            }
        }
    }
}
