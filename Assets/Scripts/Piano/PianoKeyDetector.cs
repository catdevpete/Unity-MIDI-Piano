using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class PianoKeyDetector : MonoBehaviour
{
	public MidiPlayer MidiPlayer;
	public Transform PianoKeysParent;
	public AudioClip[] Notes;

	public string StartKey;
	public int StartOctave;
	public bool Sort = true;
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
			if (PianoKeysParent.GetChild(i).GetComponent<AudioSource>())
			{
				PianoKeysParent.GetChild(i).GetComponent<AudioSource>().clip = Notes[count];
				PianoNotes.Add(KeyString(count + Array.IndexOf(_keyIndex, StartKey)), PianoKeysParent.GetChild(i).GetComponent<PianoKey>());
				count++;
			}
		}
	}

	string KeyString (int count)
	{
		return _keyIndex[count % 12] + (Mathf.Floor(count / 12) + StartOctave);
	}
}