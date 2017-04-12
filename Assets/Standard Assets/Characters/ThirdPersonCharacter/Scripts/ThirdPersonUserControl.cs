using System;
using UnityEngine;

namespace UnityStandardAssets.Characters.ThirdPerson
{
    [RequireComponent(typeof (ThirdPersonCharacter))]
    public class ThirdPersonUserControl : MonoBehaviour
    {
        private ThirdPersonCharacter m_Character; // A reference to the ThirdPersonCharacter on the object
        private Transform m_Cam;                  // A reference to the main camera in the scenes transform
        private Vector3 m_CamForward;             // The current forward direction of the camera
        private Vector3 m_Move;
        private bool m_Jump;                      // the world-relative desired move direction, calculated from the camForward and user input.

        
        private void Start()
        {
            // get the transform of the main camera
            if (Camera.main != null)
            {
                m_Cam = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning(
                    "Warning: no main camera found. Third person character needs a Camera tagged \"MainCamera\", for camera-relative controls.", gameObject);
                // we use self-relative controls in this case, which probably isn't what the user wants, but hey, we warned them!
            }

            // get the third person character ( this should never be null due to require component )
            m_Character = GetComponent<ThirdPersonCharacter>();
        }


        private void Update()
        {
            if (!m_Jump)
            {
                m_Jump = Input.GetKey(KeyCode.Space);
            }
        }

        public bool isRotating = false;
        // Fixed update is called in sync with physics
        private void FixedUpdate()
        {
            // read inputs
            // Angryboy: Hold right-mouse button to rotate
            if (Input.GetMouseButtonDown(1))
            {
                isRotating = true;
            }
            if (Input.GetMouseButtonUp(1))
            {
                isRotating = false;
            }
            

            float h = 0;
            float v = 0;

            if (Input.GetKey(KeyCode.W))
                v = 1;
            else if (Input.GetKey(KeyCode.S))
                v = -1;

            if (Input.GetKey(KeyCode.A))
                h = -1;
            else if (Input.GetKey(KeyCode.D))
                h = 1;

            if (isRotating)
            {
                // Made by LookForward
                // Angryboy: Replaced min/max Y with numbers, not sure why we had variables in the first place
                h = Input.GetAxis("Mouse X");
                //rotationY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                //rotationY = Mathf.Clamp(rotationY, -90, 90);
                //transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0.0f);
            }

            bool crouch = Input.GetKey(KeyCode.C);

            // calculate move direction to pass to character
            if (m_Cam != null)
            {
                // calculate camera relative direction to move:
                m_CamForward = Vector3.Scale(m_Cam.forward, new Vector3(1, 0, 1)).normalized;
                m_Move = v*m_CamForward + h*m_Cam.right;
            }
            else
            {
                // we use world-relative directions in the case of no main camera
                m_Move = v*Vector3.forward + h*Vector3.right;
            }
#if !MOBILE_INPUT
			// walk speed multiplier
	        if (Input.GetKey(KeyCode.LeftShift)) m_Move *= 0.5f;
#endif

            // pass all parameters to the character control script
            m_Character.Move(m_Move, crouch, m_Jump);
            m_Jump = false;
        }
    }
}
