using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting; // Added for Visual Scripting support

[IncludeInSettings(true)] // Forces this script to be accepted by Visual Scripting
public class DayControl : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("Duration of a full day in seconds")]
    [SerializeField] private float dayDuration = 600f; // Increased from 120f to 600f (10 minutes)
    
    [Tooltip("Current time of day (0.0 = Midnight, 0.25 = Sunrise, 0.5 = Noon, 0.75 = Sunset)")]
    [Range(0f, 1f)]
    [SerializeField] private float timeOfDay = 0.25f;

    [Tooltip("If true, time advances automatically based on Day Duration.")]
    [SerializeField] private bool autoTimeProgression = false;

    /// <summary>
    /// Public property for Visual Scripting access.
    /// Gets or sets the current time of day (0.0 to 1.0).
    /// </summary>
    public float TimeOfDay
    {
        get { return timeOfDay; }
        set { SetTime(value); }
    }

    [Header("Sun Settings")]
    [SerializeField] private Light sunLight;
    [SerializeField] private float maxIntensity = 1f;
    [SerializeField] private Gradient sunColor;
    [SerializeField] private AnimationCurve sunIntensityCurve;

    [Header("Ambient Settings")]
    [SerializeField] private Gradient ambientColor;
    [SerializeField] private Color dayFogColor = new Color(0.5f, 0.6f, 0.7f, 1f);
    [SerializeField] private Color nightFogColor = new Color(0.02f, 0.02f, 0.05f, 1f);

    [Header("Skybox Settings")]
    [SerializeField] private List<SkyboxTimeMapping> skyboxCycle;
    
    [Header("Polyverse Blending Properties")]
    [SerializeField] private string[] floatProperties = new string[] { 
        "_Exposure", "_SunIntensity", "_SunSize", "_StarsIntensity", "_StarsSize", 
        "_CloudsIntensity", "_CloudsHeight", "_CloudHeight", 
        "_FogIntensity", "_FogHeight", "_FogSmoothness", 
        "_EquatorHeight", "_StarsRotationSpeed", "_CloudHeight",
        "_MoonIntensity", "_MoonSize" // Added Moon float props
    };
    [SerializeField] private string[] colorProperties = new string[] { 
        "_SkyColor", "_EquatorColor", "_GroundColor", "_SunColor", "_Tint", 
        "_CloudLightColor", "_CloudShadowColor", 
        "_CloudsLightColor", "_CloudsShadowColor", 
        "_FogColor", "_MoonColor" // Added Moon color prop
    };

    [ContextMenu("Reset To Polyverse Defaults")]
    public void ResetToPolyverseDefaults()
    {
        floatProperties = new string[] { 
            "_Exposure", "_SunIntensity", "_SunSize", "_StarsIntensity", "_StarsSize", 
            "_CloudsIntensity", "_CloudsHeight", "_CloudHeight", 
            "_FogIntensity", "_FogHeight", "_FogSmoothness", 
            "_EquatorHeight", "_StarsRotationSpeed", "_CloudHeight",
            "_MoonIntensity", "_MoonSize"
        };
        colorProperties = new string[] { 
             "_SkyColor", "_EquatorColor", "_GroundColor", "_SunColor", "_Tint", 
            "_CloudLightColor", "_CloudShadowColor", 
            "_CloudsLightColor", "_CloudsShadowColor", 
            "_FogColor", "_MoonColor"
        };
        
        // Reset Fog Colors to a reasonable default
        dayFogColor = new Color(0.5f, 0.6f, 0.7f, 1f); // Bluish Gray
        nightFogColor = new Color(0.02f, 0.02f, 0.05f, 1f); // Deep Blue Black
        
        Debug.Log("DayControl: Reset properties.");
    }

    private Material runtimeSkyboxMaterial;

    [System.Serializable]
    public struct SkyboxTimeMapping
    {
        public string phaseName;
        [Range(0f, 1f)]
        public float time;
        public Material skyboxMaterial; 
    }

    private void Start()
    {
        // Auto-find Directional Light if not assigned
        if (sunLight == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    sunLight = l;
                    break;
                }
            }
        }
        
        // Setup default curves if empty to ensure it works out of the box
        if (sunIntensityCurve == null || sunIntensityCurve.length == 0)
        {
             sunIntensityCurve = new AnimationCurve(
                 new Keyframe(0f, 0f), 
                 new Keyframe(0.2f, 0f), 
                 new Keyframe(0.25f, 1f), 
                 new Keyframe(0.75f, 1f), 
                 new Keyframe(0.8f, 0f), 
                 new Keyframe(1f, 0f)
             );
        }

        // Sort skybox cycle by time to ensure consistency
        if (skyboxCycle != null)
        {
            skyboxCycle.Sort((a, b) => a.time.CompareTo(b.time));
        }

        // Initialize Runtime Material from the first skybox in the cycle
        if (skyboxCycle != null && skyboxCycle.Count > 0 && skyboxCycle[0].skyboxMaterial != null)
        {
            // Create a copy of the first material to serve as the runtime skybox
            // We use the first one so it has the correct shader and initial texture references
            runtimeSkyboxMaterial = new Material(skyboxCycle[0].skyboxMaterial);
            runtimeSkyboxMaterial.name = "Runtime Skybox (DayControl)";
            
            // Critical: Ensure Moon is enabled on the runtime material, 
            // because the Day material (the source) might have it disabled.
            runtimeSkyboxMaterial.SetFloat("_EnableMoon", 1f);
            runtimeSkyboxMaterial.EnableKeyword("_ENABLEMOON_ON");
            runtimeSkyboxMaterial.EnableKeyword("_ENABLESUNMOON_ON");

            RenderSettings.skybox = runtimeSkyboxMaterial;
        }
    }

    private void Update()
    {
        UpdateTime();
        UpdateSun();
        UpdateLighting();
    }

    private void UpdateTime()
    {
        if (autoTimeProgression)
        {
            timeOfDay += Time.deltaTime / dayDuration;
            if (timeOfDay >= 1f)
            {
                timeOfDay -= 1f;
            }
        }
    }

    private void UpdateSun()
    {
        if (sunLight == null) return;

        // Rotation
        // Map 0..1 to -90..270
        float sunAngle = (timeOfDay * 360f) - 90f;
        
        // Rotate around X axis. Adjust Y axis as needed for your scene's north/south preference.
        sunLight.transform.localRotation = Quaternion.Euler(sunAngle, 170f, 0f); // 170 Y gives a slight angle, standard is often good.

        // Update Global Vectors for Polyverse Skies
        // Polyverse uses "GlobalSunDirection" and "GlobalMoonDirection"
        Shader.SetGlobalVector("GlobalSunDirection", -sunLight.transform.forward);
        Shader.SetGlobalVector("GlobalMoonDirection", sunLight.transform.forward); // Moon is opposite to Sun

        // Intensity
        if (sunIntensityCurve != null)
        {
            sunLight.intensity = sunIntensityCurve.Evaluate(timeOfDay) * maxIntensity;
        }

        // Color
        if (sunColor != null)
        {
            // If sunColor gradient is not setup, don't change color, or use default white
            // Actually basic check:
            sunLight.color = sunColor.Evaluate(timeOfDay); 
        }
    }

    private void UpdateLighting()
    {
        // Optional: Update Ambient Light
        if (ambientColor != null)
        {
            RenderSettings.ambientLight = ambientColor.Evaluate(timeOfDay);
        }

        // Blend Fog Color
        // We use the sun intensity curve to determine if it is day or night
        // If curve is missing, we approximate based on time (0.25 to 0.75 is day)
        float blendFactor = 0f;
        if (sunIntensityCurve != null && sunIntensityCurve.length > 0)
        {
             blendFactor = sunIntensityCurve.Evaluate(timeOfDay);
        }
        else
        {
            // Simple fallback: 0.0 at midnight, 1.0 at noon
            // 0.0->0.25 (Night->Day), 0.75->1.0 (Day->Night)
            float t = timeOfDay;
            if (t < 0.25f) blendFactor = t * 4f; // 0 to 1
            else if (t < 0.75f) blendFactor = 1f;
            else blendFactor = 1f - ((t - 0.75f) * 4f); // 1 to 0
        }

        RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, blendFactor);

        // Update Skybox Blending
        if (skyboxCycle != null && skyboxCycle.Count > 0 && runtimeSkyboxMaterial != null)
        {
            // Find current phase and next phase
            SkyboxTimeMapping phase1 = skyboxCycle[skyboxCycle.Count - 1]; // Default to last (wrap around case)
            SkyboxTimeMapping phase2 = skyboxCycle[0];
            
            for (int i = 0; i < skyboxCycle.Count; i++)
            {
                if (timeOfDay >= skyboxCycle[i].time)
                {
                    phase1 = skyboxCycle[i];
                    int nextIndex = (i + 1) % skyboxCycle.Count;
                    phase2 = skyboxCycle[nextIndex];
                }
            }

            // Calculate blend factor
            float startTime = phase1.time;
            float endTime = phase2.time;
            
            // Handle wrap around (e.g. 0.9 -> 0.1)
            float currentTime = timeOfDay;
            if (endTime < startTime)
            {
                endTime += 1f;
                if (currentTime < startTime) 
                {
                    currentTime += 1f;
                }
            }

            float duration = endTime - startTime;
            float blend = 0f;
            if (duration > 0.0001f)
            {
                blend = (currentTime - startTime) / duration;
            }

            // Blend Properties
            Material mat1 = phase1.skyboxMaterial;
            Material mat2 = phase2.skyboxMaterial;

            if (mat1 != null && mat2 != null)
            {
                // Blend Float Properties
                foreach (string prop in floatProperties)
                {
                    if (mat1.HasProperty(prop) && mat2.HasProperty(prop))
                    {
                        float val1 = mat1.GetFloat(prop);
                        float val2 = mat2.GetFloat(prop);
                        runtimeSkyboxMaterial.SetFloat(prop, Mathf.Lerp(val1, val2, blend));
                    }
                }

                // Blend Color Properties
                foreach (string prop in colorProperties)
                {
                    if (mat1.HasProperty(prop) && mat2.HasProperty(prop))
                    {
                        Color col1 = mat1.GetColor(prop);
                        Color col2 = mat2.GetColor(prop);
                        runtimeSkyboxMaterial.SetColor(prop, Color.Lerp(col1, col2, blend));
                    }
                }
                
                DynamicGI.UpdateEnvironment(); // Necessary for GI to pick up the changes
            }
        }
    }

    // Helper for editor use
    public void SetTime(float time)
    {
        timeOfDay = Mathf.Clamp01(time);
        UpdateSun();
        UpdateLighting();
    }
}
