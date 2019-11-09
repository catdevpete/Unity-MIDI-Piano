using UnityEngine;
using UnityEngine.Events;

public class MidiPlayer : MonoBehaviour
{
	public PianoKeyDetector PianoKeyDetector;
	public float Speed = 1;
	public RepeatType RepeatType;

	public UnityEngine.Object[] MIDIFiles;

	[Header("Ensure Playlist is filled for builds")]
	public string[] Playlist;

	public MidiNote[] MidiNotes { get; set; }
	public UnityEvent OnPlayTrack { get; set; }

	MidiFileInspector _midi;

	string _path;
	string[] _keyIndex;
	int _noteIndex = 0;
	int _midiIndex;
	public float _timer = 0;

	void Start ()
	{
		OnPlayTrack = new UnityEvent();
		OnPlayTrack.AddListener(delegate{FindObjectOfType<MusicText>().StartSequence();});
		
		_midiIndex = 0;
		PlayCurrentMIDI();
	}

	void Update ()
	{
		if (Playlist.Length <= 0)
			enabled = false;
		
		if (_midi != null && MidiNotes.Length > 0 && _noteIndex < MidiNotes.Length)
		{
			_timer += Time.deltaTime * Speed * (float)MidiNotes[_noteIndex].Tempo;

			if (MidiNotes[_noteIndex].StartTime < _timer)
			{
				if (PianoKeyDetector.PianoNotes.ContainsKey(MidiNotes[_noteIndex].Note))
					PianoKeyDetector.PianoNotes[MidiNotes[_noteIndex].Note].Play(MidiNotes[_noteIndex].Velocity, 
																				MidiNotes[_noteIndex].Length, 
																				PianoKeyDetector.MidiPlayer.Speed);

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
		if (_midiIndex >= Playlist.Length - 1)
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
		_path = string.Format("{0}/MIDI/{1}.mid", Application.streamingAssetsPath, MIDIFiles[_midiIndex].name);
#else
		_path = string.Format("{0}/MIDI/{1}.mid", Application.streamingAssetsPath, Playlist[_midiIndex]);
#endif
		_midi = new MidiFileInspector(_path);
		MidiNotes = _midi.GetNotes();
		_noteIndex = 0;

		OnPlayTrack.Invoke();
	}


	[ContextMenu("MIDI array to playlist")]
	public void MIDIToPlaylist()
	{
		Playlist = new string[MIDIFiles.Length];
		
		for (int i = 0; i < MIDIFiles.Length; i++)
		{
			Playlist[i] = MIDIFiles[i].name;
		}
	}
}

public enum RepeatType { NoRepeat, RepeatLoop, RepeatOne }