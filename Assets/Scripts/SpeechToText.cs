using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class SpeechToText : MonoBehaviour
{
    private AudioClip clip;
    private byte[] bytes;
    private bool recording;
    private Action<string> onResultCallback;

    private const string DeepgramDirectUrl = "https://api.deepgram.com/v1/listen?model=nova-3&smart_format=true";

    private void Start()
    {
        Debug.Log("SpeechToText Start called");
    }

    private void Update()
    {
        if (recording && Microphone.GetPosition(null) >= clip.samples)
        {
            StopRecordingInternal();
        }
    }

    public void StartRecording(Action<string> callback)
    {
        onResultCallback = callback;
        StartRecording();
    }

    private void StartRecording()
    {
        Debug.Log("Starting Record");
        clip = Microphone.Start(null, false, 10, 44100);
        recording = true;
    }

    public void StopRecording()
    {
        if (recording)
        {
            StopRecordingInternal();
        }
    }

    private void StopRecordingInternal()
    {
        var position = Microphone.GetPosition(null);
        Microphone.End(null);
        var samples = new float[position * clip.channels];
        clip.GetData(samples, 0);
        bytes = EncodeAsWAV(samples, clip.frequency, clip.channels);
        recording = false;
        StartCoroutine(SendToDeepgramLocalUpload());
    }

    private IEnumerator SendToDeepgramLocalUpload()
    {
        string apiKey = Environment.DEEPGRAM_API_KEY;

        UnityWebRequest request = new UnityWebRequest(DeepgramDirectUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bytes);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Authorization", "Token " + apiKey);
        request.SetRequestHeader("Content-Type", "audio/wav");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Deepgram transcription failed: " + request.error);
            onResultCallback?.Invoke("");
            yield break;
        }

        string response = request.downloadHandler.text;
        string transcript = ExtractTranscript(response);
        onResultCallback?.Invoke(transcript);
    }

    private string ExtractTranscript(string json)
    {
        if (json.Contains("\"transcript\""))
        {
            int index = json.IndexOf("\"transcript\"");
            int start = json.IndexOf(":", index) + 2;
            int end = json.IndexOf("\"", start);
            return json.Substring(start, end - start);
        }
        return "";
    }

    private byte[] EncodeAsWAV(float[] samples, int frequency, int channels)
    {
        using (var memoryStream = new MemoryStream(44 + samples.Length * 2))
        {
            using (var writer = new BinaryWriter(memoryStream))
            {
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + samples.Length * 2);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2);
                writer.Write((ushort)(channels * 2));
                writer.Write((ushort)16);
                writer.Write("data".ToCharArray());
                writer.Write(samples.Length * 2);
                foreach (var sample in samples)
                {
                    writer.Write((short)(sample * short.MaxValue));
                }
            }
            return memoryStream.ToArray();
        }
    }
}
