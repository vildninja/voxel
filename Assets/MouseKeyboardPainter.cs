using UnityEngine;
using System.Collections;

public class MouseKeyboardPainter : MonoBehaviour
{

    public byte color = 1;
    public float radius = 2;

    public float speed = 2;
    public float boost = 5;

    // Use this for initialization
    void Start () {
	
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
	        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("UpDown"), Input.GetAxis("Vertical"));
	        move *= speed + (Input.GetKey(KeyCode.LeftShift) ? boost : 0);
            float spin = Input.GetAxis("Spin");
            spin *= speed + (Input.GetKey(KeyCode.LeftShift) ? boost : 0);

            Quaternion look = Quaternion.Euler(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), spin);
            
            transform.Translate(move * Time.deltaTime, Space.Self);
            transform.Rotate(look.eulerAngles, Space.Self);
	    }
	}
}
