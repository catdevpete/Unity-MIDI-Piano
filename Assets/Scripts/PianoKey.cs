using UnityEngine;
using System.Collections;

public class PianoKey : MonoBehaviour
{
	public AudioSource AudioSource { get; set; }

	private bool _played = false;
	private IEnumerator _fadeCoro;

	private Vector3 _position;
	private Vector3 _rotation;

	private Rigidbody _rigidbody;
	private HingeJoint _springJoint;
	private ConstantForce _constantForce;
	private IEnumerator _playCoro;

	// Debug
	public bool TestPlay = false;

	void Awake()
	{
		AudioSource = GetComponent<AudioSource>();
		_rigidbody = GetComponent<Rigidbody>();
		_springJoint = GetComponent<HingeJoint>();
		_constantForce = GetComponent<ConstantForce>();

		_position = transform.position;
		_rotation = transform.eulerAngles;
	}

	// Update is called once per frame
	void Update()
	{
		Constrain();

		if (transform.eulerAngles.x > 350 && transform.eulerAngles.x < 359.5f && !_played)
		{
			if (AudioSource.clip)
			{
				StartCoroutine(CalculateVolume());
			}

			_played = true;
		}
		else if (transform.eulerAngles.x > 359.9 || transform.eulerAngles.x < 350)
		{
			if (_fadeCoro == null && AudioSource.volume > 0)
			{
				_fadeCoro = FadeVolume();
				StartCoroutine(_fadeCoro);
			}

			_played = false;
		}

		// Debug
		if (TestPlay)
		{
			Play();
			TestPlay = false;
		}
	}

	public void Play(float velocity = 10, float length = 1, float speed = 1)
	{
		if (_playCoro != null)
		{
			StopCoroutine(_playCoro);
			_playCoro = null;
		}

		_playCoro = KeyPress(velocity, length, speed);
		StartCoroutine(_playCoro);
	}

	IEnumerator CalculateVolume()
	{
		if (_fadeCoro != null)
		{
			StopCoroutine(_fadeCoro);
			_fadeCoro = null;
		}

		float startAngle = transform.eulerAngles.x;

		yield return new WaitForFixedUpdate();

		if (Mathf.Abs(startAngle - transform.eulerAngles.x) > 0)
		{
			AudioSource.volume = Mathf.Lerp(0, 1, Mathf.Clamp((Mathf.Abs(startAngle - transform.eulerAngles.x) / 2f), 0, 1));
		}
		AudioSource.Play();
	}

	IEnumerator KeyPress(float velocity, float length, float speed = 1)
	{
		float count = 0;
		_springJoint.useSpring = false;
		_constantForce.enabled = false;
		
		while (count < 0.75f)
		{
			if (transform.eulerAngles.x < 1 || transform.eulerAngles.x > 359.5f)
				_rigidbody.AddTorque(-Vector3.right * velocity / 1024f);
			
			count += Time.deltaTime / length * speed;
			yield return null;
		}

		_constantForce.enabled = true;
		_springJoint.useSpring = true;
		_playCoro = null;
	}

	void Constrain()
	{
		transform.position = _position;
		transform.rotation = Quaternion.Euler(transform.eulerAngles.x, _rotation.y, _rotation.z);

		if (transform.eulerAngles.x > 0 && transform.eulerAngles.x < 90)
		{
			transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, transform.eulerAngles.z);
		}
		if (transform.eulerAngles.x > 90 && transform.eulerAngles.x < 351)
		{
			transform.rotation = Quaternion.Euler(352, transform.eulerAngles.y, transform.eulerAngles.z);
		}
	}

	IEnumerator FadeVolume()
	{
		float count = 0;
		float volume = AudioSource.volume;

		while (count < 1)
		{
			AudioSource.volume = Mathf.Lerp(volume, 0, count);
			count += Time.deltaTime;
			yield return null;
		}
		AudioSource.volume = 0;
		_fadeCoro = null;
	}
}
