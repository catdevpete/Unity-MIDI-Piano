using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class PianoKeyController : MonoBehaviour
{
	[Header("References")]
	public MidiPlayer MidiPlayer;
	public Transform PianoKeysParent;
	public Transform SustainPedal;
	public AudioClip[] Notes;

	[Header("Properties")]
	public string StartKey = "A";			// If the first key is not "A", change it to the appropriate note.
	public int StartOctave;					// Start Octave can be increased if the piano/keyboard is not full length. 
	public float PedalReleasedAngle;		// Local angle that a pedal is considered to be released, or off.
	public float PedalPressedAngle;			// Local angle that a pedal is considered to be pressed, or on.
	public float SustainSeconds = 5;		// May want to reduce this if there's too many AudioSources being generated per key.
	public float PressAngleThreshold = 355f;// Rate of keys being slowly released.
	public float PressAngleDecay = 1f;		// Rate of keys being slowly released.
	public bool Sort = true;				// Sorts the Notes. If regex is not empty, it will use that to do the sorting.
	public bool NoMultiAudioSource;			// Will prevent duplicates if true, if you need to optimise. Multiple Audio sources are necessary to remove crackling.
	

	[Header("Attributes")]
	public bool SustainPedalPressed = true;	// When enabled, keys will not stop playing immediately after release.
	public bool KeyPressAngleDecay = true;	// When enabled, keys will slowly be released.
	public bool RepeatedKeyTeleport = true;	// When enabled, during midi mode, a note played on a pressed key will force the rotation to reset.
	

	private float _sustainPedalLerp = 1;

	// Should be controlled via MidiPlayer
	public KeyMode KeyMode					
	{
		get
		{
			if (MidiPlayer)
				return MidiPlayer.KeyMode;
			else
				return KeyMode.Physical;
		}
	}

	public bool ShowMIDIChannelColours		
	{
		get
		{
			if (MidiPlayer)
				return MidiPlayer.ShowMIDIChannelColours;
			else
				return false;
		}
	}

	public Color[] MIDIChannelColours					
	{
		get
		{
			if (MidiPlayer)
				return MidiPlayer.MIDIChannelColours;
			else
				return null;
		}
	}

	[Header("Note: Leave regex blank to sort alphabetically")]
    public string Regex;

	public Dictionary<string, PianoKey> PianoNotes = new Dictionary<string, PianoKey>();

	private readonly string[] _keyIndex = new string[12] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

	void Awake ()
	{
		if (Sort)
		{
			Regex sortReg = new Regex(@Regex);
            Notes = Notes.OrderBy(note => sortReg.Match(note.name).Value).ToArray();
		}

		var count = 0;

		for (int i = 0; i < PianoKeysParent.childCount; i++)
		{
			AudioSource keyAudioSource = PianoKeysParent.GetChild(i).GetComponent<AudioSource>();
			
			if (keyAudioSource)
			{
				PianoKey pianoKey = PianoKeysParent.GetChild(i).GetComponent<PianoKey>();
				
				keyAudioSource.clip = Notes[count];
				PianoNotes.Add(KeyString(count + Array.IndexOf(_keyIndex, StartKey)), pianoKey);
				pianoKey.PianoKeyController = this;
				
				count++;
			}
		}
	}

	// https://stackoverflow.com/a/228060
	string Reverse(string s)
	{
		char[] charArray = s.ToCharArray();
		Array.Reverse( charArray );
		return new string( charArray );
	}

	void Update()
	{
		_sustainPedalLerp -= Time.deltaTime * (SustainPedalPressed ? 1 : -1) * 3.5f;
		_sustainPedalLerp = Mathf.Clamp01(_sustainPedalLerp);

		if (PedalPressedAngle > PedalReleasedAngle)
			SustainPedal.localRotation = Quaternion.Lerp(Quaternion.Euler(PedalReleasedAngle, 0, 0), Quaternion.Euler(PedalPressedAngle, 0, 0), _sustainPedalLerp);
		else
			SustainPedal.localRotation = Quaternion.Lerp(Quaternion.Euler(PedalPressedAngle, 0, 0), Quaternion.Euler(PedalReleasedAngle, 0, 0), _sustainPedalLerp);
	}

	string KeyString (int count)
	{
		return _keyIndex[count % 12] + (Mathf.Floor(count / 12) + StartOctave);
	}
}