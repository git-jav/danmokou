﻿using System;
using BagoumLib.Events;

namespace Danmokou.UI.XML {
/// <summary>
/// A data source for the configuration of a <see cref="EmptyNode"/> created within a freeform <see cref="IFixedXMLObjectContainer"/>.
/// </summary>
public interface IFixedXMLObject {
    string Descriptor { get; }
    ICObservable<float> Left { get; }
    ICObservable<float> Top { get; }
    ICObservable<float> Width { get; }
    ICObservable<float> Height { get; }
    ICObservable<bool> IsVisible { get; }
    UIResult? Navigate(UINode n, UICommand c);
}

/// <summary>
/// A freeform UITK menu that contains dynamically added/removed nodes.
/// </summary>
public interface IFixedXMLObjectContainer {
    void AddNode(UINode n);
} 

public record FixedXMLObject(float l, float t, float w = 100, float h = 100) : IFixedXMLObject {
    public string Descriptor { get; init; } = "";
    ICObservable<float> IFixedXMLObject.Left => Left;
    public Evented<float> Left { get; } = new(l);
    ICObservable<float> IFixedXMLObject.Top => Top;
    public Evented<float> Top { get; } = new(t);
    ICObservable<float> IFixedXMLObject.Width => Width;
    public Evented<float> Width { get; } = new(w);
    ICObservable<float> IFixedXMLObject.Height => Height;
    public Evented<float> Height { get; } = new(h);
    public Evented<bool> IsVisible { get; } = new(true);
    ICObservable<bool> IFixedXMLObject.IsVisible => IsVisible;
    public Func<UINode, UIResult?>? OnConfirm { get; init; }
    public UIResult? Navigate(UINode n, UICommand c) => 
        c is UICommand.Confirm ? OnConfirm?.Invoke(n) : null;
}
}