using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OVRFingerCapsuleSizer : MonoBehaviour
{
	public bool updateRadiusOnTick;
	public float fingerRadius = 0.01f;

	private OVRSkeleton skeleton;
	private IList<OVRBoneCapsule> handBoneCapsules;
	private bool capsulesInit;
	
	// Start is called before the first frame update
    IEnumerator Start()
    {
		skeleton = GetComponent<OVRSkeleton>();

		if (skeleton) {
			while (!skeleton.IsInitialized) {
				yield return null;
			}

			handBoneCapsules = skeleton.Capsules;

			foreach (var capsule in handBoneCapsules) {
				if (!capsule.CapsuleCollider.gameObject.name.Contains("Hand_Start") && !capsule.CapsuleCollider.gameObject.name.Contains("Hand_Thumb1")) {
					capsule.CapsuleCollider.radius = fingerRadius;
				}
			}

			capsulesInit = true;
		}
    }

	void Update() {
		if (!updateRadiusOnTick || !capsulesInit)
			return;
		
		foreach (var capsule in handBoneCapsules) {
			if (!capsule.CapsuleCollider.gameObject.name.Contains("Hand_Start") && !capsule.CapsuleCollider.gameObject.name.Contains("Hand_Thumb1")) {
				capsule.CapsuleCollider.radius = fingerRadius;
			}
		}
	}
}
