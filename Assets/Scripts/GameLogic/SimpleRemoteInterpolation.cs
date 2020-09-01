using UnityEngine;
using System.Collections;

/**
 * Extremely simple and dumb interpolation script.
 * But it works for this example.
 */
public class SimpleRemoteInterpolation : MonoBehaviour {


	private Vector3 desiredPos;
	private Quaternion desiredRot;
	
	private float dampingFactor = 5f;
	
	void Start() {
		desiredPos = this.transform.position;
		desiredRot = this.transform.rotation;
	}
	
	public void SetTransform(Vector3 pos, Quaternion rot, bool interpolate) {
		// If interpolation, then set the desired pososition+rotation; else force set (for spawning new models)
		if (interpolate) {
			desiredPos = pos;
			desiredRot = rot;
		} else {
			this.transform.position = pos;
			this.transform.rotation = rot;
		}
	}
	
	void Update () {
		this.transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * dampingFactor);
		this.transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * dampingFactor);
	}
}
