﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Danmaku {
/// <summary>
/// A component which combines multiple background images using a transition configuration
/// set by the BackgroundOrchestrator.
/// </summary>
public class BackgroundCombiner : RegularUpdater {
    private static BackgroundCombiner main;
    private int renderMask;
    private MaterialPropertyBlock pb;
    private SpriteRenderer sr;
    private Material mat;

    private float time = 0f;
    private void Awake() {
        main = this;
        renderMask = LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer));
        pb = new MaterialPropertyBlock();
        sr = GetComponent<SpriteRenderer>();
        sr.enabled = true;
        mat = sr.material;
        mat.EnableKeyword("MIX_FROM_ONLY");
        sr.SetPropertyBlock(pb);
    }

    private void Start() => UpdateTextures();

    public static void SetMaterial(Material newMat, MaterialPropertyBlock newPB) => main._SetMaterial(newMat, newPB);
    private void _SetMaterial(Material newMat, MaterialPropertyBlock newPB) {
        mat = newMat;
        pb = newPB;
        UpdateTextures();
        sr.material = mat;
        time = 0f;
    }

    private void UpdateTextures() {
        //Update PB
        var fromTex = BackgroundOrchestrator.FromBG.Capturer.Captured;
        var toTex = (BackgroundOrchestrator.ToBG == null) ? null : BackgroundOrchestrator.ToBG.Capturer.Captured;
        if (toTex == null) {
            mat.EnableKeyword("MIX_FROM_ONLY");
            pb.SetTexture(PropConsts.fromTex, fromTex);
        } else {
            mat.DisableKeyword("MIX_FROM_ONLY");
            pb.SetTexture(PropConsts.fromTex, fromTex);
            pb.SetTexture(PropConsts.toTex, toTex);
        }
        pb.SetFloat(PropConsts.time, time);
        sr.SetPropertyBlock(pb);
    }

    public override void RegularUpdate() {
        if (ETime.LastUpdateForScreen) UpdateTextures();
        time += ETime.FRAME_TIME;
    }
}
}