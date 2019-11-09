using System;
using UnityEngine;
using UnityEngine.Events;

public class MidiPlayer : MonoBehaviour
{
	[Header("References")]
	public PianoKeyController PianoKeyDetector;

	[Header("Properties")]
	public float GlobalSpeed = 1;
	public RepeatType RepeatType;

	public KeyMode KeyMode;

	[Header("Ensure Song Name is filled for builds")]
	public MidiSong[] MIDISongs;

	public MidiNote[] MidiNotes { get; set; }
	public UnityEvent OnPlayTrack { get; set; }

	MidiFileInspector _midi;

	string _path;
	string[] _keyIndex;
	int _noteIndex = 0;
	int _midiIndex;
	float _timer = 0;



	void Start ()
	{
		OnPlayTrack = new UnityEvent();
		OnPlayTrack.AddListener(delegate{FindObjectOfType<MusicText>().StartSequence(MIDISongs[_midiIndex].Details);});
		
		_midiIndex = 0;
		PlayCurrentMIDI();
	}

	void Update ()
	{
		if (MIDISongs.Length <= 0)
			enabled = false;
		
		if (_midi != null && MidiNotes.Length > 0 && _noteIndex < MidiNotes.Length)
		{
			_timer += Time.deltaTime * GlobalSpeed * (float)MidiNotes[_noteIndex].Tempo;

			if (MidiNotes[_noteIndex].StartTime < _timer)
			{
				if (PianoKeyDetector.PianoNotes.ContainsKey(MidiNotes[_noteIndex].Note))
					PianoKeyDetector.PianoNotes[MidiNotes[_noteIndex].Note].Play(MidiNotes[_noteIndex].Velocity, 
																				MidiNotes[_noteIndex].Length, 
																				PianoKeyDetector.MidiPlayer.GlobalSpeed * MIDISongs[_midiIndex].Speed);

				_noteIndex++;
			}
		}
		else
		{
			SetupNextMIDI();
		}
	}

	void SetupNextMIDI()
	{
		if (_midiIndex >= MIDISongs.Length - 1)
		{
			if (RepeatType != RepeatType.NoRepeat)
				_midiIndex = 0;
			else
			{
				_midi = null;
				return;
			}
		}
		else
		{
			if (RepeatType != RepeatType.RepeatOne)
				_midiIndex++;
		}

		PlayCurrentMIDI();
	}

	void PlayCurrentMIDI()
	{
		_timer = 0;

#if UNITY_EDITOR
		_path = string.Format("{0}/MIDI/{1}.mid", Application.streamingAssetsPath, MIDISongs[_midiIndex].MIDIFile.name);
#else
		_path = string.Format("{0}/MIDI/{1}.mid", Application.streamingAssetsPath, MIDISongs[_midiIndex].SongFileName);
#endif
		_midi = new MidiFileInspector(_path);
		MidiNotes = _midi.GetNotes();
		_noteIndex = 0;

		OnPlayTrack.Invoke();
	}

#if UNITY_EDITOR
	[ContextMenu("MIDI to name")]
	public void MIDIToPlaylist()
	{
		for (int i = 0; i < MIDISongs.Length; i++)
		{
			MIDISongs[i].SongFileName = MIDISongs[i].MIDIFile.name;
		}
	}
#endif
}

public enum RepeatType { NoRepeat, RepeatLoop, RepeatOne }
public enum KeyMode { Physical, ForShow }

[Serializable]
public class MidiSong
{
#if UNITY_EDITOR
	public UnityEngine.Object MIDIFile;
#endif
	public string SongFileName;
	public float Speed = 1;
	[TextArea]
	public string Details;
}