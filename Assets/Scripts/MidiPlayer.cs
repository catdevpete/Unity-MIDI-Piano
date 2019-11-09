using UnityEngine;

public class MidiPlayer : MonoBehaviour
{
	public PianoKeyDetector PianoKeyDetector;
	public float Speed = 1;
	public RepeatType RepeatType;

	public UnityEngine.Object[] MIDIFiles;
	public string[] Playlist;

	public MidiNote[] MidiNotes { get; set; }

	MidiFileInspector _midi;

	string _path;
	string[] _keyIndex;
	int _noteIndex = 0;
	int _midiIndex;
	private float _timer = 0;

	void Start ()
	{
		_midiIndex = 0;
		PlayMIDI();
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
				if (_timer - 50 < MidiNotes[_noteIndex].StartTime && PianoKeyDetector.PianoNotes.ContainsKey(MidiNotes[_noteIndex].Note))
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

		PlayMIDI();
	}

	void PlayMIDI()
	{
		_timer = 0;

		_path = string.Format("{0}/MIDI/{1}.mid", Application.streamingAssetsPath, Playlist[_midiIndex]);
		_midi = new MidiFileInspector(_path);
		MidiNotes = _midi.GetNotes();
		_noteIndex = 0;
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