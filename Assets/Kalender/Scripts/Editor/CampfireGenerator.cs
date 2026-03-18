using UnityEngine;
using UnityEditor;

public class CampfireGenerator
{
    [MenuItem("GameObject/Effects/Detailed Campfire", false, 10)]
    public static void CreateCampfire(MenuCommand menuCommand)
    {
        // 1. Ana Obje
        GameObject campfireObj = new GameObject("Campfire");
        GameObjectUtility.SetParentAndAlign(campfireObj, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(campfireObj, "Create Campfire");

        // Işık
        Light light = campfireObj.AddComponent<Light>();
        light.type = LightType.Point;
        ColorUtility.TryParseHtmlString("#FF8A00", out Color c);
        light.color = c;
        light.range = 7f;
        light.intensity = 2f;

        // Default Particle Material bulalım
        Material defaultParticleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");

        // 2. Ana Ateş Particle System
        GameObject fireObj = new GameObject("FireParticles");
        fireObj.transform.SetParent(campfireObj.transform);
        fireObj.transform.localPosition = Vector3.zero;
        ParticleSystem firePS = fireObj.AddComponent<ParticleSystem>();
        
        var fireMain = firePS.main;
        fireMain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        fireMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        fireMain.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        fireMain.startColor = new Color(1f, 0.6f, 0.1f, 1f); 

        var fireEmission = firePS.emission;
        fireEmission.rateOverTime = 25f;

        var fireShape = firePS.shape;
        fireShape.shapeType = ParticleSystemShapeType.Cone;
        fireShape.angle = 15f;
        fireShape.radius = 0.2f;

        var fireSize = firePS.sizeOverLifetime;
        fireSize.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        fireSize.size = new ParticleSystem.MinMaxCurve(1f, curve);

        ParticleSystemRenderer fireRenderer = fireObj.GetComponent<ParticleSystemRenderer>();
        if(defaultParticleMat != null) fireRenderer.sharedMaterial = defaultParticleMat;

        // 3. Kıvılcımlar (Sparks)
        GameObject sparksObj = new GameObject("Sparks");
        sparksObj.transform.SetParent(campfireObj.transform);
        sparksObj.transform.localPosition = Vector3.up * 0.2f;
        ParticleSystem sparksPS = sparksObj.AddComponent<ParticleSystem>();

        var sparksMain = sparksPS.main;
        sparksMain.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
        sparksMain.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        sparksMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        sparksMain.startColor = new Color(1f, 0.9f, 0.4f, 1f);

        var sparksEmission = sparksPS.emission;
        sparksEmission.rateOverTime = 10f;

        var sparksShape = sparksPS.shape;
        sparksShape.shapeType = ParticleSystemShapeType.Cone;
        sparksShape.angle = 25f;
        sparksShape.radius = 0.3f;

        var sparksNoise = sparksPS.noise;
        sparksNoise.enabled = true;
        sparksNoise.strength = 0.5f;
        sparksNoise.frequency = 1f;

        ParticleSystemRenderer sparksRenderer = sparksObj.GetComponent<ParticleSystemRenderer>();
        if(defaultParticleMat != null) sparksRenderer.sharedMaterial = defaultParticleMat;

        // 4. Duman (Smoke)
        GameObject smokeObj = new GameObject("Smoke");
        smokeObj.transform.SetParent(campfireObj.transform);
        smokeObj.transform.localPosition = Vector3.up * 0.5f;
        ParticleSystem smokePS = smokeObj.AddComponent<ParticleSystem>();

        var smokeMain = smokePS.main;
        smokeMain.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
        smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
        smokeMain.startSize = new ParticleSystem.MinMaxCurve(1f, 3f);
        smokeMain.startColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);

        var smokeEmission = smokePS.emission;
        smokeEmission.rateOverTime = 12f;

        var smokeShape = smokePS.shape;
        smokeShape.shapeType = ParticleSystemShapeType.Cone;
        smokeShape.angle = 10f;
        smokeShape.radius = 0.4f;

        var smokeSize = smokePS.sizeOverLifetime;
        smokeSize.enabled = true;
        AnimationCurve smokeCurve = new AnimationCurve();
        smokeCurve.AddKey(0f, 0.5f);
        smokeCurve.AddKey(1f, 2f);
        smokeSize.size = new ParticleSystem.MinMaxCurve(1f, smokeCurve);

        ParticleSystemRenderer smokeRenderer = smokeObj.GetComponent<ParticleSystemRenderer>();
        if(defaultParticleMat != null) smokeRenderer.sharedMaterial = defaultParticleMat;

        Selection.activeObject = campfireObj;
    }
}
