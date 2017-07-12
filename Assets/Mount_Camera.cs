using Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[AddComponentMenu("Camera-Control/Mount Orbit with zoom")]
public class Mount_Camera : MonoBehaviour
{
    public int button = 1;
    public Transform target;
    public float distance = 5.0f;
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;

    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;

    public float distanceMin = .5f;
    public float distanceMax = 15f;
    public float zOffset = 0;

    float x = 0.0f;
    float y = 0.0f;
    Vector3 lastPos = Vector3.zero;
    // Use this for initialization
    void Start()
    {
        setAngles();

        Dictionary<string, string> settings = DotNet.Config.AppSettings.Retrieve("telarafly.cfg");
        if (settings.ContainsKey("MOUNT_ZOFFSET"))
            zOffset = float.Parse(settings["MOUNT_ZOFFSET"]);


    }

    void setAngles()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }
    bool isRotating = false;
    void Update()
    {
        
        // Angryboy: Hold right-mouse button to rotate
        if (Input.GetMouseButtonDown(button))
        {
            isRotating = true;
        }
        if (Input.GetMouseButtonUp(button))
        {
            isRotating = false;
        }


        if (target)
        {
            Quaternion rotation;

            if (isRotating)
            {
                x += Input.GetAxis("Mouse X") * xSpeed * distance * 0.02f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * distance * 0.02f;
            }

            //y = ClampAngle(y, yMinLimit, yMaxLimit);
            rotation = Quaternion.Euler(y, x, 0);
            if (!lastPos.Equals(this.target.transform.position) || Input.GetMouseButton(1))
            {
                rotation = target.transform.rotation;
                lastPos = this.target.transform.position;
                setAngles();
            }
            Vector3 targetPos = target.position;

            if (Input.GetKey(KeyCode.LeftAlt))
            {
                zOffset += Input.GetAxis("Mouse ScrollWheel") * 5;
                Dictionary<string, string> settings = DotNet.Config.AppSettings.Retrieve("telarafly.cfg");
                if (!settings.ContainsKey("MOUNT_ZOFFSET"))
                    settings.Add("MOUNT_ZOFFSET", "" + zOffset);
                else
                    settings["MOUNT_ZOFFSET"] = "" + zOffset;
                DotNet.Config.AppSettings.saveFrom(settings, "telarafly.cfg");
            }
            else
            {
                distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * 5, distanceMin, distanceMax);
            }
            targetPos.y += zOffset;

            RaycastHit hit;
            if (Physics.Linecast(targetPos, transform.position, out hit))
            {
                distance -= hit.distance;
            }
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + targetPos;

            transform.rotation = rotation;
            transform.position = position;
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}
