﻿using System;
using BagoumLib.Culture;
using Danmokou.Core;
using Danmokou.Services;
using Suzunoya.Entities;
using SuzunoyaUnity.Derived;
using SuzunoyaUnity.Mimics;
using UnityEngine;

namespace Danmokou.VN {
public class Reimu : SZYUCharacter {
    public override Color TextColor => new(1f, 0.85f, 0.92f);
    public override Color UIColor => new(0.85f, 0.1f, 0.24f);
    public override LString Name { get; set; } = LocalizedStrings.FindReference("dialogue.reimu");

    public override void RollEvent() => ServiceLocator.SFXService.Request("x-bubble-3", SFXType.TypingSound);
}

public class ReimuMimic : PiecewiseCharacterMimic {
    public override Type[] CoreTypes => new[] {typeof(Reimu)};
}

}