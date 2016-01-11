﻿using UnityEngine;
using System.Collections;

public class HydraPainter : MonoBehaviour
{
    public SixenseHands hand;
    private SixenseInput.Controller controller;

    public float scale = 0.1f;

    public byte color = 1;
    public float radius = 2;

	// Use this for initialization
	IEnumerator Start ()
    {
	    while (SixenseInput.GetController(hand) == null)
	    {
	        yield return null;
	    }

	    controller = SixenseInput.GetController(hand);
    }
	
	// Update is called once per frame
	void Update ()
    {
	    if (controller == null)
	    {
	        return;
	    }

	    transform.position = controller.Position * scale;

        if (controller.Trigger > 0.3f)
	    {
	        ChunkManager.Instance.Draw(controller.Position * scale, radius * controller.Trigger, color);
	    }
	}
}
