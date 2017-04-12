using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace cam
{
     class camera_movement : MonoBehaviour
    {

        /*
         * Based on Windex's flycam script found here: http://forum.unity3d.com/threads/fly-cam-simple-cam-script.67042/
         * C# conversion created by Ellandar
         * Improved camera made by LookForward
         * Modifications created by Angryboy
         * 1) Have to hold right-click to rotate
         * 2) Made variables public for testing/designer purposes
         * 3) Y-axis now locked (as if space was always being held)
         * 4) Q/E keys are used to raise/lower the camera
         */

        public float mainSpeed = 100.0f; //regular speed
        public float shiftAdd = 250.0f; //multiplied by how long shift is held.  Basically running
        public float maxShift = 1000.0f; //Maximum speed when holdin gshift
        public float camSens = 0.25f; //How sensitive it with mouse
        private Vector3 lastMouse = new Vector3(255, 255, 255); //kind of in the middle of the screen, rather than at the top (play)
        private float totalRun = 1.0f;

        public bool isRotating = false; // Angryboy: Can be called by other things (e.g. UI) to see if camera is rotating
        private float speedMultiplier; // Angryboy: Used by Y axis to match the velocity on X/Z axis

        public float mouseSensitivity = 5.0f;        // Mouse rotation sensitivity.
        private float rotationY = 0.0f;
        Vector3 lastPos = Vector3.zero;
        static Properties p = null;
        void Start()
        {
            if (p == null)
                p = new Properties("nif2obj.properties");
        }
        void Update()
        {

            // Angryboy: Hold right-mouse button to rotate
            if (Input.GetMouseButtonDown(1))
            {
                isRotating = true;
            }
            if (Input.GetMouseButtonUp(1))
            {
                isRotating = false;
            }
            if (isRotating)
            {
                // Made by LookForward
                // Angryboy: Replaced min/max Y with numbers, not sure why we had variables in the first place
                float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivity;
                rotationY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                rotationY = Mathf.Clamp(rotationY, -90, 90);
                transform.localEulerAngles = new Vector3(rotationY, rotationX, 0.0f);
            }

            //Keyboard commands
            float f = 0.0f;
            Vector3 p = GetBaseInput();
            if (Input.GetKey(KeyCode.LeftShift))
            {
                totalRun += Time.deltaTime;
                p = p * totalRun * shiftAdd;
                p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
                p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
                p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
                // Angryboy: Use these to ensure that Y-plane is affected by the shift key as well
                speedMultiplier = totalRun * shiftAdd * Time.deltaTime;
                speedMultiplier = Mathf.Clamp(speedMultiplier, -maxShift, maxShift);
            }
            else
            {
                totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
                p = p * mainSpeed;
                speedMultiplier = mainSpeed * Time.deltaTime; // Angryboy: More "correct" speed
            }

            p = p * Time.deltaTime;

            // Angryboy: Removed key-press requirement, now perma-locked to the Y plane
            Vector3 newPosition = transform.position;//If player wants to move on X and Z axis only
            transform.Translate(p);
            newPosition.x = transform.position.x;
            newPosition.z = transform.position.z;

            // Angryboy: Manipulate Y plane by using Q/E keys
            if (Input.GetKey(KeyCode.Q))
            {
                newPosition.y += -speedMultiplier;
            }
            if (Input.GetKey(KeyCode.E))
            {
                newPosition.y += speedMultiplier;
            }

            transform.position = newPosition;

            if (newPosition != lastPos && !isRotating)
            {
                //checkHits(transform.position);
                lastPos = newPosition;
            }
        }


        // Angryboy: Can be called by other code to see if camera is rotating
        // Might be useful in UI to stop accidental clicks while turning?
        public bool amIRotating()
        {
            return isRotating;
        }

        private Vector3 GetBaseInput()
        { //returns the basic values, if it's 0 than it's not active.
            Vector3 p_Velocity = new Vector3();
            if (Input.GetKey(KeyCode.W))
            {
                p_Velocity += new Vector3(0, 0, 1);
            }
            if (Input.GetKey(KeyCode.S))
            {
                p_Velocity += new Vector3(0, 0, -1);
            }
            if (Input.GetKey(KeyCode.A))
            {
                p_Velocity += new Vector3(-1, 0, 0);
            }
            if (Input.GetKey(KeyCode.D))
            {
                p_Velocity += new Vector3(1, 0, 0);
            }
            return p_Velocity;
        }
    }

}