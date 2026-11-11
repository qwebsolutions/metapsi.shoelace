using Metapsi.Html;
using Metapsi.Hyperapp;
using Metapsi.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Metapsi.Shoelace;

public class ShoelaceBasePathDependency : IDependency, IDependencyDefaultImport
{
    public static readonly EmbeddedResource BasePathJs = EmbeddedResource.Versioned(
        typeof(Metapsi.Shoelace.SlNodeExtensions).Assembly,
        Cdn.Version,
        $"shoelace@{Cdn.Version}/utilities/base-path.js");

    public string DependencyId => "shoelace-base-path";

    public void Import(HtmlBuilder b)
    {
        var basePathJsModulePath = b.ResolvePath(BasePathJs);

        if (string.IsNullOrWhiteSpace(basePathJsModulePath))
        {
            throw new Exception("/utilities/base-path.js not found");
        }

        var basePathValue = basePathJsModulePath.Replace("utilities/base-path.js", string.Empty);

        b.HeadAppend(b.HtmlScriptModule(
            b =>
            {
                var setBasePath = b.ImportName<Action<string>>(basePathJsModulePath, "setBasePath");
                b.Call(setBasePath, b.Const(basePathValue));
            }));
    }

    public static readonly ShoelaceBasePathDependency Dependency = new ShoelaceBasePathDependency();
}

public class ShoelaceTagDependency : IDependency, IDependencyDefaultImport
{
    private readonly string tag;

    public ShoelaceTagDependency(string tag)
    {
        this.tag = tag;
    }

    public string DependencyId => "shoelace-tag-" + tag;

    public void Import(HtmlBuilder b)
    {
        string scriptLogicalName = $"shoelace@{Cdn.Version}/{Cdn.ImportPaths[tag]}";

        var resolvedTagJs = b.ResolvePath(EmbeddedResource.Versioned(
            typeof(Metapsi.Shoelace.SlNodeExtensions).Assembly,
            Cdn.Version,
            scriptLogicalName));

        b.HeadAppend(
            b.HtmlScript(
                b =>
                {
                    b.SetSrc(resolvedTagJs);
                    b.SetTypeModule();
                }));
    }
}

public static partial class SlNodeExtensions
{
    private static readonly EmbeddedStylesheetDependency LightTheme =
        new EmbeddedStylesheetDependency(
                EmbeddedResource.Versioned(
                    typeof(Metapsi.Shoelace.SlNodeExtensions).Assembly,
                    Cdn.Version,
                    $"shoelace@{Cdn.Version}/themes/light.css"));

    public static IHtmlNode SlTag<T>(
        this HtmlBuilder b,
        string tag,
        Action<AttributesBuilder<T>> setAttributes,
        List<IHtmlNode> children)
    {
        ImportShoelaceTag(b, tag);
        return b.Tag(tag, setAttributes, children);
    }

    public static IHtmlNode SlTag<T>(
        this HtmlBuilder b,
        string tag,
        Action<AttributesBuilder<T>> setAttributes,
        params IHtmlNode[] children)
    {
        ImportShoelaceTag(b, tag);
        return b.Tag(tag, setAttributes, children);
    }

    public static IHtmlNode SlTag(
        this HtmlBuilder b,
        string tag,
        Dictionary<string, string> attributes,
        List<IHtmlNode> children)
    {
        ImportShoelaceTag(b, tag);
        return b.Tag(tag, attributes, children);
    }

    public static IHtmlNode SlTag(
        this HtmlBuilder b,
        string tag,
        Dictionary<string, string> attributes,
        params IHtmlNode[] children)
    {
        ImportShoelaceTag(b, tag);
        return b.Tag(tag, attributes, children);
    }

    public static IHtmlNode SlTag<T>(
        this HtmlBuilder b,
        string tag,
        List<IHtmlNode> children)
    {
        ImportShoelaceTag(b, tag);
        return b.Tag(tag, children);
    }

    public static IHtmlNode SlTag<T>(
        this HtmlBuilder b,
        string tag,
        params IHtmlNode[] children)
    {
        ImportShoelaceTag(b, tag);
        return b.Tag(tag, children);
    }

    public static void ImportShoelaceTag(ICanRequireDependency b, string tag)
    {
        b.Require(LightTheme);
        b.Require(new ShoelaceTagDependency(tag));
        b.Require(ShoelaceBasePathDependency.Dependency);
        b.TrackWebComponent(tag);
    }

    public static Var<IVNode> SlNode<TProps>(
        this LayoutBuilder b,
        string tag,
        Action<PropsBuilder<TProps>> buildProps,
        Var<List<IVNode>> children)
        where TProps : new()
    {
        ImportShoelaceTag(b, tag);
        return b.H(tag, buildProps, children);
    }

    public static Var<IVNode> SlNode<TProps>(
        this LayoutBuilder b,
        string tag,
        Action<PropsBuilder<TProps>> buildProps,
        Var<IVNode>[] children)
        where TProps : new()
    {
        ImportShoelaceTag(b, tag);
        return b.H(tag, buildProps, children);
    }

    public static Var<IVNode> SlNode(
        this LayoutBuilder b,
        string tag,
        Var<List<IVNode>> children)
    {
        ImportShoelaceTag(b, tag);
        return b.H(tag, children);
    }

    public static Var<IVNode> SlNode(
        this LayoutBuilder b,
        string tag,
        Var<IVNode>[] children)
    {
        ImportShoelaceTag(b, tag);
        return b.H(tag, children);
    }

    internal static Var<HyperType.Effect> AwaitUpdateCompleteEffect<TState>(SyntaxBuilder b, Var<Html.Event> e, Var<HyperType.Action<TState, Html.Event>> action)
    {
        return b.MakeEffect(
            (b, dispatch) =>
            {
                var slControl = b.Get(e, x => x.target);
                var updateComplete = b.GetProperty<Promise>(slControl, b.Const("updateComplete"));
                b.PromiseThen<object>(updateComplete, (b, _) =>
                {
                    b.Dispatch(dispatch, action, e);
                });
            });
    }

    internal static Var<HyperType.Action<TState, Html.Event>> AwaitUpdateCompleteAction<TState>(ISyntaxBuilder b, Var<HyperType.Action<TState, Html.Event>> action)
    {
        return b.MakeAction((SyntaxBuilder b, Var<TState> model, Var<Html.Event> e) =>
        {
            return b.MakeStateWithEffects(model, b.Call(AwaitUpdateCompleteEffect, e, action));
        });
    }

    internal static void OnSlEvent<TState, T>(this PropsBuilder<T> b, string eventName, Var<HyperType.Action<TState, Html.Event>> action)
    {
        b.SetProperty(b.Props, b.Const(eventName), b.Call(AwaitUpdateCompleteAction, action));
    }

    internal static void OnSlEvent<TState,T>(this PropsBuilder<T> b, string eventName, Var<HyperType.Action<TState>> action)
    {
        b.SetProperty(b.Props, b.Const(eventName), b.Call(AwaitUpdateCompleteAction, action.As<HyperType.Action<TState, Html.Event>>()));
    }

    internal static void OnSlEvent<TState, T, TEventDetail>(this PropsBuilder<T> b, string eventName, Var<HyperType.Action<TState, Html.CustomEvent<TEventDetail>>> action)
    {
        b.SetProperty(b.Props, b.Const(eventName), b.Call(AwaitUpdateCompleteAction, action.As<HyperType.Action<TState, Html.Event>>()));
    }
}