﻿using System.Collections;
using Danmokou.Behavior;
using Danmokou.Core;
using Danmokou.DMath;
using Danmokou.Graphics;
using UnityEngine;

namespace Danmokou.Services {

public interface IShaderCamera {
    void AddXRotation(float dx, float t);
    void AddYRotation(float dy, float t);
    void ShowBlackHole(BlackHoleEffect bhe);
}

public readonly struct BlackHoleEffect {
    public readonly float absorbT;
    public readonly float hideT;
    public readonly float fadeBackT;
    public BlackHoleEffect(float absorbT, float hideT, float fadeBackT) {
        this.absorbT = absorbT;
        this.hideT = hideT;
        this.fadeBackT = fadeBackT;
    }
}
public class SeijaCamera : CoroutineRegularUpdater, IShaderCamera {
    private static readonly int rotX = Shader.PropertyToID("_RotateX");
    private static readonly int rotY = Shader.PropertyToID("_RotateY");
    private static readonly int rotZ = Shader.PropertyToID("_RotateZ");
    private static readonly int xBound = Shader.PropertyToID("_XBound");
    private static readonly int yBound = Shader.PropertyToID("_YBound");
    private static readonly int blackHoleT = Shader.PropertyToID("_BlackHoleT");


    private Camera cam = null!;

    private float targetXRot = 0f;
    private float targetYRot = 0f;
    private float sourceXRot = 0f;
    private float sourceYRot = 0f;
    private float timeToTarget = 0f;
    private float timeElapsedToTarget = 0f;

    private float lastXRot = 0f;
    private float lastYRot = 0f;


    public Shader seijaShader = null!;
    public Material seijaMaterial = null!;

    private void Awake() {
        cam = GetComponent<Camera>();
        seijaMaterial = new Material(seijaShader);
        seijaMaterial.SetFloat(xBound, GameManagement.References.bounds.right + 1);
        seijaMaterial.SetFloat(yBound, GameManagement.References.bounds.top + 1);
        SetLocation(0, 0);
    }

    protected override void BindListeners() {
        base.BindListeners();
        RegisterDI<IShaderCamera>(this);
        Listen(Events.ClearPhase, () => ResetTargetFlip(1f));
#if UNITY_EDITOR || ALLOW_RELOAD
        Listen(Events.LocalReset, () => ResetTargetFlip(0.2f));
#endif
    }

    public override void RegularUpdate() {
        base.RegularUpdate();
        if (timeElapsedToTarget >= timeToTarget) {
            targetXRot = lastXRot = M.Mod(360, targetXRot);
            targetYRot = lastYRot = M.Mod(360, targetYRot);
            SetLocation(targetXRot, targetYRot);
        } else {
            lastXRot = Mathf.Lerp(sourceXRot, targetXRot, timeElapsedToTarget / timeToTarget);
            lastYRot = Mathf.Lerp(sourceYRot, targetYRot, timeElapsedToTarget / timeToTarget);
            SetLocation(lastXRot, lastYRot);
        }
        timeElapsedToTarget += ETime.FRAME_TIME;
    }

    private void SendLastToSource(float time) {
        timeToTarget = time;
        timeElapsedToTarget = 0f;
        sourceXRot = lastXRot;
        sourceYRot = lastYRot;
    }

    public void AddXRotation(float dx, float time) {
        SendLastToSource(time);
        targetXRot += dx;
    }

    public void AddYRotation(float dy, float time) {
        SendLastToSource(time);
        targetYRot += dy;
    }

    public void ResetTargetFlip(float time) {
        if (targetXRot * targetXRot + targetYRot * targetYRot > 0) {
            SendLastToSource(time);
            targetXRot = 360 * Mathf.Round(targetXRot / 360);
            targetYRot = 360 * Mathf.Round(targetYRot / 360);
        }
    }

    private void SetLocation(float xrd, float yrd) {
        seijaMaterial.SetFloat(rotX, xrd * M.degRad);
        seijaMaterial.SetFloat(rotY, yrd * M.degRad);
    }

    public void ShowBlackHole(BlackHoleEffect bhe) {
        RunRIEnumerator(BlackHole(bhe));
    }

    [ContextMenu("Black hole")]
    public void debugBlackHole() => ShowBlackHole(new BlackHoleEffect(5, 1, 2));
    private IEnumerator BlackHole(BlackHoleEffect bhe) {
        seijaMaterial.EnableKeyword("FT_BLACKHOLE");
        seijaMaterial.SetFloat("_BlackHoleAbsorbT", bhe.absorbT);
        seijaMaterial.SetFloat("_BlackHoleBlackT", bhe.hideT);
        seijaMaterial.SetFloat("_BlackHoleFadeT", bhe.fadeBackT);
        float t = 0;
        for (; t < bhe.absorbT + bhe.hideT + bhe.fadeBackT; t += ETime.FRAME_TIME) {
            seijaMaterial.SetFloat(blackHoleT, t);
            yield return null;
        }
        seijaMaterial.DisableKeyword("FT_BLACKHOLE");
    }

    private void OnPreRender() {
        cam.targetTexture = MainCamera.RenderTo;
    }
    private void OnRenderImage(RenderTexture src, RenderTexture dest) {
        //Dest is dirty, rendering to it directly can cause issues if there are alpha pixels.
        dest.GLClear();
        UnityEngine.Graphics.Blit(src, dest, seijaMaterial);
    }

}
}