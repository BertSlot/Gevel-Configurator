using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeCamMove : MonoBehaviour {
	public Transform target;
	public float maxOffsetDistance = 2000f;
	public float orbitSpeed = 2f;
	public float panSpeed = 5f;
	public float zoomSpeed = 100f;
	private Vector3 targetOffset = Vector3.zero;
	private Vector3 targetPosition;

	public float mainSpeed = 100.0f; //regular speed
	public float shiftAdd = 250.0f; //multiplied by how long shift is held.  Basically running
	public float maxShift = 1000.0f; //Maximum speed when holdin gshift
	private float totalRun = 1.0f;


	// Use this for initialization
	void Start() {
		if (target != null)
			transform.LookAt(target);
	}



	void Update() {
		targetPosition = target.position + targetOffset;


		if (target != null) {
			targetPosition = target.position + targetOffset;

			// Left Mouse to Orbit
			if (Input.GetMouseButton(1)) {
				transform.RotateAround(targetPosition, Vector3.up, Input.GetAxis("Mouse X") * orbitSpeed);
				float pitchAngle = Vector3.Angle(Vector3.up, transform.forward);
				float pitchDelta = -Input.GetAxis("Mouse Y") * orbitSpeed;
				float newAngle = Mathf.Clamp(pitchAngle + pitchDelta, 0f, 180f);
				pitchDelta = newAngle - pitchAngle;
				transform.RotateAround(targetPosition, transform.right, pitchDelta);
			}
			// Right Mouse To Pan
			if (Input.GetMouseButton(0)) {
				Vector3 offset = transform.right * -Input.GetAxis("Mouse X") * panSpeed + transform.up * -Input.GetAxis("Mouse Y") * panSpeed;
				Vector3 newTargetOffset = Vector3.ClampMagnitude(targetOffset + offset, maxOffsetDistance);
				transform.position += newTargetOffset - targetOffset;
				//targetOffset = newTargetOffset;
			}

			var view = GetComponent<Camera>().ScreenToViewportPoint(Input.mousePosition);
			var isOutside = view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1;
			// Scroll to Zoom
			if (!isOutside)
				transform.position += transform.forward * Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;


			//Keyboard commands
			//float f = 0.0f;
			Vector3 p = GetBaseInput();
			if (Input.GetKey(KeyCode.LeftShift)) {
				totalRun += Time.deltaTime;
				p = p * totalRun * shiftAdd;
				p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
				p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
				p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
			} else {
				totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
				p *= mainSpeed;
			}

			p *= Time.deltaTime;
			Vector3 newPosition = transform.position;
			if (Input.GetKey(KeyCode.Space)) { //If player wants to move on X and Z axis only
				transform.Translate(p);
				newPosition.x = transform.position.x;
				newPosition.z = transform.position.z;
				transform.position = newPosition;
			} else {
				transform.Translate(p);
			}

		}



	}

	private Vector3 GetBaseInput() { //returns the basic values, if it's 0 than it's not active.
		Vector3 p_Velocity = new Vector3();
		if (Input.GetKey(KeyCode.W)) {
			p_Velocity += new Vector3(0, 0, 1);
		}
		if (Input.GetKey(KeyCode.S)) {
			p_Velocity += new Vector3(0, 0, -1);
		}
		if (Input.GetKey(KeyCode.A)) {
			p_Velocity += new Vector3(-1, 0, 0);
		}
		if (Input.GetKey(KeyCode.D)) {
			p_Velocity += new Vector3(1, 0, 0);
		}
		return p_Velocity;
	}
}// Class
