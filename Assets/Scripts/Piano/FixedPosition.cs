using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedPosition : MonoBehaviour
{
	private new Rigidbody rigidbody;
	private Vector3 targetPosition;

	// Start is called before the first frame update
	void Start()
	{
		rigidbody = GetComponent<Rigidbody>();
		targetPosition = transform.position;
	}

	// Update is called once per frame
	void FixedUpdate()
	{
		rigidbody.position = targetPosition;
	}
}
