using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Standards;
using System.Threading;
using System.Linq;



public class NotesController : MonoBehaviour
{
    //MIDI Eigenschaften
    public int m_BPM;
    public float m_Position;
    public float m_TimePlayed;

    public string m_Path;

    public DevicesConnector m_DeviceConnector;
    public InputDevice[] m_InputDevices;
    public OutputDevice[] m_OutputDevices;
    public static Playback m_playback;



    //Noten Generierung
    public GameObject m_Prefab_Notes;
    public GameObject m_Prefab_Grid;
    private int[] m_Notes;

    [SerializeField] private int m_Row;
    [SerializeField] private int m_Column;
    [SerializeField] private float m_XStartPos;
    [SerializeField] private float m_YStartPos;
    [SerializeField] private float m_XSpace;
    [SerializeField] private float m_YSpace;




    // Start is called before the first frame update
    void Start()
    {
        var m_File = ReadFile(m_Path);
        var m_Duration = GetDuration(m_File);
        var m_NoteObject = GameObject.Find("ObjectNotes");
        var m_GridObject = GameObject.Find("GRID_Square");

        DisplayNotes(m_File, m_Duration, m_NoteObject, m_GridObject);
        m_InputDevices = GetInputDevices();
        m_OutputDevices = GetOutputDevices();
        setDevices(m_InputDevices, m_OutputDevices);
        //PlayMidi(m_File, m_OutputDevices,m_Duration);
        WriteNotes(m_InputDevices, m_OutputDevices);
        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private MidiFile ReadFile(string Path)
    {
        var File = MidiFile.Read(Path, new ReadingSettings
        {
            InvalidChannelEventParameterValuePolicy = InvalidChannelEventParameterValuePolicy.ReadValid,
            InvalidChunkSizePolicy = InvalidChunkSizePolicy.Ignore,
            InvalidMetaEventParameterValuePolicy = InvalidMetaEventParameterValuePolicy.SnapToLimits,
            MissedEndOfTrackPolicy = MissedEndOfTrackPolicy.Ignore,
            NoHeaderChunkPolicy = NoHeaderChunkPolicy.Ignore,
            NotEnoughBytesPolicy = NotEnoughBytesPolicy.Ignore,
            UnexpectedTrackChunksCountPolicy = UnexpectedTrackChunksCountPolicy.Ignore,
            UnknownChannelEventPolicy = UnknownChannelEventPolicy.SkipStatusByteAndOneDataByte,
            UnknownChunkIdPolicy = UnknownChunkIdPolicy.ReadAsUnknownChunk,
            UnknownFileFormatPolicy = UnknownFileFormatPolicy.Ignore,
        });
        return File;
    }

    private void PlayMidi(MidiFile File, OutputDevice[] OutPut, TimeSpan Duration)
    {     
        m_playback = File.GetPlayback();
        m_playback.Start();
        Debug.Log("Playback started");
        m_playback.NotesPlaybackStarted += OnNotesPlaybackStarted;
        PlaybackCurrentTimeWatcher.Instance.AddPlayback(m_playback, TimeSpanType.Midi);
        PlaybackCurrentTimeWatcher.Instance.CurrentTimeChanged += OnCurrentTimeChanged;
        PlaybackCurrentTimeWatcher.Instance.Start();

        SpinWait.SpinUntil(() => !m_playback.IsRunning);


        Debug.Log("Playback stopped or finished");
        OutPut[1].Dispose();
        m_playback.Dispose();
        PlaybackCurrentTimeWatcher.Instance.Dispose();
       
    }
    
    private static void OnNotesPlaybackStarted(object sender, NotesEventArgs e)
        {
            if (e.Notes.Any(n => n.Length == Melanchall.DryWetMidi.MusicTheory.Interval.Eight))
                m_playback.Stop();
        }

    private TimeSpan GetDuration(MidiFile File)
    {
        TimeSpan midiFileDuration = File.GetDuration<MetricTimeSpan>();
        //Debug.Log("Duration " + midiFileDuration);
        return midiFileDuration;
    }
    

    private void DisplayNotes(MidiFile File, TimeSpan Duration, GameObject PrefabNotes, GameObject PrefabGrid)
    {
        TempoMap tempo = File.GetTempoMap();
        Debug.Log(tempo);
        IEnumerable<Note> notes = File.GetNotes(); 
        Debug.Log("Notes laden " + notes);
        var NoteWidth = 5f;

        var notePos = new Vector3(1,1,1);

         foreach (var note in notes)
            {
                float noteTime = note.TimeAs<MetricTimeSpan>(tempo).TotalMicroseconds / 100000.0f;
                Debug.Log("NoteTime " + noteTime);
                int noteNumber = note.NoteNumber;
                Debug.Log("NoteNumber " + noteNumber);
                float noteLength = note.LengthAs<MetricTimeSpan>(tempo).TotalMicroseconds / 100000f * NoteWidth;
                Debug.Log("Note Length " + noteLength);
                float noteChannel = note.Channel;
                Debug.Log("Note Channel " + noteChannel);


                GameObject nObj = Instantiate(PrefabGrid, notePos, Quaternion.identity);
                Debug.Log("Instanz");
                nObj.GetComponent<GameNote>().InitGameNote(noteTime, noteNumber,noteLength,noteChannel);
                Debug.Log("foreach läuft");
                nObj.SetActive(true);
            }
        /* 
        foreach(var note in notes)
        {
            var notelength = note.Length;
            var notename = note.NoteName;
            var notepos = transform.position + new Vector3(0,0,0);
            
           
            for(int i = 0; i < m_Column * m_Row; i++)
            {
                var gridPos = new Vector3(m_XStartPos + (m_XSpace * (i % m_Column)), -m_YStartPos + (m_YSpace * (i / m_Column)));
                GameObject Grid = Instantiate(PrefabGrid, gridPos,Quaternion.identity);
                Grid.SetActive(false);
            }

            for(int i = 0; i < m_Column * m_Row; i++)
            {
                var notePos = new Vector3(m_XStartPos + (m_XSpace * (i % m_Column)), -m_YStartPos + (m_YSpace * (i / m_Column)));
                GameObject Notes = Instantiate(PrefabNotes, notePos, Quaternion.identity);
                Notes.SetActive(true);
            }

         




            
            GameObject NoteBox = Instantiate(PrefabNotes, notepos, Quaternion.identity);

            NoteBox.SetActive(true);
            Debug.Log(note);
        }
        */

          
    }

    private void WriteNotes(InputDevice[] InputPiano, OutputDevice[] Output)
    {
        InputPiano[0].EventReceived += OnEventReceived;
        InputPiano[0].StartEventsListening();
        Debug.Log("Input Piano working");
        
    }

     private void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
    {
        var midiDevice = (MidiDevice)sender;
        Debug.Log("Event received from " + midiDevice.Name + ": " + e.Event); 
    }
  

    private InputDevice[] GetInputDevices()
    {

        InputDevice[] inputList = InputDevice.GetAll().ToArray();
        for (int i = 0; i <= inputList.Length-1; i++)
        {
            Debug.Log("Input " + inputList[i] + " Nummer im Array " + i);
        }

        return inputList;
      
    }

    private OutputDevice[] GetOutputDevices()
    {

        OutputDevice[] outputList = OutputDevice.GetAll().ToArray();
        for (int i = 0; i <= outputList.Length-1; i++)
        {
            Debug.Log("Output " + outputList[i] + " Nummer im Array " + i);
        }

        return outputList;
      
    }

    private void setDevices(InputDevice[] Input, OutputDevice[] Output)
    {
        m_DeviceConnector = new DevicesConnector(Input[0], Output[0], Output[1]);
        m_DeviceConnector.Connect();
    }

    private static void OnCurrentTimeChanged(object sender, PlaybackCurrentTimeChangedEventArgs e)
    {
        foreach (var playbackTime in e.Times)
        {
            var playback = playbackTime.Playback;
            var time = (MidiTimeSpan)playbackTime.Time;

            Console.WriteLine($"Current time is {time}.");
        }
    }

 

    private void OnApplicationQuit() 
        {
/*           m_playback.Stop();
            m_playback.Dispose();
            PlaybackCurrentTimeWatcher.Instance.Dispose();
            m_OutputDevices[0].Dispose();
           m_OutputDevices[1].Dispose();
            m_InputDevices[0].Dispose();
            */
        }

}
