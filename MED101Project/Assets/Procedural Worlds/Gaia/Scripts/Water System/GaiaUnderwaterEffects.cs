﻿using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using ProcedualWorlds.WaterSystem;
#if HDPipeline
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
#endif
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace Gaia
{
    public class GaiaUnderwaterEffects : MonoBehaviour
    {
        public static GaiaUnderwaterEffects Instance
        {
            get { return m_instnace; }

        }
        [SerializeField]
        private static GaiaUnderwaterEffects m_instnace;

        [HideInInspector]
        public GaiaConstants.EnvironmentRenderer RenderPipeline;
        public bool IsUnderwater { get; private set; }
        [Header("Global Settings")]
        public bool m_startingUnderwater = false;
        public float m_seaLevel = 50f;
        public Transform m_playerCamera;
        public GameObject m_underwaterParticles;
        public GameObject m_horizonObject;
        public PWS_WaterSystem m_waterReflections;
        public GameObject m_underwaterPostFX;
        public bool m_editCausticTextures = false;

        [Header("Caustic Settings")]
        public bool m_useCaustics = true;
        public Light m_mainLight;
        public int m_framesPerSecond = 24;
        [Range(1f, 100f)]
        public float m_causticSize = 15f;
        public List<Texture2D> m_causticTextures = new List<Texture2D>();

        [Header("Fog Settings")]
        public bool m_supportFog = true;
        public Gradient m_fogColorGradient;
        [HideInInspector]
        public Color m_fogColor = new Color(0.533f, 0.764f, 1f, 1f);
        [HideInInspector]
        public float m_fogDepth = 100f;
        public float m_fogDistance = 45f;
        public float m_nearFogDistance = -4f;
        public float m_fogDensity = 0.045f;

        [Header("Post Processing")]
        public bool m_supportPostFX = true;
        public float m_constUnderwaterPostExposure = 0f;
        public AnimationCurve m_gradientUnderwaterPostExposure = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));
        public Color m_constUnderwaterColorFilter = Color.white;
        public Gradient m_gradientUnderwaterColorFilter = new Gradient();
#if UNITY_POST_PROCESSING_STACK_V2
        public PostProcessProfile m_postProcessProfile;
#endif

        [Header("Audio Settings")]
        [Range(0f, 1f)]
        public float m_playbackVolume = 0.5f;
        public AudioClip m_submergeSoundFXDown;
        public AudioClip m_submergeSoundFXUp;
        public AudioClip m_underwaterSoundFX;

        private int m_indexNumber = 0;
        private AudioSource m_audioSource;
        private AudioSource m_audioSourceUnderwater;
        private ParticleSystem m_underwaterParticleSystem;

        private Color m_surfaceFogColor;
        private float m_surfaceFogDensity;
        private float m_surfaceFogStartDistance;
        private float m_surfaceFogEndDistance;

        private bool m_surfaceSetup = false;
        private bool m_underwaterSetup = false;
        private bool m_startingSystem = false;
        private List<MeshRenderer> m_horizonMeshRenders = new List<MeshRenderer>();
#if UNITY_EDITOR
        [HideInInspector]
        [SerializeField]
        private GaiaSceneInfo m_sceneInfo;
#endif
        [HideInInspector]
        [SerializeField]
        private Material m_underwaterMaterial;

#if HDPipeline
        [HideInInspector]
        [SerializeField]
        private VolumeProfile HDRPVolumeProfile;
        [HideInInspector]
        [SerializeField]
        private UnityEngine.Rendering.HighDefinition.Fog Fog;
        [HideInInspector]
        [SerializeField]
        private Volume EnvironmentVolume;
#endif

        private void Start()
        {
            RenderPipeline = GaiaUtils.GetActivePipeline();
            m_instnace = this;

            //only get scene info when running in editor, otherwise this can cause issues with the sea level
#if UNITY_EDITOR
            if (m_sceneInfo == null)
            {
                m_sceneInfo = GaiaSceneInfo.GetSceneInfo();
            }

            if (m_sceneInfo != null)
            {
                m_seaLevel = m_sceneInfo.m_seaLevel;
            }
#endif

            if (m_playerCamera == null)
            {
                if (Camera.main != null)
                {
                    m_playerCamera = Camera.main.transform;
                }
            }

            if (m_waterReflections == null)
            {
                m_waterReflections = FindObjectOfType<PWS_WaterSystem>();
            }

            if (m_mainLight == null)
            {
                m_mainLight = GaiaUtils.GetMainDirectionalLight();
            }

            if (m_audioSource == null)
            {
                m_audioSource = GetAudioSource();
            }

            if (m_audioSourceUnderwater == null)
            {
                m_audioSourceUnderwater = GetAudioSource();
            }

            if (m_underwaterPostFX != null)
            {
                m_underwaterPostFX.SetActive(true);
            }

            if (m_audioSourceUnderwater != null)
            {
                m_audioSourceUnderwater.clip = m_underwaterSoundFX;
                m_audioSourceUnderwater.loop = true;
                m_audioSourceUnderwater.volume = m_playbackVolume;
                m_audioSourceUnderwater.Stop();
            }

            if (m_startingUnderwater)
            {
                m_underwaterSetup = false;
                m_surfaceSetup = true;
            }
            else
            {
                m_underwaterSetup = true;
                m_surfaceSetup = false;
            }

            if (m_underwaterParticles != null)
            {
                m_underwaterParticleSystem = m_underwaterParticles.GetComponent<ParticleSystem>();
                if (m_underwaterParticleSystem != null)
                {
                    m_underwaterParticleSystem.Stop();
                }

                m_underwaterParticles.SetActive(false);
            }

            if (m_horizonObject != null)
            {
                MeshRenderer[] meshRenders = m_horizonObject.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer render in meshRenders)
                {
                    m_horizonMeshRenders.Add(render);
                    render.enabled = false;
                }
            }

            m_surfaceFogColor = RenderSettings.fogColor;
            m_surfaceFogDensity = RenderSettings.fogDensity;
            m_surfaceFogStartDistance = RenderSettings.fogStartDistance;
            m_surfaceFogEndDistance = RenderSettings.fogEndDistance;
        }

        private void OnEnable()
        {
            m_instnace = this;
        }

        private void OnDisable()
        {
            m_instnace = this;
            if (m_underwaterParticleSystem != null)
            {
                m_underwaterParticleSystem.Stop();
            }

            if (m_underwaterPostFX != null)
            {
                m_underwaterPostFX.SetActive(false);
            }
        }

        private void Update()
        {
            if (m_playerCamera == null)
            {
                Debug.LogError("Player Camera is missing from the setup. Will try find it for you");
                Camera camera = GaiaUtils.GetCamera();
                if (camera != null)
                {
                    m_playerCamera = camera.transform;
                }
                return;
            }
            else
            {
                if (m_playerCamera.position.y > m_seaLevel)
                {
                    if (!m_surfaceSetup)
                    {
                        IsUnderwater = SetupWaterSystems(false);
                        m_underwaterSetup = false;
                        m_surfaceSetup = true;
                    }
                }
                else
                {
                    if (!m_underwaterSetup)
                    {
                        UpdateSurfaceFogSettings();
                        IsUnderwater = SetupWaterSystems(true);
                        m_underwaterSetup = true;
                        m_surfaceSetup = false;
                    }

                    UpdateUnderwaterFog();
                    UpdateUnderwaterPostFX();
                }
            }

            if (m_underwaterMaterial == null)
            {
                m_underwaterMaterial = GaiaUtils.GetWaterMaterial(GaiaConstants.waterSurfaceObject, true);
            }
            else
            {
                UpdateUnderwaterMaterial();
            }
        }

        /// <summary>
        /// Plays the underwater caustic animations
        /// </summary>
        /// <param name="systemOn"></param>
        /// <returns></returns>
        private void CausticsAnimation()
        {
            if (m_mainLight != null)
            {
                m_mainLight.cookieSize = m_causticSize;
                m_mainLight.cookie = m_causticTextures[m_indexNumber];
                m_indexNumber++;
            }

            if (m_indexNumber == m_causticTextures.Count)
            {
                m_indexNumber = 0;
            }
        }

        /// <summary>
        /// Sets the water settings
        /// </summary>
        /// <param name="isUnderwater"></param>
        private bool SetupWaterSystems(bool isUnderwater)
        {
            bool active = isUnderwater;
            if (m_startingSystem)
            {
                if (isUnderwater)
                {
                    if (m_waterReflections != null)
                    {
                        m_waterReflections.enabled = false;
                    }

                    if (m_submergeSoundFXDown != null)
                    {
                        m_audioSource.PlayOneShot(m_submergeSoundFXDown, m_playbackVolume);
                    }

                    if (m_causticTextures != null)
                    {
                        if (m_useCaustics)
                        {
                            InvokeRepeating("CausticsAnimation", 0f, 1f / m_framesPerSecond);
                        }
                        else
                        {
                            CancelInvoke();
                        }
                    }

                    if (m_horizonMeshRenders != null)
                    {
                        foreach (MeshRenderer render in m_horizonMeshRenders)
                        {
                            render.enabled = true;
                        }
                    }

                    m_underwaterParticles.SetActive(true);
                    m_underwaterParticleSystem.Play();
                    m_audioSourceUnderwater.Play();
                }
                else
                {
                    if (m_waterReflections != null)
                    {
                        m_waterReflections.enabled = true;
                    }

                    if (m_submergeSoundFXUp != null)
                    {
                        m_audioSource.PlayOneShot(m_submergeSoundFXUp, m_playbackVolume);
                    }

                    CancelInvoke();

                    if (m_mainLight != null)
                    {
                        m_mainLight.cookie = null;
                    }

                    if (m_supportFog)
                    {
                        UpdateSurfaceFog();
                    }

                    if (m_horizonMeshRenders != null)
                    {
                        foreach (MeshRenderer render in m_horizonMeshRenders)
                        {
                            render.enabled = false;
                        }
                    }

                    m_underwaterParticleSystem.Stop();
                    m_underwaterParticles.SetActive(false);
                    m_audioSourceUnderwater.Stop();
                }

#if GAIA_PRO_PRESENT
                if (ProceduralWorldsGlobalWeather.Instance != null)
                {
                    ProceduralWorldsGlobalWeather.Instance.CheckUnderwaterParticlesVFX(isUnderwater);
                }
#endif
            }
            else
            {
                m_startingSystem = true;
            }

            return isUnderwater;
        }

        /// <summary>
        /// Updates the underwater fog
        /// </summary>
        private void UpdateUnderwaterFog()
        {
            if (m_supportFog)
            {
                if (m_fogColorGradient != null)
                {
                    if (m_playerCamera.position.y < m_seaLevel)
                    {
                        float distance = Mathf.Clamp01(m_playerCamera.position.y / m_fogDepth);
                        m_fogColor = m_fogColorGradient.Evaluate(distance);

                        if (RenderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
                        {
                            RenderSettings.fogColor = m_fogColor;
                            RenderSettings.fogDensity = m_fogDensity;
                            RenderSettings.fogStartDistance = m_nearFogDistance;
                            RenderSettings.fogEndDistance = m_fogDistance;
                        }
                        else
                        {
#if HDPipeline
                            if (EnvironmentVolume == null)
                            {
                                Volume[] volumes = FindObjectsOfType<Volume>();
                                if (volumes.Length > 0)
                                {
                                    foreach(Volume volume in volumes)
                                    {
                                        if (volume.isGlobal)
                                        {
                                            if (!volume.name.Contains("Processing"))
                                            {
                                                EnvironmentVolume = volume;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (HDRPVolumeProfile == null)
                                {
                                    HDRPVolumeProfile = EnvironmentVolume.sharedProfile;
                                }
                                else
                                {
                                    if (HDRPVolumeProfile.TryGet(out Fog))
                                    {
                                        Fog.active = true;
                                        Fog.albedo.value = m_fogColor;
                                        Fog.color.value = m_fogColor;
                                        Fog.meanFreePath.value = m_fogDistance;
                                    }
                                }
                            }
#endif
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the underwater post fx
        /// </summary>
        private void UpdateUnderwaterPostFX()
        {
            if (m_supportPostFX)
            {
#if UNITY_POST_PROCESSING_STACK_V2
                if (m_postProcessProfile == null)
                {
                    if (m_underwaterPostFX != null)
                    {
                        PostProcessVolume processVolume = m_underwaterPostFX.GetComponent<PostProcessVolume>();
                        if (processVolume != null)
                        {
                            m_postProcessProfile = processVolume.sharedProfile;
                        }
                    }
                }
                else
                {
                    UnityEngine.Rendering.PostProcessing.ColorGrading colorGrading;
#if GAIA_PRO_PRESENT
                    if (ProceduralWorldsGlobalWeather.Instance == null)
                    {
#endif
                        if (m_postProcessProfile.TryGetSettings(out colorGrading))
                        {
                            colorGrading.postExposure.value = m_constUnderwaterPostExposure;
                            colorGrading.postExposure.overrideState = true;
                            colorGrading.colorFilter.value = m_constUnderwaterColorFilter;
                            colorGrading.colorFilter.overrideState = true;
                        }
#if GAIA_PRO_PRESENT
                    }
                    else
                    {
                        if (m_postProcessProfile.TryGetSettings(out colorGrading))
                        {
                            colorGrading.postExposure.value = m_gradientUnderwaterPostExposure.Evaluate(GaiaGlobal.GetTimeOfDayMainValue());
                            colorGrading.postExposure.overrideState = true;
                            colorGrading.colorFilter.value = m_gradientUnderwaterColorFilter.Evaluate(GaiaGlobal.GetTimeOfDayMainValue());
                            colorGrading.colorFilter.overrideState = true;
                        }
                    }
#endif
                }
#endif
            }
        }

        private void UpdateSurfaceFog()
        {
            if (RenderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                RenderSettings.fogColor = m_surfaceFogColor;
                RenderSettings.fogDensity = m_surfaceFogDensity;
                RenderSettings.fogStartDistance = m_surfaceFogStartDistance;
                RenderSettings.fogEndDistance = m_surfaceFogEndDistance;
            }
            else
            {
#if HDPipeline
                if (EnvironmentVolume == null)
                {
                    Volume[] volumes = FindObjectsOfType<Volume>();
                    if (volumes.Length > 0)
                    {
                        foreach (Volume volume in volumes)
                        {
                            if (volume.isGlobal)
                            {
                                if (!volume.name.Contains("Processing"))
                                {
                                    EnvironmentVolume = volume;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (HDRPVolumeProfile == null)
                    {
                        HDRPVolumeProfile = EnvironmentVolume.sharedProfile;
                    }
                    else
                    {
                        if (HDRPVolumeProfile.TryGet(out Fog))
                        {
                            Fog.color.value = m_surfaceFogColor;
                            Fog.color.value = m_surfaceFogColor;
                            Fog.meanFreePath.value = m_surfaceFogEndDistance;
                        }
                    }
                }
#endif
            }
        }

        /// <summary>
        /// Updates the fog surface settings such as color density distance etc.
        /// </summary>
        public void UpdateSurfaceFogSettings()
        {
            if (RenderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                m_surfaceFogColor = RenderSettings.fogColor;
                m_surfaceFogDensity = RenderSettings.fogDensity;
                m_surfaceFogStartDistance = RenderSettings.fogStartDistance;
                m_surfaceFogEndDistance = RenderSettings.fogEndDistance;
            }
            else
            {
#if HDPipeline
                if (EnvironmentVolume == null)
                {
                    Volume[] volumes = FindObjectsOfType<Volume>();
                    if (volumes.Length > 0)
                    {
                        foreach (Volume volume in volumes)
                        {
                            if (volume.isGlobal)
                            {
                                if (!volume.name.Contains("Processing"))
                                {
                                    EnvironmentVolume = volume;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (HDRPVolumeProfile == null)
                    {
                        HDRPVolumeProfile = EnvironmentVolume.sharedProfile;
                    }
                    else
                    {
                        if (HDRPVolumeProfile.TryGet(out Fog))
                        {
                            m_surfaceFogColor = Fog.color.value;
                            m_surfaceFogEndDistance = Fog.meanFreePath.value;
                        }
                    }
                }
#endif
            }
        }

        /// <summary>
        /// Sets the underwater color on the material
        /// </summary>
        private void UpdateUnderwaterMaterial()
        {
            m_underwaterMaterial.SetColor(GaiaWeatherShaderID.m_underwaterColor, m_fogColor);
        }

        /// <summary>
        /// Gets and returns the audio source
        /// </summary>
        /// <returns></returns>
        private AudioSource GetAudioSource()
        {
            AudioSource audioSource = null;
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                return audioSource;
            }
            {
                return audioSource;
            }
        }

        public void LoadUnderwaterSystemAssets()
        {
#if UNITY_EDITOR
            Camera camera = GaiaUtils.GetCamera();
            if (camera != null)
            {
                m_playerCamera = camera.transform;
            }

            if (m_causticTextures == null || m_causticTextures.Count <= 0)
            {
                m_causticTextures = new List<Texture2D>();
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_001.tif")));
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_002.tif")));
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_003.tif")));
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_004.tif")));
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_005.tif")));
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_006.tif")));
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_007.tif")));
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_008.tif")));
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_009.tif")));
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_010.tif")));
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_011.tif")));
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_012.tif")));
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_013.tif")));
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_014.tif")));
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_015.tif")));
                m_causticTextures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_016.tif")));
            }

            if (m_submergeSoundFXDown == null)
            {
                m_submergeSoundFXDown = AssetDatabase.LoadAssetAtPath<AudioClip>(GaiaUtils.GetAssetPath("Gaia Ambient Submerge Down.mp3"));
            }

            if (m_submergeSoundFXUp == null)
            {
                m_submergeSoundFXUp = AssetDatabase.LoadAssetAtPath<AudioClip>(GaiaUtils.GetAssetPath("Gaia Ambient Submerge Up.mp3"));
            }

            if (m_underwaterSoundFX == null)
            {
                m_underwaterSoundFX = AssetDatabase.LoadAssetAtPath<AudioClip>(GaiaUtils.GetAssetPath("Gaia Ambient Underwater Sound Effect.mp3"));
            }

            if (m_fogColorGradient == null)
            {
                m_fogColorGradient = CreateGradient();
            }
#endif
        }

        /// <summary>
        /// Gets the underwater status
        /// </summary>
        /// <returns></returns>
        public static bool IsUnderWater()
        {
            bool underwater = false;
            GaiaUnderwaterEffects gaiaUnderwaterEffects = FindObjectOfType<GaiaUnderwaterEffects>();
            if (gaiaUnderwaterEffects != null)
            {
                underwater = gaiaUnderwaterEffects.IsUnderwater;
            }

            return underwater;
        }

        /// <summary>
        /// Creates a default Gradient
        /// </summary>
        /// <returns></returns>
        private Gradient CreateGradient()
        {
            Gradient gradient = new Gradient();

            // Populate the color keys at the relative time 0 and 1 (0 and 100%)
            GradientColorKey[] colorKey = new GradientColorKey[3];
            colorKey[0].color = GaiaUtils.GetColorFromHTML("0C233A");
            colorKey[0].time = 0.0f;
            colorKey[1].color = GaiaUtils.GetColorFromHTML("5686BC");
            colorKey[1].time = 0.5f;
            colorKey[2].color = GaiaUtils.GetColorFromHTML("5C9BE0");
            colorKey[2].time = 1f;

            // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
            GradientAlphaKey[] alphaKey = new GradientAlphaKey[3];
            alphaKey[0].alpha = 1.0f;
            alphaKey[0].time = 0.0f;
            alphaKey[1].alpha = 1.0f;
            alphaKey[1].time = 0.5f;
            alphaKey[2].alpha = 1.0f;
            alphaKey[2].time = 1.0f;

            gradient.SetKeys(colorKey, alphaKey);

            return gradient;
        }
    }
}