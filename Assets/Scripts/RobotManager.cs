using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;

public class RobotManager : MonoBehaviour
{
    [Header("RobotType")]
    [EnumToggleButtons, HideLabel]
    public RobotType robotType;

    [Header("Target Tracking")]
    public GameObject target;
    public GameObject targetPrefab;
    public GameObject forks;
    public GameObject forkZone;
	private NavMeshAgent agent;
    public InteratctionManager playerManager;


    [SerializeField]
    private bool isMoving = false;


    public enum RobotType {Roomba, Forklift}

    // Use this for initialization
    void Start ()
	{
		agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update ()
	{
        if (playerManager.activeRobot == gameObject)
        {
            gameObject.GetComponent<ColourSwitcher>().ActiveOn();
        }
        if(playerManager.activeRobot != gameObject)
        {
            gameObject.GetComponent<ColourSwitcher>().ActiveOff();
        }

        if(target != null)
        {
            if (isMoving)
            {
                agent.SetDestination(target.transform.localPosition);
            }

            if (target.transform.position.x == gameObject.transform.position.x && target.transform.position.z == gameObject.transform.position.z)
            {
                Destroy(target.gameObject);
                //TODO: pick up cube
                if(forks != null)
                {
                    if (forks.GetComponent<ForkliftForks>().IsTouchingPickup())
                    {
                        LeanTween.move(forks.GetComponent<ForkliftForks>().pickup, forkZone.transform, 1f);
                        forks.GetComponent<ForkliftForks>().pickup.transform.parent = forkZone.transform;
                    }
                }
                isMoving = false;
            }
        }
        

        if (Input.GetMouseButtonDown(0) && playerManager.activeRobot == gameObject)
        {
            
            Ray toMouse = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit rhInfo;
            bool didHit = Physics.Raycast(toMouse, out rhInfo, 500f);
            
            if (didHit && rhInfo.collider.tag == "Ground")
            {
                isMoving = true;
                if (target == null)
                {
                    target = GameObject.Instantiate(targetPrefab, rhInfo.point, new Quaternion());
                }
                target.gameObject.transform.position = rhInfo.point;
            }
            if(didHit && rhInfo.collider.tag == "RobotTrigger")
            {
                isMoving = true;
                if(target == null)
                {
                    target = GameObject.Instantiate(targetPrefab, rhInfo.collider.GetComponent<RobotTrigger>().zone.transform.position, new Quaternion());
                }
                target.gameObject.transform.position = rhInfo.collider.GetComponent<RobotTrigger>().zone.transform.position;
            }
        }
    }
}
