using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecurityDrone : RoombaControl {

    public ReplacementShaderScript replacementShader;
    public override void OnFire1Pressed()
    {
        base.OnFire1Pressed();
        replacementShader.ToggleShader();
    }
}
