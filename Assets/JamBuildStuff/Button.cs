using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class Button : Pawn
{
    public UnityEvent myEvent;
    protected override void Start()
    {
        base.Start();
    }
    protected override void Update()
    {
        base.Update();
    }

    public void Pressed()
    {
        myEvent.Invoke();
    }
}
