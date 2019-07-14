using UnityEngine;
using System.Collections;

public class CameraFacingCanvas : MonoBehaviour {
	public Camera m_Camera;

	void Start(){
		if (!m_Camera) {
			Debug.Log ("Setting the Main Camera in " + this.name);
			m_Camera = Camera.main;
		}

	}

	void Update(){
		transform.LookAt(transform.position + m_Camera.transform.rotation * Vector3.forward, m_Camera.transform.rotation * Vector3.up);
	}
}