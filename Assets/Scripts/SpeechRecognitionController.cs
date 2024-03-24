using LLMUnity;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using System.IO;
using System;
using System.Text;

public class SpeechRecognitionController : MonoBehaviour {
    [SerializeField] private UnityEvent onStartRecording;
    [SerializeField] private UnityEvent onSendRecording;
    [SerializeField] public UnityEvent<string> onResponse;
    [SerializeField] private TMP_Dropdown m_deviceDropdown;
    [SerializeField] private Image m_progress;

    public RunWhisper runWhisper; // This is the reference to the RunWhisper script
    public LLM llm;

    private string m_deviceName;
    private AudioClip m_clip;
    private byte[] m_bytes;
    private bool m_recording;

    private string transcribedText;
    public string final_reply = "";
    public RunJets runjets;

    private void Awake() {
        // Select the microphone device (by default the first one) but
        // also populate the dropdown with all available devices
        onResponse.AddListener(HandleTranscribedText);

        m_deviceName = Microphone.devices[0];
        foreach (var device in Microphone.devices) {
            m_deviceDropdown.options.Add(new TMP_Dropdown.OptionData(device));
        }
        m_deviceDropdown.value = 0;
        m_deviceDropdown.onValueChanged.AddListener(OnDeviceChanged);
    }

    public string RemoveTextWithinParentheses(string input)
    {
        bool inParentheses = false;
        StringBuilder output = new StringBuilder();

        foreach (char c in input)
        {
            if (c == '(')
            {
                inParentheses = true;
                continue;
            }
            if (c == ')')
            {
                inParentheses = false;
                continue;
            }
            if (!inParentheses)
            {
                output.Append(c);
            }
        }

        return output.ToString();
    }


    void HandleReply(string reply){
        // do something with the reply from the model
        Debug.Log(reply);
        final_reply = reply;
    }

    void ReplyCompleted(){
        Debug.Log("FINISHED AND COMPLETED");
        /// handle call to wav generation here
        final_reply = RemoveTextWithinParentheses(final_reply);
        runjets.inputText = final_reply;
        
        runjets.TextToSpeech();
    }

    private void HandleTranscribedText(string text) {
        transcribedText = text; // Store the transcribed text in the variable
        string newtranscribedText = "";
        if (transcribedText.Length > 8) {
            newtranscribedText = transcribedText.Substring(0, transcribedText.Length - 8);
        } 
        Debug.Log(newtranscribedText + "as;dlfkjas;ldkfj");
        _ = llm.Chat(newtranscribedText, HandleReply, ReplyCompleted);
    }   

    /// <summary>
    /// This method is called when the user selects a different device from the dropdown
    /// </summary>
    /// <param name="index"></param>
    private void OnDeviceChanged(int index) {
        m_deviceName = Microphone.devices[index];
    }

    /// <summary>
    /// This method is called when the user clicks the button
    /// </summary>
    public void Click() {
        if (!m_recording) {
            StartRecording();
        } else {
            StopRecording();
        }
    }

    /// <summary>
    /// Start recording the user's voice
    /// </summary>
    private void StartRecording() {
        m_clip = Microphone.Start(m_deviceName, false, 5, 16000);
        m_recording = true;
        onStartRecording.Invoke();
    }

    /// <summary>
    /// Stop recording the user's voice and send the audio to the Whisper Model
    /// </summary>
    private void StopRecording() {
        var position = Microphone.GetPosition(m_deviceName);
        Microphone.End(m_deviceName);
        m_recording = false;
        SendRecording();
    }

    /// <summary>
    /// Run the Whisper Model with the audio clip to transcribe the user's voice
    /// </summary>
    private void SendRecording() {
        onSendRecording.Invoke();
        runWhisper.audioClip = m_clip;
        runWhisper.Transcribe();
    }

    private void Update() {
        if (!m_recording) {
            return;
        }

        m_progress.fillAmount = (float)Microphone.GetPosition(m_deviceName) / m_clip.samples;

        if (Microphone.GetPosition(m_deviceName) >= m_clip.samples) {
            StopRecording();
        }
    }
}
