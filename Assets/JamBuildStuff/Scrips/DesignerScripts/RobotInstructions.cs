using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RobotInstructions : MonoBehaviour {
    private PlayerController player;
    private Pawn pawn;

    private Image panel;

    public Text text;
    public Text showInstructions;

    public float fadetime = 5;

    [TextArea(3,10)]
    public string roombaInstrucitons;

    [TextArea(3, 10)]
    public string forkliftInstructions;

    [TextArea(3, 10)]
    public string securityInstruction;

    [TextArea(3, 10)]
    public string craneInstructions;

    [TextArea(3, 10)]
    public string defaultInstructions;

    private bool hiding;

    // Use this for initialization
	void Start ()
    {
        player = FindObjectOfType<PlayerController>();
        panel = GetComponent<Image>();
        pawn = player.posessedPawn;
        DisplayInstructions();
	}
	
	// Update is called once per frame
	void Update () {
        if (pawn != player.posessedPawn)
        {
            pawn = player.posessedPawn;
            DisplayInstructions();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            DisplayInstructions();
        }
	}

    private void DisplayInstructions()
    {
        if (pawn == null)
        {
            return;
        }
        if (pawn.pawnType=="Roomba")
        {
            text.text = roombaInstrucitons;
        }
        else if (pawn.pawnType == "ForkLift")
        {
            text.text = forkliftInstructions;
        }
        else if (pawn.pawnType == "Security")
        {
            text.text = securityInstruction;
        }
        else if (pawn.pawnType == "Crane")
        {
            text.text = craneInstructions;
        }
        else
        {
            text.text = defaultInstructions;
        }
        text.gameObject.SetActive(true);
        panel.enabled = true;
        showInstructions.gameObject.SetActive(false);
        if (!hiding)
        {
            StartCoroutine(HideAgain());
        }
    }

    private IEnumerator HideAgain()
    {
        hiding = true;
        yield return new WaitForSeconds(fadetime);

        text.gameObject.SetActive(false);
        panel.enabled = false;
        showInstructions.gameObject.SetActive(true);
        hiding = false;
    }
}
