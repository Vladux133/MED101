//========= Copyright 2018, HTC Corporation. All rights reserved. ===========
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ViveSR.anipal.Eye
{
    public class SRanipal_EyeFocusForExperiment : MonoBehaviour
    {
        private FocusInfo FocusInfo;
        private readonly float MaxDistance = 100;
        private readonly GazeIndex[] GazePriority = new GazeIndex[] { GazeIndex.COMBINE, GazeIndex.LEFT, GazeIndex.RIGHT };
        private static EyeData eyeData = new EyeData();
        private bool eye_callback_registered = false;
        private int _prevFrame;
        private string _currentObject;
        private float _lookedInSec;
        private string _focusFileName;
        private string _dataFileName;
        [Range(0f,5f),Tooltip("Number of seconds needed to indentify that player payed attention to the object")]
        public float FocusNumberOfSeconds;
        private float _gameTime;
        private void Start()
        {
            if (!SRanipal_Eye_Framework.Instance.EnableEye)
            {
                enabled = false;
                return;
            }
            _currentObject = "None";
            _lookedInSec = 0f;
            _prevFrame = 0;
            _dataFileName = "EyeTrackingData" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + ".txt";
            _focusFileName = "FocusData" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + ".txt";
            PcCreateFile(_dataFileName);
            PcCreateFile(_focusFileName);
        }

        private void Update()
        {
            _gameTime += Time.deltaTime;

            if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
                SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT) return;

            if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
            {
                SRanipal_Eye.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
                eye_callback_registered = true;
            }
            else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
            {
                SRanipal_Eye.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
                eye_callback_registered = false;
            }

            foreach (GazeIndex index in GazePriority)
            {
                Ray GazeRay;
                int layerMask = LayerMask.NameToLayer("Default");
                bool eye_focus;
                if (eye_callback_registered)
                {
                    eye_focus = SRanipal_Eye.Focus(index, out GazeRay, out FocusInfo, 0, MaxDistance, (1 << layerMask), eyeData);
                }
                else
                {
                    eye_focus = SRanipal_Eye.Focus(index, out GazeRay, out FocusInfo, 0, MaxDistance, (1 << layerMask));
                }
                if (eye_focus)
                {
                    if (_prevFrame != eyeData.frame_sequence)
                    {
                        if (FocusInfo.transform.name.ToString() != _currentObject)
                        {
                            if (_lookedInSec >= FocusNumberOfSeconds)
                            {
                                WriteToPC(_gameTime + "," + _currentObject + "," + _lookedInSec, _focusFileName);
                            }
                                _currentObject = FocusInfo.transform.name;
                                _lookedInSec = 0f;         
                        }
                        else
                        {
                            _lookedInSec += Time.deltaTime;
                        }
                        ////writes data to file
                        WriteToPC(_gameTime + "," + FocusInfo.point.ToString() + "," + FocusInfo.normal.ToString() + "," +
                            FocusInfo.transform.name.ToString() + "," + eyeData.verbose_data.combined.eye_data.gaze_direction_normalized.ToString() + "," +
                            eyeData.verbose_data.combined.eye_data.gaze_origin_mm.ToString() + "," + eyeData.verbose_data.left.pupil_diameter_mm.ToString() + "," + eyeData.verbose_data.right.pupil_diameter_mm.ToString(), _dataFileName);
                        _prevFrame = eyeData.frame_sequence;
                        Debug.Log(_currentObject + _lookedInSec);
                    }
                }
            }
        }
        private void Release()
        {
            if (eye_callback_registered == true)
            {
                SRanipal_Eye.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
                eye_callback_registered = false;
            }
        }
        private static void EyeCallback(ref EyeData eye_data)
        {
            eyeData = eye_data;
        }
        private void PcCreateFile(string fileName)

        {

            if (!Directory.Exists(fileName))

            {
                if (fileName.Substring(0, 1) == "E")
                {

                    using (StreamWriter sw = File.CreateText(fileName))

                    {

                        sw.WriteLine("Eye-Tracking Data:DateTime.Now, FocusInfo.point,FocusInfo.normal,FocusInfo.transform.name,eyeData.verbose_data.combined.eye_data.gaze_direction_normalized,eyeData.verbose_data.combined.eye_data.gaze_origin_mm," +
                            "eyeData.verbose_data.left.pupil_diameter_mm,eyeData.verbose_data.right.pupil_diameter_mm");

                    }

                }
                else
                {
                    using (StreamWriter sw = File.CreateText(fileName))

                    {

                        sw.WriteLine("Object focus data: DateTime.Now, Object Name, Time looked at object");

                    }
                }

            }
        }
        private void WriteToPC(string message, string fileName)

        {

            using (StreamWriter sw = File.AppendText(fileName))

            {

                sw.WriteLine(message);

            }

        }
    }
}