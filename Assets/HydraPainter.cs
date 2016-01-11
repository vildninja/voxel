using UnityEngine;
using System.Collections;

public class HydraPainter : MonoBehaviour
{
    private static int scene = 1;
    private static bool saved = true;

    public SixenseHands hand;
    private SixenseInput.Controller controller;

    public float scale = 0.1f;

    public byte color = 1;
    public float radius = 2;

    private Vector3 lastPosition;
    private Quaternion lastRotation;

    private bool started = false;

	// Use this for initialization
	IEnumerator Start ()
    {
        if (hand == SixenseHands.RIGHT)
        {
            ChunkManager.Instance.LoadChunks(scene + ".vox");
        }

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
            if (started)
            {
                ChunkManager.Instance.Draw(controller.Position * scale, radius * controller.Trigger, color);
                saved = false;
            }
	    }
        else
        {
            // when clicking trigger to initialize controller, the first draw is always an error.
            started = true;
        }

        if (controller.GetButton(SixenseButtons.BUMPER))
        {
            ChunkManager.Instance.Drag(controller.Position * scale,
                (controller.Position - lastPosition) * scale,
                controller.Rotation * Quaternion.Inverse(lastRotation));
        }

        lastPosition = controller.Position;
        lastRotation = controller.Rotation;

        if (controller.GetButtonDown(SixenseButtons.START))
        {
            ChunkManager.Instance.SaveChunks(scene + ".vox");
            saved = true;
        }

        if (saved)
        {
            bool load = false;
            if (controller.GetButtonDown(SixenseButtons.ONE))
            {
                load = true;
                scene = 1;
            }
            if (controller.GetButtonDown(SixenseButtons.TWO))
            {
                load = true;
                scene = 2;
            }
            if (controller.GetButtonDown(SixenseButtons.THREE))
            {
                load = true;
                scene = 3;
            }
            if (controller.GetButtonDown(SixenseButtons.FOUR))
            {
                load = true;
                scene = 4;
            }

            if (load)
            {
                ChunkManager.Instance.LoadChunks(scene + ".vox");
            }
        }
	}
}
