﻿using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{

    public const float VISIBLE_TABLE_WIDTH = 23f;
    public const float VISIBLE_TABLE_HEIGHT = 14f;

    private float adjustedToAspectRatio;

    public static Camera Cam;
    public static CameraController Main;

    void Awake()
    {
        Main = this;
        Cam = GetComponent<Camera>();
    }

    // Use this for initialization
    void Start()
    {
        adjustToAspectRatio();
        // Set screen timeout to never-sleep
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    // Update is called once per frame
    void Update()
    {
        if(Mathf.Abs(Cam.aspect - adjustedToAspectRatio) > 0.01f)
        {
            adjustToAspectRatio();
        }
    }

    // Adjust the camera's orthographic "zoom" so that the entire table is visible in the current resolution
    private void adjustToAspectRatio()
    {
        Cam.orthographicSize = VISIBLE_TABLE_WIDTH / 2 / Cam.aspect;
        adjustedToAspectRatio = Cam.aspect;
    }

    // Stores a screenshot
    public void takeScreenshot()
    {
        string filepath = String.Format("{0}/screenshot_{1:yyyyMMddHHmmssfff}.png", Application.persistentDataPath, System.DateTime.Now);
        ScreenCapture.CaptureScreenshot(filepath);
        Debug.LogFormat("Screenshot saved to {0}.", filepath);
    }

    void OnApplicationQuit()
    {
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
    }
}
