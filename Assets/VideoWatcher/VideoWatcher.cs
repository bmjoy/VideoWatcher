﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Xml.Schema;

public class VideoWatcher : MonoBehaviour
{
    private string videoFileFolderPath;
    public string videoFileFolderPathMac = "////Users/tedbarnett/Dropbox (barnettlabs)/Videos/Ted Videos/";
    public string videoFileFolderPathWindows;
    public GameObject startupPanel; // hide after loading
    public GameObject videoPanels; // show after loading
    public List<GameObject> VideoPanel; // a list of n videoPanels (4, 6, or whatever can be displayed)
    public List<string> ValidVideoExtensions;
    public Text AutoLaunchCountdownText;
    public string blankVideoFileName;

    private int currentVideo = 0;
    public float showFilenameTimeSecs = 3;
    private int maxPanels = 6;
    private bool preparingSetup = false; // ensures nothing happens in Update loop
    private float launchedTime;
    public float secondsToAutoStart = 10.0f;
    public float skipVideosShorterThanSecs = 0.0f;
    public float longVideoLengthMinimum = 30.0f; // if video is longer than this minimum, start at a random frame
    public List<string> VideoFileNames;
    private bool firstLoop = true;
    private int firstLoopCounter = 0;

    // ****************************************** START ****************************************************************
    void Start()
    {
        launchedTime = Time.time;
        Debug.Log("START: launchedTime = " + launchedTime);
        startupPanel.SetActive(true);
        videoPanels.SetActive(true);

        videoFileFolderPath = Application.streamingAssetsPath + "/";
        videoFileFolderPath = videoFileFolderPathMac; // default assumption is Mac platform
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer) videoFileFolderPath = videoFileFolderPathWindows;
        maxPanels = VideoPanel.Count; //If on smaller screen, set maxPanels to a smaller number than VideoPanel.Count

        for (int i = 0; i < maxPanels; i++) // set up videoPlayer for each panel
        {
            var vp = VideoPanel[i].GetComponentInChildren<UnityEngine.Video.VideoPlayer>();
            vp.Stop(); // TODO: Stop vs. Play?
            vp.loopPointReached += PlayNextVideo; // when videos end, automatically play another

            //var currentButton = VideoPanel[i].GetComponentInChildren<Button>(); // finds the first button child and sets currentButton to that
            //currentButton.onClick.AddListener(() => { ClickedOnVideoPanel(vp); });
            vp.SetDirectAudioVolume(0, 0.0f); // Mute volume
            vp.url = videoFileFolderPath + blankVideoFileName; // set to blank video
        }
        SetupVideoList();
        preparingSetup = true; // starts wait cycle countdown (and covers over setup process)
    }

    // ---------------------------------------------------- SetupVideoList ----------------------------------------------------
    void SetupVideoList()
    {
        VideoFileNames = new List<string>();
        DirectoryInfo dir = new DirectoryInfo(videoFileFolderPath);
        FileInfo[] info = dir.GetFiles("*.*"); // TODO: Read from Dropbox or Google Drive instead?

        foreach (FileInfo f in info)
        {
            string fileNameString = Path.GetFileName(f.ToString());
            string fileExtension = Path.GetExtension(fileNameString).ToLower(); // lowercase extension
            if ((ValidVideoExtensions.Contains(fileExtension)) && (fileNameString != blankVideoFileName)) VideoFileNames.Add(fileNameString);
        }
        Debug.Log("Total # videos = " + VideoFileNames.Count);
        Debug.Log("Finished SetupVideoList: Time.time = " + Time.time);

        if (VideoFileNames.Count == 0) Debug.Log("No files found in Directory");
    }


    // ****************************************************** UPDATE ****************************************************************
    void Update()
    {
        if (preparingSetup) {
            if ((Time.time - launchedTime) > secondsToAutoStart) // if time runs out, BeginPlaying()...
            {
                preparingSetup = false;
                BeginPlaying();
            }
            else // otherwise continue countdown...
            {
                string timeLeft = Mathf.FloorToInt(secondsToAutoStart + launchedTime - Time.time).ToString();
                AutoLaunchCountdownText.text = timeLeft;
            }
        }
    }

    // ---------------------------------------------------- BeginPlaying ----------------------------------------------------
    public void BeginPlaying()
    {
        startupPanel.SetActive(false);
        videoPanels.SetActive(true);

        for (int i = 0; i < maxPanels; i++) // start playing each video panel
        {
            var vp = VideoPanel[i].GetComponentInChildren<UnityEngine.Video.VideoPlayer>();
            vp.Play(); // start playing each panel TODO: Not needed since playOnWake already?
        }
    }


    // ---------------------------------------------------- PlayNextVideo ----------------------------------------------------
    public void PlayNextVideo(UnityEngine.Video.VideoPlayer vp)
    {
        currentVideo = Random.Range(0, VideoFileNames.Count - 1); // Choose next video at random
        if(firstLoop) vp.url = videoFileFolderPath + blankVideoFileName; // use blank video for first set of panels
            else vp.url = videoFileFolderPath + VideoFileNames[currentVideo];
        vp.prepareCompleted += SetVideoCaption;
        vp.Prepare();
    }
    // ---------------------------------------------------- SetVideoCaption ----------------------------------------------------
    void SetVideoCaption(UnityEngine.Video.VideoPlayer vp) // once video URL is loaded, set the currentFileNameText.text with the file name and length
    {
        TextMeshProUGUI currentFileNameText = vp.gameObject.GetComponentInChildren<TextMeshProUGUI>();
        if (firstLoop)
        {
            currentFileNameText.text = "";
            //vp.Play(); // TODO: need this running or will BeginPlaying take care of this?
            firstLoopCounter++;
            if (firstLoopCounter >= maxPanels) firstLoop = false;
        }
        else
        {
            float videoLength = (float)vp.length;
            if (videoLength > longVideoLengthMinimum) vp.frame = Mathf.FloorToInt(vp.frameCount * Random.Range(0.0f, 1.0f));

            int min = Mathf.FloorToInt(videoLength / 60);
            int sec = Mathf.FloorToInt(videoLength % 60);
            string videoLengthString;
            if (min > 0) videoLengthString = min.ToString("00") + ":" + sec.ToString("00");
                else videoLengthString = sec.ToString("00") + " secs";
            string vpFileName = vp.url;
            //Debug.Log("videoFileFolderPath = " + videoFileFolderPath); // TODO: Why is this sometimes NULL (after hovering over button)?
            vpFileName = vpFileName.Replace(videoFileFolderPath, "");
            currentFileNameText.text = makeNameString(vpFileName) + "\n<alpha=#88><size=70%>(" + videoLengthString + ")</size>";
            // TODO: don't need this? vp.Play();
        }
    }

    // ---------------------------------------------------- makeNameString ----------------------------------------------------

    public string makeNameString(string fileName)
    {
        string newFileName = fileName;
        string dateString = "date unknown";
        int fileExtPos = newFileName.LastIndexOf(".");
        if (fileExtPos >= 0)
            newFileName = newFileName.Substring(0, fileExtPos); // strip off the file extension
        if (newFileName.Length > 10)
        {
            dateString = newFileName.Substring(0, 10);

            string dateText = "";
            int dateNum;
            int.TryParse(dateString.Substring(8, 2), out dateNum);

            string[] monthNames = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            int monthNum;
            int.TryParse(dateString.Substring(5, 2), out monthNum);
            string monthText;
            if (monthNum != 0)
            {
                if (dateNum == 0) // if month is set but date is 00 (i.e. we knew the month but not the day)
                {
                    monthText = monthNames[monthNum - 1].ToString() + " ";
                    dateText = "";
                }
                else // if month and date are valid
                {
                    monthText = monthNames[monthNum - 1].ToString() + " ";
                    dateText = dateNum.ToString() + " ";
                }
            }
            else
            {
                monthText = "";
                dateText = "";
            };

            //Debug.Log("monthNum = " + monthNum + ", dateNum = " + dateNum);
            dateString = monthText + dateText + dateString.Substring(0, 4);
        }
        newFileName = dateString; // Set name of file to date of movie clip (extracted from front of filename)
        return newFileName;
    }

    // ---------------------------------------------------- ButtonEnter ----------------------------------------------------
    public void ButtonEnter(UnityEngine.Video.VideoPlayer vp)
    {
        //vp.Pause();
        //ShowVideoCaption(vp, true);
    }
    // ---------------------------------------------------- ButtonExit ----------------------------------------------------
    public void ButtonExit(UnityEngine.Video.VideoPlayer vp)
    {
        //vp.Play();
        //ShowVideoCaption(vp, false);
    }

    // ---------------------------------------------------- ShowVideoCaption ----------------------------------------------------
    public void ShowVideoCaption(UnityEngine.Video.VideoPlayer vp, bool showNameNow)
    {
        TextMeshProUGUI currentFileNameText = vp.gameObject.GetComponentInChildren<TextMeshProUGUI>();
        if (showNameNow)
        {
            Debug.Log("Showing caption: video player is " + vp);

            SetVideoCaption(vp);
            currentFileNameText.color = new Color32(255, 255, 0, 255); // set text to yellow color to make it visible
        }
        else
        {
            Debug.Log("Hiding caption: video player is " + vp);
            currentFileNameText.color = new Color32(255, 255, 0, 0); // set text to transparent to hide
        }
    }

    // ---------------------------------------------------- ToggleVolume ----------------------------------------------------
    public void ToggleVolume(UnityEngine.Video.VideoPlayer vp)
    {
        // toggle mute for this video panel

        float tempVolume = vp.GetDirectAudioVolume(0);
        if (tempVolume > 0.0f)
        {
            vp.SetDirectAudioVolume(0, 0.0f);
        }
        else
        {
            vp.SetDirectAudioVolume(0, 1.0f);
        }
    }

    // ---------------------------------------------------- JumpToFrame ----------------------------------------------------

    public void JumpToFrame(UnityEngine.Video.VideoPlayer vp, float percentOfClip)
    {
        var newFrame = vp.frameCount * percentOfClip;
        vp.frame = (long)newFrame;
    }

    // ---------------------------------------------------- ClickedOnVideoPanel ----------------------------------------------------
    public void ClickedOnVideoPanel(UnityEngine.Video.VideoPlayer vp)
    {
        //ToggleVolume(vp);
        //vp.Play();

    }

    // ---------------------------------------------------- SkipToNextVideo ----------------------------------------------------
    public void SkipToNextVideo(UnityEngine.Video.VideoPlayer vp)
    {
        vp.Stop();
        PlayNextVideo(vp);

    }


    /* TODO List
     * Try maximizing windows, etc.
     * Do JumpToFrame for longer videos (i.e. don't start on frame 0)
     * Try instantiating video panels when needed
     * Create and release RenderTextures
     */
}
