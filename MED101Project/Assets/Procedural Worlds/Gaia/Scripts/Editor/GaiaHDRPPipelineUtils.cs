﻿using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.SceneManagement;
#if HDPipeline
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;
#endif
using System.Collections;
using ProcedualWorlds.WaterSystem;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif


namespace Gaia.Pipeline.HDRP
{
    /// <summary>
    /// HDRP prefs keys
    /// </summary>
    public static class HDRPKeys
    {
        internal const string sceneViewAntialiasing = "HDRP.SceneView.Antialiasing";
        internal const string sceneViewStopNaNs = "HDRP.SceneView.StopNaNs";
        internal const string matcapViewMixAlbedo = "HDRP.SceneView.MatcapMixAlbedo";
        internal const string matcapViewScale = "HDRP.SceneView.MatcapViewScale";
        internal const string lightColorNormalization = "HDRP.UI.LightColorNormalization";
        internal const string materialEmissionColorNormalization = "HDRP.UI.MaterialEmissionNormalization";
    }

    /// <summary>
    /// Static class that handles all the HDRP setup in Gaia
    /// </summary>
    public static class GaiaHDRPPipelineUtils
    {
        #region Variables

        //Public
        public static float m_waitTimer1 = 1f;
        public static float m_waitTimer2 = 3f;

        //Private
#if HDPipeline
        private static VolumeProfile m_volumeProfile;
#endif

        #endregion

        #region Public Functions

        /// <summary>
        /// Updates the scene lighting
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="pipelineProfile"></param>
        public static void UpdateSceneLighting(GaiaLightingProfileValues profile, UnityPipelineProfile pipelineProfile, GaiaLightingProfile lightingProfile)
        {
#if HDPipeline
            GaiaLighting.LightingSavedSetup(lightingProfile, profile);

            GameObject hdrp2019_3Lighting = GameObject.Find("Sky and Fog Volume");
            GameObject hdrp2019_3PostFX = GameObject.Find("Post Process Volume");
            if (hdrp2019_3Lighting != null || hdrp2019_3PostFX != null)
            {
                if (EditorUtility.DisplayDialog("HDRP Volumes Found!", "Default HDRP Lighting is setup in your scene, we recommend removing these so it doesn't conflict with Gaia HDRP lighting. Would you like to remove them from your scene?", "Yes", "No"))
                {
                    if (hdrp2019_3Lighting != null)
                    {
                        Object.DestroyImmediate(hdrp2019_3Lighting);
                    }

                    if (hdrp2019_3PostFX != null)
                    {
                        Object.DestroyImmediate(hdrp2019_3PostFX);
                    }
                }
            }

            m_volumeProfile = GetAndSetVolumeProfile(pipelineProfile, false);
            if (m_volumeProfile == null)
            {
                Debug.LogError("Volume profile could not be found. Check that 'Unity Pipeline Gaia Profile' HD Scene Lighting is set to 'HD Volume Profile'");
            }
            else
            {
                GaiaLighting.SetupAmbientAudio(profile);

                if (profile.m_profileType != GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
                {
                    SetSkyboxAndFog2019_3(profile, m_volumeProfile);
                    SetAmbientLight(profile, m_volumeProfile);
                    SetShadows(profile, m_volumeProfile);
                    SetSunSettings(profile);
                    SetAntiAliasing(lightingProfile.m_antiAliasingMode);

                    GaiaLighting.RemoveAllPostProcessV2();
                    if (lightingProfile.m_enablePostProcessing)
                    {
                        SetPostProcessing(pipelineProfile, profile);
                    }
                    else
                    {
                        RemovePostProcesing(pipelineProfile);
                    }
                    SetCamera2019_3(lightingProfile);

                    GaiaLighting.NewGlobalReflectionProbe(lightingProfile);
                }
                else
                {
                    GaiaLighting.RemoveAllPostProcessV2();
                    if (lightingProfile.m_enablePostProcessing)
                    {
                        SetPostProcessing(pipelineProfile, profile);
                    }
                    else
                    {
                        RemovePostProcesing(pipelineProfile);
                    }
                    SetSkyboxAndFog2019_3(profile, m_volumeProfile);
                    SetAmbientLight(profile, m_volumeProfile);
                    SetShadows(profile, m_volumeProfile);
                    SetAntiAliasing(lightingProfile.m_antiAliasingMode);
                    SetCamera2019_3(lightingProfile);
                }

                EditorUtility.SetDirty(m_volumeProfile);
            }
#endif
            //Marks the scene as dirty
            MarkSceneDirty(false);
        }

        /// <summary>
        /// Sets the pipeline asset to the procedural worlds asset if the profile is set yo change it
        /// </summary>
        /// <param name="profile"></param>
        public static void SetPipelineAsset(UnityPipelineProfile profile)
        {
            if (GraphicsSettings.renderPipelineAsset == null)
            {
                GaiaPackageVersion unityVersion = GaiaPackageVersion.Unity2019_1;

                //Installation setup
                if (Application.unityVersion.Contains("2019.2"))
                {
                    unityVersion = GaiaPackageVersion.Unity2019_2;
                }
                else if (Application.unityVersion.Contains("2019.3"))
                {
                    unityVersion = GaiaPackageVersion.Unity2019_3;
                }
                else if (Application.unityVersion.Contains("2019.4"))
                {
                    unityVersion = GaiaPackageVersion.Unity2019_4;
                }

                UnityVersionPipelineAsset mapping = profile.m_highDefinitionPipelineProfiles.Find(x => x.m_unityVersion == unityVersion);
                string pipelineAssetName = "";

                if (mapping != null)
                {
                    pipelineAssetName = mapping.m_pipelineAssetName;
                }
                else
                {
                    Debug.LogError("Could not determine the correct render pipeline settings asset for this unity version / rendering pipeline!");
                    return;
                }

                GraphicsSettings.renderPipelineAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(GetAssetPath(pipelineAssetName));
            }

            profile.m_pipelineSwitchUpdates = true;
        }

        /// <summary>
        /// Sets HDRP default Lighting
        /// </summary>
        /// <param name="profile"></param>
        public static void SetDefaultHDRPLighting(UnityPipelineProfile profile, bool setDayInstead)
        {
            SetupDefaultSceneLighting2019_3(profile, setDayInstead);
        }

        /// <summary>
        /// Cleans up LWRP components in the scene
        /// </summary>
        public static void CleanUpHDRP(UnityPipelineProfile profile, GaiaSettings gaiaSettings)
        {
#if HDPipeline
            HDAdditionalCameraData[] camerasData = Object.FindObjectsOfType<HDAdditionalCameraData>();
            if (camerasData != null)
            {
                foreach (HDAdditionalCameraData data in camerasData)
                {
                    Object.DestroyImmediate(data);
                }
            }

            HDAdditionalLightData[] lightsData = Object.FindObjectsOfType<HDAdditionalLightData>();
            if (lightsData != null)
            {
                foreach (HDAdditionalLightData data in lightsData)
                {
                    Object.DestroyImmediate(data);
                }
            }

            HDAdditionalReflectionData[] reflectionsData = Object.FindObjectsOfType<HDAdditionalReflectionData>();
            if (reflectionsData != null)
            {
                foreach (HDAdditionalReflectionData data in reflectionsData)
                {
                    Object.DestroyImmediate(data);
                }
            }

            GameObject planarObject = GameObject.Find("HD Planar Water Reflections");
            if (planarObject != null)
            {
                Object.DestroyImmediate(planarObject);
            }

            GameObject hdEnvironment = GameObject.Find("HD Environment Volume");
            if (hdEnvironment != null)
            {
                Object.DestroyImmediate(hdEnvironment);
            }

            GameObject hdPostProcessEnvironment = GameObject.Find("HD Post Processing Environment Volume");
            if (hdPostProcessEnvironment != null)
            {
                Object.DestroyImmediate(hdPostProcessEnvironment);
            }

            if (profile.m_underwaterHorizonMaterial != null)
            {
                profile.m_underwaterHorizonMaterial.shader = Shader.Find(profile.m_builtInHorizonObjectShader);
            }

            //reverting default water mesh quality
            gaiaSettings.m_gaiaWaterProfile.m_customMeshQuality = 2;
            GaiaWater.UpdateWaterMeshQuality(gaiaSettings.m_gaiaWaterProfile, gaiaSettings.m_gaiaWaterProfile.m_waterPrefab);

            GameObject waterPrefab = GameObject.Find(gaiaSettings.m_gaiaWaterProfile.m_waterPrefab.name);
            if (waterPrefab != null)
            {
                PWS_WaterSystem reflection = waterPrefab.GetComponent<PWS_WaterSystem>();
                if (reflection == null)
                {
                    reflection = waterPrefab.AddComponent<PWS_WaterSystem>();
                }
            }

            Terrain[] terrains = Terrain.activeTerrains;
            if (terrains != null)
            {
                foreach (Terrain terrain in terrains)
                {
                    terrain.materialTemplate = profile.m_builtInTerrainMaterial;
                }
            }

            Terrain[] terrainDetails = Terrain.activeTerrains;
            if (terrainDetails.Length > 0)
            {
                if (profile.m_HDDisableTerrainDetails)
                {
                    foreach (Terrain terrain in terrainDetails)
                    {
                        terrain.detailObjectDensity = 0.5f;
                    }

                    Debug.Log("Terrain details for each active terrain in the scene has been set back to 0.5 by default. They was disabled in HDRP due to HDRP not yet supported the terrain detail system.");
                }
            }

            GraphicsSettings.renderPipelineAsset = null;

            GaiaLighting.GetProfile(gaiaSettings.m_gaiaLightingProfile, gaiaSettings.m_pipelineProfile, GaiaConstants.EnvironmentRenderer.BuiltIn);

            if (waterPrefab != null)
            {
                Material waterMat = GaiaWater.GetGaiaOceanMaterial();
                if (waterMat != null)
                {
                    GaiaWater.GetProfile(gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex, waterMat, gaiaSettings.m_gaiaWaterProfile, gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, true, false);
                }
                else
                {
                    Debug.Log("Material could not be found");
                }
            }

            MarkSceneDirty(false);
            EditorUtility.SetDirty(profile);
            profile.m_activePipelineInstalled = GaiaConstants.EnvironmentRenderer.BuiltIn;

            GaiaManagerEditor manager = EditorWindow.GetWindow<Gaia.GaiaManagerEditor>(false, "Gaia Manager");
            if (manager != null)
            {
                manager.GaiaManagerStatusCheck();
            }

            bool isChanged = false;
            string currBuildSettings = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (currBuildSettings.Contains("HDPipeline"))
            {
                currBuildSettings = currBuildSettings.Replace("HDPipeline;", "");
                currBuildSettings = currBuildSettings.Replace("HDPipeline", "");
                isChanged = true;
            }

            if (isChanged)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, currBuildSettings);
            }
#endif
        }

        /// <summary>
        /// Starts the LWRP Setup
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public static IEnumerator StartHDRPSetup(UnityPipelineProfile profile)
        {
            if (profile == null)
            {
                Debug.LogError("UnityPipelineProfile is empty");
                yield return null;
            }
            else
            {
                EditorUtility.DisplayProgressBar("Installing HighDefinition", "Updating scripting defines", 0.5f);
                m_waitTimer1 -= Time.deltaTime;
                if (m_waitTimer1 < 0)
                {
                    SetScriptingDefines();
                }
                else
                {
                    yield return null;
                }

                while (EditorApplication.isCompiling == true)
                {
                    yield return null;
                }

                EditorUtility.DisplayProgressBar("Installing HighDefinition", "Updating scene to HighDefinition", 0.75f);
                m_waitTimer2 -= Time.deltaTime;
                if (m_waitTimer2 < 0)
                {
                    ConfigureSceneToHDRP(profile);
                    SetDefaultHDRPLighting(profile, true);
                    profile.m_pipelineSwitchUpdates = false;

                    EditorUtility.ClearProgressBar();
                }
                else
                {
                    yield return null;
                }
            }
        }

#if HDPipeline

        /// <summary>
        /// Sets the HDRP Anti Aliasing
        /// </summary>
        /// <param name="antiAliasingMode"></param>
        public static void SetAntiAliasing(GaiaConstants.GaiaProAntiAliasingMode antiAliasingMode)
        {
            Camera camera = GetCamera();
            if (camera != null)
            {
                HDAdditionalCameraData cameraData = camera.gameObject.GetComponent<HDAdditionalCameraData>();
                if (cameraData != null)
                {
                    switch (antiAliasingMode)
                    {
                        case GaiaConstants.GaiaProAntiAliasingMode.None:
                            cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
                            break;
                        case GaiaConstants.GaiaProAntiAliasingMode.FXAA:
                            cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing;
                            break;
                        case GaiaConstants.GaiaProAntiAliasingMode.SMAA:
                            //cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                            break;
                        case GaiaConstants.GaiaProAntiAliasingMode.TAA:
                            cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
                            break;
                        case GaiaConstants.GaiaProAntiAliasingMode.MSAA:
                            Debug.Log("MSAA Anti Aliasing is not supported in HDRP. Please select a different Anti Aliasing method");
                            break;
                    }
                }
            }
        }

#endif

#endregion

        #region Private Functions

        /// <summary>
        /// Configures scripting defines in the project
        /// </summary>
        private static void SetScriptingDefines()
        {
            bool isChanged = false;
            string currBuildSettings = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (!currBuildSettings.Contains("HDPipeline"))
            {
                if (string.IsNullOrEmpty(currBuildSettings))
                {
                    currBuildSettings = "HDPipeline";
                }
                else
                {
                    currBuildSettings += ";HDPipeline";
                }
                isChanged = true;
            }
            if (currBuildSettings.Contains("LWPipeline"))
            {
                currBuildSettings = currBuildSettings.Replace("LWPipeline;", "");
                currBuildSettings = currBuildSettings.Replace("LWPipeline", "");
                isChanged = true;
            }
            if (isChanged)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, currBuildSettings);
            }
        }

        /// <summary>
        /// Configures project for HDRP
        /// </summary>
        /// <param name="profile"></param>
        private static void ConfigureSceneToHDRP(UnityPipelineProfile profile)
        {
            GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
            if (gaiaSettings.m_currentRenderer != GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                Debug.LogError("Unable to configure your scene/project to LWRP as the current render inside of gaia does not equal Lightweight as it's active render pipeline. This process [GaiaLWRPPipelineUtils.ConfigureSceneToLWRP()] will now exit.");
                return;
            }

            if (profile.m_setHDPipelineProfile)
            {
                SetPipelineAsset(profile);
            }

            if (profile.m_HDAutoConfigureCamera)
            {
                ConfigureCamera();
            }

            if (profile.m_HDAutoConfigureLighting)
            {
                ConfigureLighting();
            }

            if (profile.m_HDAutoConfigureWater)
            {
                ConfigureWater(profile, gaiaSettings);
            }

            if (profile.m_HDAutoConfigureProbes)
            {
                ConfigureReflectionProbes();
            }

            if (profile.m_HDAutoConfigureTerrain)
            {
                ConfigureTerrain(profile);
            }

            if (profile.m_HDAutoConfigureBiomePostFX)
            {
                UpdateBiomePostFX();
            }

            FinalizeHDRP(profile);
        }

        /// <summary>
        /// Sets up default HDRP lighting in your scene
        /// </summary>
        private static void SetupDefaultSceneLighting(UnityPipelineProfile profile, bool setDayInstead)
        {
#if !HDPipeline
            Debug.LogError("HDRP has not been installed with Gaia. Please go to standard tab and setup to install HDRP into Gaia");
#else
            if (setDayInstead)
            {
                GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
                GaiaLighting.GetProfile(gaiaSettings.m_gaiaLightingProfile, profile, GaiaConstants.EnvironmentRenderer.HighDefinition);
            }
            else
            {
                Volume sceneVolume = Object.FindObjectOfType<Volume>();
                if (sceneVolume != null)
                {
                    if (sceneVolume.sharedProfile.Has<VisualEnvironment>())
                    {
                        return;
                    }
                }
                if (EditorUtility.DisplayDialog("Setup Default HDRP Lighting", "It looks like you are starting with a new terrain and HDRP lighting is not setup in your scene. Would you like to setup default HDRP lighting suitable for editing your terrain?", "Yes", "No"))
                {
                    GameObject parentObject = GetOrCreateParentObject("Gaia Lighting Environment", true);
                    if (string.IsNullOrEmpty(profile.m_HDDefaultSceneLighting))
                    {
                        Debug.LogError("'HD Default Scene Lighting' is empty please check 'Unity Pipeline Gaia Profile' name input is not empty");
                        return;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(profile.m_HDVolumeObjectName))
                        {
                            Debug.LogError("'HD Volume Object' is empty please check 'Unity Pipeline Gaia Profile' name input is not empty");
                            return;
                        }

                        GameObject volumeObject = GetOrCreateVolumeObject(profile, true);
                        Volume volume = volumeObject.GetComponent<Volume>();
                        if (volume == null)
                        {
                            volume = volumeObject.AddComponent<Volume>();
                        }

                        volume.isGlobal = true;
                        if (volume.sharedProfile == null)
                        {
                            volume.sharedProfile = GetAndSetVolumeProfile(profile, true);
                        }
                        else
                        {
                            if (volume.sharedProfile.name != profile.m_HDDefaultSceneLighting)
                            {
                                volume.sharedProfile = GetAndSetVolumeProfile(profile, true);
                            }
                        }  

                        Light sunLight = GetSunLight();
                        if (sunLight != null)
                        {
                            HDAdditionalLightData lightData = GetHDLightData(sunLight);

                            lightData.intensity = 6f;
                            lightData.UpdateAllLightValues();
                            sunLight.intensity = 6f;
                        }

                        if (string.IsNullOrEmpty(profile.m_HDDefaultPostProcessing))
                        {
                            Debug.LogError("'HD Volume Object' is empty please check 'Unity Pipeline Gaia Profile' name input is not empty");
                            return;
                        }
                        GameObject postVolumeObject = GameObject.Find(profile.m_HDPostVolumeObjectName);
                        if (postVolumeObject == null)
                        {
                            postVolumeObject = new GameObject(profile.m_HDPostVolumeObjectName);
                            postVolumeObject.AddComponent<Volume>();
                        }

                        Volume postVolume = postVolumeObject.GetComponent<Volume>();
                        if (volume == null)
                        {
                            postVolume = postVolumeObject.AddComponent<Volume>();
                            postVolume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GetAssetPath(profile.m_HDDefaultPostProcessing));
                            postVolume.isGlobal = true;
                        }
                        else
                        {
                            postVolume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GetAssetPath(profile.m_HDDefaultPostProcessing));
                            postVolume.isGlobal = true;
                        }

                        GaiaLighting.RemovePostProcessing();

                        volumeObject.transform.SetParent(parentObject.transform);
                        postVolumeObject.transform.SetParent(parentObject.transform);
                    }
                }

                GaiaLighting.RemovePostProcessing();
            }
#endif
        }

        /// <summary>
        /// Sets up the default HDRP Lighting in your scene in 2019_3_OR_NEWER
        /// </summary>
        /// <param name="profile"></param>
        private static void SetupDefaultSceneLighting2019_3(UnityPipelineProfile profile, bool setDayInstead)
        {
#if !HDPipeline
            Debug.LogError("HDRP has not been installed with Gaia. Please go to standard tab and extra settings to install HDRP into Gaia");
#else
            if (setDayInstead)
            {
                GaiaSettings gaiaSettings = GaiaUtils.GetGaiaSettings();
                GaiaLighting.GetProfile(gaiaSettings.m_gaiaLightingProfile, profile, GaiaConstants.EnvironmentRenderer.HighDefinition);
            }
            else
            {
                Volume sceneVolume = Object.FindObjectOfType<Volume>();
                if (sceneVolume != null)
                {
                    if (sceneVolume.sharedProfile.Has<VisualEnvironment>())
                    {
                        return;
                    }
                }

                if (EditorUtility.DisplayDialog("Setup Default HDRP Lighting", "It looks like you are starting with a new terrain and HDRP lighting is not setup in your scene. Would you like to setup default HDRP lighting suitable for editing your terrain?", "Yes", "No"))
                {
                    GameObject parentObject = GetOrCreateParentObject(GaiaConstants.gaiaLightingObject, true);
                    if (string.IsNullOrEmpty(profile.m_HDDefaultSceneLighting))
                    {
                        Debug.LogError("'HD Default Scene Lighting' is empty please check 'Unity Pipeline Gaia Profile' name input is not empty");
                        return;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(profile.m_HDVolumeObjectName))
                        {
                            Debug.LogError("'HD Volume Object' is empty please check 'Unity Pipeline Gaia Profile' name input is not empty");
                            return;
                        }
                        GameObject volumeObject = GameObject.Find(profile.m_HDVolumeObjectName);
                        if (volumeObject == null)
                        {
                            volumeObject = new GameObject(profile.m_HDVolumeObjectName);
                            volumeObject.AddComponent<Volume>();
                        }

                        Volume volume = volumeObject.GetComponent<Volume>();
                        if (volume == null)
                        {
                            volume = volumeObject.AddComponent<Volume>();
                            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GetAssetPath(profile.m_HDDefaultSceneLighting));
                            volume.isGlobal = true;
                        }
                        else
                        {
                            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GetAssetPath(profile.m_HDDefaultSceneLighting));
                            volume.isGlobal = true;
                        }

                        Light sunLight = GetSunLight();
                        if (sunLight != null)
                        {
                            HDAdditionalLightData lightData = GetHDLightData(sunLight);

                            lightData.lightUnit = LightUnit.Lux;
                            lightData.color = GaiaUtils.GetColorFromHTML("FFECD6");
                            lightData.intensity = 12f;
                        }

                        if (string.IsNullOrEmpty(profile.m_HDDefaultPostProcessing))
                        {
                            Debug.LogError("'HD Volume Object' is empty please check 'Unity Pipeline Gaia Profile' name input is not empty");
                            return;
                        }
                        GameObject postVolumeObject = GameObject.Find(profile.m_HDPostVolumeObjectName);
                        if (postVolumeObject == null)
                        {
                            postVolumeObject = new GameObject(profile.m_HDPostVolumeObjectName);
                            postVolumeObject.AddComponent<Volume>();
                        }

                        Volume postVolume = postVolumeObject.GetComponent<Volume>();
                        if (volume == null)
                        {
                            postVolume = postVolumeObject.AddComponent<Volume>();
                            postVolume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GetAssetPath(profile.m_HDDefaultPostProcessing));
                            postVolume.isGlobal = true;
                        }
                        else
                        {
                            postVolume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GetAssetPath(profile.m_HDDefaultPostProcessing));
                            postVolume.isGlobal = true;
                        }

                        GaiaLighting.RemovePostProcessing();

                        volumeObject.transform.SetParent(parentObject.transform);
                        postVolumeObject.transform.SetParent(parentObject.transform);
                    }
                }
            }
#endif
        }

#if HDPipeline

        /// <summary>
        /// Sets the skybox and fog settings in 2019_3_OR_NEWER
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="volumeProfile"></param>
        private static void SetSkyboxAndFog2019_3(GaiaLightingProfileValues profile, VolumeProfile volumeProfile)
        {
#if HDPipeline
            VisualEnvironment visualEnvironment;
            HDRISky hDRISky;
            PhysicallyBasedSky physicallyBasedSky;
            GradientSky gradientSky;
            UnityEngine.Rendering.HighDefinition.Fog volumetricFog;
            if (volumeProfile.TryGet(out visualEnvironment))
            {
                if (volumeProfile.TryGet(out hDRISky))
                {
                    if (volumeProfile.TryGet(out physicallyBasedSky))
                    {
                        if (volumeProfile.TryGet(out gradientSky))
                        {
                            switch (profile.m_hDSkyType)
                            {
                                case GaiaConstants.HDSkyType.HDRI:
                                    visualEnvironment.skyType.value = 1;
                                    hDRISky.active = true;
                                    hDRISky.hdriSky.value = profile.m_hDHDRISkybox;
                                    hDRISky.exposure.value = profile.m_hDHDRIExposure;
                                    hDRISky.multiplier.value = profile.m_hDHDRIMultiplier;
                                    hDRISky.rotation.value = profile.m_sunRotation;

                                    physicallyBasedSky.active = false;
                                    gradientSky.active = false;
                                    break;
                                case GaiaConstants.HDSkyType.Procedural:
                                    visualEnvironment.skyType.value = 4;
                                    hDRISky.active = false;
                                    physicallyBasedSky.active = true;

                                    //Geometry
                                    physicallyBasedSky.planetaryRadius.value = profile.m_hDPBSPlanetaryRadius;
                                    physicallyBasedSky.planetCenterPosition.value = profile.m_hDPBSPlantetCenterPosition;
                                    //Air
                                    //physicallyBasedSky.airDensityR.value = profile.m_hDPBSAirOpacity.r;
                                    //physicallyBasedSky.airDensityG.value = profile.m_hDPBSAirOpacity.g;
                                    //physicallyBasedSky.airDensityB.value = profile.m_hDPBSAirOpacity.b;
                                    physicallyBasedSky.airDensityR.value = 0.04534f;
                                    physicallyBasedSky.airDensityG.value = 0.1023724f;
                                    physicallyBasedSky.airDensityB.value = 0.2326406f;
                                    physicallyBasedSky.airTint.value = profile.m_hDPBSAirAlbedo;
                                    physicallyBasedSky.airMaximumAltitude.value = profile.m_hDPBSAirMaximumAltitude;
                                    //Aerosols
                                    //physicallyBasedSky.aerosolDensity.value = profile.m_hDPBSAerosolDensity;
                                    physicallyBasedSky.aerosolDensity.value = 0.01f;
                                    physicallyBasedSky.aerosolTint.value = profile.m_hDPBSAerosolAlbedo;
                                    physicallyBasedSky.aerosolMaximumAltitude.value = profile.m_hDPBSAerosolMaximumAltitude;
                                    physicallyBasedSky.aerosolAnisotropy.value = profile.m_hDPBSAerosolAnisotropy;
                                    //Planet
                                    physicallyBasedSky.planetRotation.value = profile.m_hDPBSPlanetRotation;
                                    physicallyBasedSky.groundTint.value = profile.m_hDPBSGroundTint;
                                    physicallyBasedSky.groundColorTexture.value = profile.m_hDPBSGroundAlbedoTexture;
                                    physicallyBasedSky.groundEmissionTexture.value = profile.m_hDPBSGroundEmissionTexture;
                                    //Space
                                    physicallyBasedSky.spaceRotation.value = profile.m_hDPBSSpaceRotation;
                                    physicallyBasedSky.spaceEmissionTexture.value = profile.m_hDPBSSpaceEmissionTexture;
                                    //Artistic Overrides
                                    physicallyBasedSky.colorSaturation.value = profile.m_hDPBSColorSaturation;
                                    physicallyBasedSky.alphaSaturation.value = profile.m_hDPBSAlphaSaturation;
                                    physicallyBasedSky.alphaMultiplier.value = profile.m_hDPBSAlphaMultiplier;
                                    physicallyBasedSky.horizonTint.value = profile.m_hDPBSHorizonTint;
                                    physicallyBasedSky.horizonZenithShift.value = profile.m_hDPBSHorizonZenithShift;
                                    physicallyBasedSky.zenithTint.value = profile.m_hDPBSZenithTint;
                                    //Miscellaneous
                                    physicallyBasedSky.numberOfBounces.value = profile.m_hDPBSNumberOfBounces;
                                    switch (profile.m_hDPBSIntensityMode)
                                    {
                                        case GaiaConstants.HDIntensityMode.Exposure:
                                            physicallyBasedSky.skyIntensityMode.value = SkyIntensityMode.Exposure;
                                            physicallyBasedSky.exposure.value = profile.m_hDPBSExposure;
                                            break;
                                        case GaiaConstants.HDIntensityMode.Multiplier:
                                            physicallyBasedSky.skyIntensityMode.value = SkyIntensityMode.Multiplier;
                                            physicallyBasedSky.multiplier.value = profile.m_hDPBSMultiplier;
                                            break;
                                    }
                                    physicallyBasedSky.includeSunInBaking.value = profile.m_hDPBSIncludeSunInBaking;

                                    gradientSky.active = false;
                                    break;
                                case GaiaConstants.HDSkyType.Gradient:
                                    visualEnvironment.skyType.value = 3;
                                    hDRISky.active = false;
                                    physicallyBasedSky.active = false;
                                    gradientSky.active = true;
                                    gradientSky.top.value = profile.m_hDGradientTopColor;
                                    gradientSky.middle.value = profile.m_hDGradientMiddleColor;
                                    gradientSky.bottom.value = profile.m_hDGradientBottomColor;
                                    gradientSky.gradientDiffusion.value = profile.m_hDGradientDiffusion;
                                    gradientSky.exposure.value = profile.m_hDGradientExposure;
                                    gradientSky.multiplier.value = profile.m_hDGradientMultiplier;
                                    break;
                            }

                            if (volumeProfile.TryGet(out volumetricFog))
                            {
                                switch (profile.m_hDFogType2019_3)
                                {
                                    case GaiaConstants.HDFogType2019_3.None:
                                        volumetricFog.active = false;
                                        volumetricFog.enabled.value = false;
                                        break;
                                    case GaiaConstants.HDFogType2019_3.Volumetric:
                                        volumetricFog.active = true;
                                        volumetricFog.enabled.value = true;
                                        volumetricFog.albedo.value = profile.m_hDVolumetricFogScatterColor;
                                        volumetricFog.meanFreePath.value = profile.m_hDVolumetricFogDistance;
                                        volumetricFog.baseHeight.value = profile.m_hDVolumetricFogBaseHeight;
                                        volumetricFog.maximumHeight.value = profile.m_hDVolumetricFogMeanHeight;
                                        volumetricFog.enableVolumetricFog.value = true;
                                        volumetricFog.anisotropy.value = profile.m_hDVolumetricFogAnisotropy;
                                        volumetricFog.globalLightProbeDimmer.value = profile.m_hDVolumetricFogProbeDimmer;
                                        volumetricFog.maxFogDistance.value = profile.m_hDVolumetricFogMaxDistance;
                                        volumetricFog.depthExtent.value = profile.m_hDVolumetricFogDepthExtent;
                                        volumetricFog.sliceDistributionUniformity.value = profile.m_hDVolumetricFogSliceDistribution;
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            #if GAIA_PRO_PRESENT
            if (profile.m_profileType != GaiaConstants.GaiaLightingProfileType.ProceduralWorldsSky)
            {
                ProceduralWorldsGlobalWeather.RemoveGlobalWindShader();
            }
            else
            {
                ProceduralWorldsGlobalWeather.AddGlobalWindShader(GaiaConstants.GaiaGlobalWindType.Custom);

                if (GaiaGlobal.Instance != null)
                {
                    GaiaGlobal.Instance.GaiaTimeOfDayValue.m_todEnabled = false;
                }
            }
            #endif
#endif
        }

        /// <summary>
        /// Setup and configure 2019.3 HDRP Camera
        /// </summary>
        /// <param name="profile"></param>
        private static void SetCamera2019_3(GaiaLightingProfile profile)
        {
            Camera camera = GetCamera();
            if (camera != null)
            {
                HDAdditionalCameraData cameraData = GetHDCameraData(camera);
                if (cameraData != null)
                {
                    HDAdditionalCameraData.AntialiasingMode aaMode = HDAdditionalCameraData.AntialiasingMode.None;
                    switch (profile.m_antiAliasingMode)
                    {
                        case GaiaConstants.GaiaProAntiAliasingMode.None:
                            cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
                            break;
                        case GaiaConstants.GaiaProAntiAliasingMode.FXAA:
                            cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing;
                            aaMode = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing;
                            break;
                        case GaiaConstants.GaiaProAntiAliasingMode.SMAA:
                            cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                            aaMode = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                            break;
                        case GaiaConstants.GaiaProAntiAliasingMode.TAA:
                            cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
                            aaMode = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;                      
                            break;
                    }

                    if (EditorPrefs.HasKey(HDRPKeys.sceneViewAntialiasing))
                    {
                        EditorPrefs.SetInt(HDRPKeys.sceneViewAntialiasing, (int)aaMode);
                    }

                    cameraData.taaSharpenStrength = profile.m_antiAliasingTAAStrength;
                    cameraData.dithering = profile.m_cameraDithering;
                    cameraData.physicalParameters.aperture = profile.m_cameraAperture;
                    camera.sensorSize = profile.m_cameraSensorSize;
                    camera.usePhysicalProperties = profile.m_usePhysicalCamera;
                }
            }
        }

        /// <summary>
        /// Sets the ambient lighting settings
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="volumeProfile"></param>
        private static void SetAmbientLight(GaiaLightingProfileValues profile, VolumeProfile volumeProfile)
        {
            VisualEnvironment visualEnvironment;

            if (volumeProfile.TryGet(out visualEnvironment))
            {
                switch (profile.m_hDAmbientMode)
                {
                    case GaiaConstants.HDAmbientMode.Static:
                        visualEnvironment.skyAmbientMode.value = SkyAmbientMode.Static;
                        break;
                    case GaiaConstants.HDAmbientMode.Dynamic:
                        visualEnvironment.skyAmbientMode.value = SkyAmbientMode.Dynamic;
                        break;
                }
            }
        }

        /// <summary>
        /// Sets the shadow settings
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="volumeProfile"></param>
        private static void SetShadows(GaiaLightingProfileValues profile, VolumeProfile volumeProfile)
        {
            HDShadowSettings hDShadow;
            if (volumeProfile.TryGet(out hDShadow))
            {
                hDShadow.maxShadowDistance.value = profile.m_hDShadowDistance;
            }

            ContactShadows contactShadows;
            if (volumeProfile.TryGet(out contactShadows))
            {
                contactShadows.active = profile.m_hDContactShadows;
                contactShadows.enable.value = profile.m_hDContactShadows;
                contactShadows.opacity.value = profile.m_hDContactShadowOpacity;
                contactShadows.maxDistance.value = profile.m_hDContactShadowsDistance;

                switch (profile.m_hDContactShadowQuality)
                {
                    case GaiaConstants.ContactShadowsQuality.Low:
                        contactShadows.sampleCount = 8;
                        profile.m_hDContactShadowCustomQuality = 8;
                        break;
                    case GaiaConstants.ContactShadowsQuality.Medium:
                        contactShadows.sampleCount = 16;
                        profile.m_hDContactShadowCustomQuality = 16;
                        break;
                    case GaiaConstants.ContactShadowsQuality.High:
                        contactShadows.sampleCount = 32;
                        profile.m_hDContactShadowCustomQuality = 32;
                        break;
                    case GaiaConstants.ContactShadowsQuality.Ultra:
                        contactShadows.sampleCount = 64;
                        profile.m_hDContactShadowCustomQuality = 64;
                        break;
                    case GaiaConstants.ContactShadowsQuality.Custom:
                        contactShadows.sampleCount = profile.m_hDContactShadowCustomQuality;
                        break;
                }
            }

            MicroShadowing microShadowing;
            if(volumeProfile.TryGet(out microShadowing))
            {
                microShadowing.active = profile.m_hDMicroShadows;
                microShadowing.enable.value = profile.m_hDMicroShadows;
                microShadowing.opacity.value = profile.m_hDMicroShadowOpacity;
            }
        }

#endif

        /// <summary>
        /// Sets the sun intensity
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="volumeProfile"></param>
        private static void SetSunSettings(GaiaLightingProfileValues profile)
        {
            Light light = GetSunLight();
            if (light != null)
            {
#if HDPipeline
                HDAdditionalLightData lightData = GetHDLightData(light);
                lightData.color = profile.m_hDSunColor;
                lightData.intensity = profile.m_hDSunIntensity;
                lightData.useContactShadow.level = -1;
                lightData.useContactShadow.useOverride = profile.m_hDContactShadows;
                switch (profile.m_hDShadowResolution)
                {
                    case GaiaConstants.HDShadowResolution.Resolution256:
                        lightData.SetShadowResolution(256);
                        break;
                    case GaiaConstants.HDShadowResolution.Resolution512:
                        lightData.SetShadowResolution(512);
                        break;
                    case GaiaConstants.HDShadowResolution.Resolution1024:
                        lightData.SetShadowResolution(1024);
                        break;
                    case GaiaConstants.HDShadowResolution.Resolution2048:
                        lightData.SetShadowResolution(2048);
                        break;
                    case GaiaConstants.HDShadowResolution.Resolution4096:
                        lightData.SetShadowResolution(4096);
                        break;
                    case GaiaConstants.HDShadowResolution.Resolution8192:
                        lightData.SetShadowResolution(8192);
                        break;
                }
#endif
            }

            //Rotates the sun to specified values
            RotateSun(profile.m_sunRotation, profile.m_sunPitch, light);
        }

        /// <summary>
        /// Sets post processing
        /// </summary>
        /// <param name="pipelineProfile"></param>
        /// <param name="profile"></param>
        private static void SetPostProcessing(UnityPipelineProfile pipelineProfile, GaiaLightingProfileValues profile)
        {
#if HDPipeline
            if (string.IsNullOrEmpty(profile.m_hDPostProcessingProfile))
            {
                Debug.LogError("Post Processing Profile in selected profile for HDRP is empty please make sure you've defined a profile. Exiting function!");
                return;
            }
            Volume volume = GetAndSetPostProcessVolume(pipelineProfile, profile);

            string profileName = "2019_3 " + profile.m_hDPostProcessingProfile;
            bool loadProfile = false;
            if (volume.sharedProfile == null)
            {
                loadProfile = true;
            }
            else if (volume.sharedProfile.name != profileName)
            {
                loadProfile = true;
            }

            if (loadProfile)
            {
                volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GetAssetPath(profileName));
            }
#endif
        }

        /// <summary>
        /// Removes HDRP Post processing
        /// </summary>
        /// <param name="pipelineProfile"></param>
        private static void RemovePostProcesing(UnityPipelineProfile pipelineProfile)
        {
            GameObject postObject = GameObject.Find(pipelineProfile.m_HDPostVolumeObjectName);
            if (postObject != null)
            {
                Object.DestroyImmediate(postObject);
            }
        }

        /// <summary>
        /// Updates the biome post fx to SRP volume profile
        /// </summary>
        private static void UpdateBiomePostFX()
        {
            GaiaPostProcessBiome[] gaiaPostProcessBiomes = GameObject.FindObjectsOfType<GaiaPostProcessBiome>();
            if (gaiaPostProcessBiomes.Length > 0)
            {
                foreach (GaiaPostProcessBiome biome in gaiaPostProcessBiomes)
                {
#if UNITY_POST_PROCESSING_STACK_V2
                    PostProcessVolume processVolume = biome.GetComponent<PostProcessVolume>();
                    if (processVolume != null)
                    {
                        GameObject.DestroyImmediate(processVolume);
                    }
#endif
#if HDPipeline
                    Volume volumeComp = biome.GetComponent<Volume>();
                    if (volumeComp == null)
                    {
                        volumeComp = biome.gameObject.AddComponent<Volume>();
                        volumeComp.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GaiaUtils.GetAssetPath("HD " + biome.PostProcessingFileName + ".asset"));
                    }
#endif
                }
            }
        }

        /// <summary>
        /// Set the suns rotation and pitch
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="pitch"></param>
        private static void RotateSun(float rotation, float pitch, Light sunLight)
        {
            //Correct the angle
            float angleDegrees = rotation % 360f;

            float sunAngle = pitch;

            //Set new directional light rotation
            if (sunLight != null)
            {
                Vector3 newRotation = sunLight.gameObject.transform.rotation.eulerAngles;
                newRotation.y = angleDegrees;
                sunLight.gameObject.transform.rotation = Quaternion.Euler(sunAngle, -newRotation.y, 0f);
            }
        }

        /// <summary>
        /// Configures camera to LWRP
        /// </summary>
        private static void ConfigureCamera()
        {
            Camera camera = GetCamera();
            if (camera == null)
            {
                Debug.LogWarning("[GaiaLWRPPipelineUtils.ConfigureCamera()] A camera could not be found to upgrade in your scene.");
            }
            else
            {
#if HDPipeline
                HDAdditionalCameraData cameraData = GetHDCameraData(camera);
                if (cameraData == null)
                {
                    cameraData = camera.gameObject.AddComponent<HDAdditionalCameraData>();
                }
#endif
            }
        }

        /// <summary>
        /// Gets or creates HD camera data
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
#if HDPipeline
        private static HDAdditionalCameraData GetHDCameraData(Camera camera)
        {
            if (camera == null)
            {
                return null;
            }

            HDAdditionalCameraData cameraData = camera.gameObject.GetComponent<HDAdditionalCameraData>();
            if (cameraData == null)
            {
                cameraData = camera.gameObject.AddComponent<HDAdditionalCameraData>();
            }

            return cameraData;
        }

        /// <summary>
        /// Gets or creates HD Light data
        /// </summary>
        /// <param name="light"></param>
        /// <returns></returns>
        private static HDAdditionalLightData GetHDLightData(Light light)
        {
            if (light == null)
            {
                return null;
            }

            HDAdditionalLightData lightData = light.GetComponent<HDAdditionalLightData>();
            if (lightData == null)
            {
                lightData = light.gameObject.AddComponent<HDAdditionalLightData>();
            }

            return lightData;
        }

#endif

        /// <summary>
        /// Configures lighting to HDRP
        /// </summary>
        private static void ConfigureLighting()
        {
            Light[] lights = Object.FindObjectsOfType<Light>();
            ConfigureLighting2019_3(lights);
        }

        /// <summary>
        /// Configures lighting to HDRP 2019_3_OR_HIGHER
        /// </summary>
        private static void ConfigureLighting2019_3(Light[] lights)
        {
#if HDPipeline
            if (lights == null)
            {
                Debug.LogError("No lights could be found");
            }
            else
            {
                if (lights != null)
                {
                    foreach (Light light in lights)
                    {
                        if (light.gameObject.GetComponent<HDAdditionalLightData>() == null)
                        {
                            light.gameObject.AddComponent<HDAdditionalLightData>();
                        }
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Configures water to LWRP
        /// </summary>
        /// <param name="profile"></param>
        private static void ConfigureWater(UnityPipelineProfile profile, GaiaSettings gaiaSettings)
        {
            if (gaiaSettings == null)
            {
                Debug.LogError("Gaia settings could not be found. Please make sure gaia settings is import into your project");
            }
            else
            {
                if (profile.m_underwaterHorizonMaterial != null)
                {
                    profile.m_underwaterHorizonMaterial.shader = Shader.Find(profile.m_highDefinitionHorizonObjectShader);
                }

                GameObject waterObject = GameObject.Find(gaiaSettings.m_gaiaWaterProfile.m_waterPrefab.name);
                if (waterObject != null)
                {
                    Material waterMat = GaiaWater.GetGaiaOceanMaterial();
                    if (waterMat != null)
                    {
                        GaiaWater.GetProfile(gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex, waterMat, gaiaSettings.m_gaiaWaterProfile, gaiaSettings.m_pipelineProfile.m_activePipelineInstalled, true, false);
                    }
                    else
                    {
                        Debug.Log("Material could not be found");
                    }
                }
            }

            PWS_WaterSystem reflection = Object.FindObjectOfType<PWS_WaterSystem>();
            if (reflection != null)
            {
                reflection.m_skyboxOnly = true;
            }
        }

        /// <summary>
        /// Configures reflections to LWRP
        /// </summary>
        private static void ConfigureReflectionProbes()
        {
            ReflectionProbe[] reflectionProbes = Object.FindObjectsOfType<ReflectionProbe>();
            if (reflectionProbes != null)
            {
                foreach (ReflectionProbe probe in reflectionProbes)
                {
#if HDPipeline
                    if (probe.GetComponent<HDAdditionalReflectionData>() == null)
                    {
                        probe.gameObject.AddComponent<HDAdditionalReflectionData>();
                    }
#endif
                }
            }
        }

        /// <summary>
        /// Configures and setup the terrain
        /// </summary>
        /// <param name="profile"></param>
        private static void ConfigureTerrain(UnityPipelineProfile profile)
        {
            Terrain[] terrains = Terrain.activeTerrains;
            if (terrains != null)
            {
                foreach(Terrain terrain in terrains)
                {
                    terrain.materialTemplate = profile.m_highDefinitionTerrainMaterial;
                }
            }
        }

        /// <summary>
        /// Finalizes the HDRP setup for your project 
        /// </summary>
        /// <param name="profile"></param>
        private static void FinalizeHDRP(UnityPipelineProfile profile)
        {
            Terrain[] terrains = Terrain.activeTerrains;
            if (terrains.Length > 0)
            {
                if (profile.m_HDDisableTerrainDetails)
                {
                    foreach (Terrain terrain in terrains)
                    {
                        if (terrain.isActiveAndEnabled)
                        {
                            terrain.detailObjectDensity = 0f;
                        }
                    }

                    Debug.Log("Deactivating Unity Terrain Details. HDRP does not currently support terrain detail system. Detail Density has been set to 0 on all active terrains in the scene.");
                }
            }

            MarkSceneDirty(true);
            EditorUtility.SetDirty(profile);
            profile.m_activePipelineInstalled = GaiaConstants.EnvironmentRenderer.HighDefinition;

            GaiaManagerEditor manager = EditorWindow.GetWindow<Gaia.GaiaManagerEditor>(false, "Gaia Manager");
            if (manager != null)
            {
                manager.GaiaManagerStatusCheck(true);
            }
        }

        /// <summary>
        /// Gets and returns the main camera in the scene
        /// </summary>
        /// <returns></returns>
        private static Camera GetCamera()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                return camera;
            }

            camera = Object.FindObjectOfType<Camera>();
            if (camera != null)
            {
                return camera;
            }

            return null;
        }

#if HDPipeline
        /// <summary>
        /// Sets the volume profile
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        private static VolumeProfile GetAndSetVolumeProfile(UnityPipelineProfile profile, bool isDefault)
        {
            VolumeProfile volumeProfile = null;
            GameObject volumeObject = GetOrCreateVolumeObject(profile, isDefault);

            volumeProfile = volumeObject.GetComponent<Volume>().sharedProfile;

            return volumeProfile;
        }

        /// <summary>
        /// Gets and returns the post processing volume component
        /// </summary>
        /// <param name="pipelineProfile"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        private static Volume GetAndSetPostProcessVolume(UnityPipelineProfile pipelineProfile, GaiaLightingProfileValues profile)
        {
            Volume volume = null;
            GameObject volumeObject = GameObject.Find(pipelineProfile.m_HDPostVolumeObjectName);
            if (volumeObject == null)
            {
                volumeObject = new GameObject(pipelineProfile.m_HDPostVolumeObjectName);
                volume = volumeObject.AddComponent<Volume>();
                volume.isGlobal = true;
            }
            else
            {
                volume = volumeObject.GetComponent<Volume>();
                if (volume == null)
                {
                    volume = volumeObject.AddComponent<Volume>();
                    volume.isGlobal = true;
                }
                else
                {
                    volume.isGlobal = true;
                }
            }

            GameObject parentObject = GetOrCreateParentObject(GaiaConstants.gaiaLightingObject, true);
            volumeObject.transform.SetParent(parentObject.transform);

            return volume;
        }

        /// <summary>
        /// Gets or creates volume object
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        private static GameObject GetOrCreateVolumeObject(UnityPipelineProfile profile, bool isDefault)
        {
            string volumeName = profile.m_HDSceneLighting;
            if (isDefault)
            {
                volumeName = profile.m_2019_3HDDefaultSceneLighting;
            }
            else
            {
                volumeName = profile.m_2019_3HDSceneLighting;
            }

            GameObject volumeObject = GameObject.Find(profile.m_HDVolumeObjectName);
            Volume volume = null;
            if (volumeObject == null)
            {
                volumeObject = new GameObject(profile.m_HDVolumeObjectName);
                volume = volumeObject.AddComponent<Volume>();
                volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GetAssetPath(volumeName));
                volume.isGlobal = true;
            }
            else
            {
                volume = volumeObject.GetComponent<Volume>();
                if (volume == null)
                {
                    volume = volumeObject.AddComponent<Volume>();
                    volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GetAssetPath(volumeName));
                    volume.isGlobal = true;
                }
                else
                {
                    volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GetAssetPath(volumeName));
                    volume.isGlobal = true;
                }
            }

            GameObject parentObject = GetOrCreateParentObject(GaiaConstants.gaiaLightingObject, true);
            volumeObject.transform.SetParent(parentObject.transform);

            return volumeObject;
        }
#endif
        /// <summary>
        /// Gets and returns the sun light in the scene
        /// </summary>
        /// <returns></returns>
        private static Light GetSunLight()
        {
            Light light = null;
            GameObject lightObject = GameObject.Find("Directional Light");
            if (lightObject != null)
            {
                light = Object.FindObjectOfType<Light>();
                if (light != null)
                {
                    if (light.type == LightType.Directional)
                    {
                        return light;
                    }
                }
            }

            Light[] lights = Object.FindObjectsOfType<Light>();
            foreach (Light activeLight in lights)
            {
                if (activeLight.type == LightType.Directional)
                {
                    return activeLight;
                }
            }

            //Create it
            lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;

            return light;
        }

        /// <summary>
        /// Get the asset path of the first thing that matches the name
        /// </summary>
        /// <param name="name">Name to search for</param>
        /// <returns>The path or null</returns>
        private static string GetAssetPath(string name)
        {
            string[] assets = AssetDatabase.FindAssets(name, null);
            if (assets.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(assets[0]);
            }
            return null;
        }

        /// <summary>
        /// Get or create a parent object
        /// </summary>
        /// <param name="parentGameObject"></param>
        /// <param name="parentToGaia"></param>
        /// <returns>Parent Object</returns>
        private static GameObject GetOrCreateParentObject(string parentGameObject, bool parentToGaia)
        {
            //Get the parent object
            GameObject theParentGo = GameObject.Find(parentGameObject);

            if (theParentGo == null)
            {
                theParentGo = GameObject.Find(GaiaConstants.gaiaLightingObject);

                if (theParentGo == null)
                {
                    theParentGo = new GameObject(GaiaConstants.gaiaLightingObject);
                }
            }

            if (parentToGaia)
            {
                GameObject gaiaParent = GaiaUtils.GetGlobalSceneObject();
                if (gaiaParent != null)
                {
                    theParentGo.transform.SetParent(gaiaParent.transform);
                }
            }

            return theParentGo;
        }

        /// <summary>
        /// Save the assets and marks scene as dirty
        /// </summary>
        /// <param name="saveAlso"></param>
        private static void MarkSceneDirty(bool saveAlso)
        {
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                if (saveAlso)
                {
                    AssetDatabase.SaveAssets();
                }
            }
        }

        #endregion

        #region Prefs Functions

        /// <summary>
        /// Setup and configure 2019.3 HDRP Camera
        /// </summary>
        /// <param name="profile"></param>
        public static void SetPrefsCamera2019_3()
        {
            /*
#if HDPipeline
            Camera camera = GetCamera();
            if (camera != null)
            {
                HDAdditionalCameraData cameraData = GetHDCameraData(camera);
                if (cameraData != null)
                {
                    HDAdditionalCameraData.AntialiasingMode aaMode = HDAdditionalCameraData.AntialiasingMode.None;
                    switch (gaiaAntialiasing)
                    {
                        case GaiaConstants.GaiaProAntiAliasingMode.None:
                            cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
                            break;
                        case GaiaConstants.GaiaProAntiAliasingMode.FXAA:
                            cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing;
                            aaMode = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing;
                            break;
                        case GaiaConstants.GaiaProAntiAliasingMode.SMAA:
                            cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                            aaMode = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                            break;
                        case GaiaConstants.GaiaProAntiAliasingMode.TAA:
                            cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
                            aaMode = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
                            break;
                    }

                    if (EditorPrefs.HasKey(HDRPKeys.sceneViewAntialiasing))
                    {
                        EditorPrefs.SetInt(HDRPKeys.sceneViewAntialiasing, (int)aaMode);
                    }

                    cameraData.taaSharpenStrength = antiAliasingTAAStrength;
                    cameraData.dithering = cameraDithering;
                    cameraData.physicalParameters.aperture = cameraAperture;
                }
            }
#endif
*/
        }

        #endregion
    }
}
