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

    private Vector2 lastThumb;
    private Material material;

    private bool started = false;

	// Use this for initialization
	IEnumerator Start ()
    {
        if (hand == SixenseHands.RIGHT)
        {
            ChunkManager.Instance.LoadChunks(scene + ".vox");
        }

        material = GetComponentInChildren<Renderer>().sharedMaterial;
        material.color = MarchingCubes.Builder.colorMap[color];

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

        var joystick = new Vector2(controller.JoystickX, controller.JoystickY);
        if (joystick.sqrMagnitude > 0.4f)
        {
            if (lastThumb.sqrMagnitude < 0.5f)
            {
                lastThumb = joystick.normalized;
            }

            if (Mathf.Abs(Vector3.Cross(joystick.normalized, lastThumb).z) > 0.5f)
            {
                int c = color + Mathf.RoundToInt(Mathf.Sign(Vector3.Cross(joystick.normalized, lastThumb).z));
                lastThumb = joystick.normalized;

                if (c < 0)
                {
                    c = (byte) (MarchingCubes.Builder.colorMap.Length - 1);
                }
                if (c >= MarchingCubes.Builder.colorMap.Length)
                {
                    c = 0;
                }

                color = (byte)c;

                material.color = MarchingCubes.Builder.colorMap[color];
            }
        }
        else
        {
            lastThumb = Vector2.zero;
        }
	}
}
