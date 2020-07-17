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

        private string _dataFileName;
        private void Start()
        {
            if (!SRanipal_Eye_Framework.Instance.EnableEye)
            {
                enabled = false;
                return;
            }
            _dataFileName = "EyeTrackingData" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + ".txt";
            PcCreateFile();
        }

        private void Update()
        {
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
                if (eye_focus && FocusInfo.point != null && FocusInfo.point.x !=0)
                {
                    ////writes data to file
                    WriteToPC(DateTime.Now +","+ FocusInfo.point.ToString()+ ","+ FocusInfo.normal.ToString()+","+ FocusInfo.distance.ToString()+","+
                        FocusInfo.transform.name.ToString()+","+ eyeData.verbose_data.combined.eye_data.gaze_direction_normalized.ToString()+","+
                        eyeData.verbose_data.combined.eye_data.gaze_origin_mm.ToString()+","+eyeData.verbose_data.left.pupil_diameter_mm.ToString()+","+eyeData.verbose_data.right.pupil_diameter_mm.ToString());
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
        private void PcCreateFile()

        {

            if (!Directory.Exists(_dataFileName))

            {

                using (StreamWriter sw = File.CreateText(_dataFileName))

                {

                    sw.WriteLine("Eye-Tracking Data:DateTime.Now, FocusInfo.point,FocusInfo.normal,FocusInfo.distance,FocusInfo.transform.name,eyeData.verbose_data.combined.eye_data.gaze_direction_normalized,eyeData.verbose_data.combined.eye_data.gaze_origin_mm," +
                        "eyeData.verbose_data.left.pupil_diameter_mm,eyeData.verbose_data.right.pupil_diameter_mm");

                }

            }

        }
        private void WriteToPC(string message)

        {

            using (StreamWriter sw = File.AppendText(_dataFileName))

            {

                sw.WriteLine(message);

            }

        }
    }
}