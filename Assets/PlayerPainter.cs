using UnityEngine;
using System.Collections;
using VildNinja.Voxels;
using System.Text;
using System.Collections.Generic;
using VildNinja.Voxels.Web;

public class PlayerPainter : MonoBehaviour {

    public byte color = 2;
    public float radius = 1;

    public float speed = 2;
    public float boost = 5;
    public float sensitivity = 2;

    public Transform cursor;
    public Transform camera;

    private Vector3 mouse;

    private float depth = 16;
    private Material material;
    private Rigidbody body;

    private int textValue = 0;
    private bool negative = false;

    private WebManager web;

    // Use this for initialization
    void Start () {
        material = cursor.GetComponent<Renderer>().material;
        material.color = MarchingCubes.Builder.colorMap[color];
        body = GetComponent<Rigidbody>();

        web = FindObjectOfType<WebManager>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        if (Input.GetMouseButtonUp(1))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        if (Input.GetMouseButton(1))
        {
            mouse += sensitivity * new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0);
            mouse.x = Mathf.Clamp(mouse.x, -90, 90);
            camera.localEulerAngles = new Vector3(mouse.x, 0, 0);
            transform.localEulerAngles = new Vector3(0, mouse.y, 0);
        }
        
        for (int i = 0; i < Input.inputString.Length; i++)
        {
            char c = Input.inputString[i];
            if (c == '-')
            {
                textValue = 0;
                negative = true;
            }
            else if (c == '+')
            {
                textValue = 0;
                negative = false;
            }
            else if (char.IsDigit(c))
            {
                textValue = textValue * 10 + int.Parse(c.ToString());
            }
            else
            {
                var pos = transform.position;
                if (c == 'x') pos.x = textValue;
                if (c == 'y') pos.y = textValue;
                if (c == 'z') pos.z = textValue;
                transform.position = pos;
            }
        }

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);


        if (Input.GetMouseButton(0))
        {
            ChunkManager.Instance.Draw(cursor.position, radius, color);
        }
        else
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                depth = hit.distance;
            }
            depth = Mathf.Clamp(depth, 1, 16);
        }
        cursor.position = ray.origin + ray.direction * depth;

        Event evt = new Event();
        while (Event.PopEvent(evt))
        {
            if (evt.type == EventType.ScrollWheel)
            {
                int c = color + Mathf.RoundToInt(Mathf.Sign(evt.delta.y));
                if (c < 1)
                {
                    c = MarchingCubes.Builder.colorMap.Length - 1;
                }
                else if (c >= MarchingCubes.Builder.colorMap.Length)
                {
                    c = 1;
                }
                color = (byte)c;


                material.color = MarchingCubes.Builder.colorMap[color];
            }
        }
    }

    void FixedUpdate()
    {
        Vector3 move = Input.GetAxis("Horizontal") * transform.right +
            Input.GetAxis("UpDown") * Vector3.up +
            Input.GetAxis("Vertical") * transform.forward;

        move *= speed + (Input.GetKey(KeyCode.LeftShift) ? boost : 0);

        body.AddForce(move, ForceMode.Acceleration);

        if (web != null)
        {
            web.position = transform.position;
        }
    }
}
