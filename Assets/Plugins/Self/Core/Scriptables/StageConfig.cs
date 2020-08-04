﻿using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

/// <summary>
/// Provides stage metadata.
/// </summary>
[CreateAssetMenu(menuName = "Data/Stage Configuration")]
public class StageConfig : ScriptableObject {
    public SceneConfig sceneConfig;
    [CanBeNull] public TextAsset stateMachine;
    public string description;
    public int stageNumber;
    public string defaultSuicideStyle;
}