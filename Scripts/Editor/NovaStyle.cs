using Nova;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class NovaStyle : OdinEditorWindow
{
    [MenuItem("Tools/Nova Style")]
    private static void OpenWindow()
    {
        GetWindow<NovaStyle>().Show();
    }

    [ShowInInspector] private static bool ApplyColor = true; 
    [ShowInInspector] private static bool ApplyLayout = false;
    [ShowInInspector] private static bool ApplyAutoLayout = false;
    [ShowInInspector] private static bool ApplyCornerRadius = true;
    [ShowInInspector] private static bool ApplyBorder = true;
    [ShowInInspector] private static bool ApplyShadow = true;
    [ShowInInspector] private static bool ApplyGradient = true;

    [ButtonGroup, GUIColor(0.5f, 0.1f, 0.5f)]
    [MenuItem("Tools/Copy Nova Style #&c")]
    private static void CopySelectedStyle()
    {
        UIBlock2D block = (Selection.activeObject as GameObject).GetComponent<UIBlock2D>();
        CopyStyle(instance = new NovaStyleData(), block);
        GetWindow<NovaStyle>().Show();
    }

    [ButtonGroup,GUIColor(0.5f,0.5f,1f)]
    [MenuItem("Tools/Paste Nova Style #&v")]
    private static void PasteSelectedStyle()
    {
        if (instance == null)
            return;

        UIBlock2D block = (Selection.activeObject as GameObject).GetComponent<UIBlock2D>();
        ApplyStyle(instance, block);
    }

    private static void CopyStyle(NovaStyleData data, UIBlock2D block)
    {
        if(data == null)
            return;

        if (block == null)
            return;

        data.bodyColor = block.Color;
        data.autoLayout = block.AutoLayout;
        data.layoutStyle = block.Layout;
        data.cornerRadius = block.CornerRadius;
        data.borderStyle = block.Border;
        data.shadowStyle = block.Shadow;
        data.gradientStyle = block.Gradient;
    }

    private static void ApplyStyle(NovaStyleData data, UIBlock2D block)
    {
        if (data == null)
            return;

        if (block == null)
            return;
        if (ApplyColor)
            block.Color = data.bodyColor;
        if (ApplyAutoLayout)
            block.AutoLayout = data.autoLayout;
        if (ApplyLayout)
            block.Layout = data.layoutStyle;
        if (ApplyCornerRadius)
            block.CornerRadius = data.cornerRadius;
        if (ApplyBorder)
            block.Border = data.borderStyle;
        if (ApplyShadow)
            block.Shadow = data.shadowStyle;
        if (ApplyGradient)
            block.Gradient = data.gradientStyle;
    }

#if UNITY_EDITOR
    private static NovaStyleData instance;
    [MenuItem("CONTEXT/UIBlock2D/Copy Style")]
    private static void Copy_Style(MenuCommand command)
    {
        instance = new NovaStyleData();
        UIBlock2D block = (UIBlock2D)command.context;
        CopyStyle(instance, block);
    }

    [MenuItem("CONTEXT/UIBlock2D/Paste Style")]
    private static void Paste_Style(MenuCommand command)
    {
        if (instance == null)
            return;

        ApplyStyle(instance, (UIBlock2D)command.context);
    }

    public class NovaStyleData
    {
        public Color bodyColor;

        public AutoLayout autoLayout;
        public Layout layoutStyle;
        public Length cornerRadius;
        public Border borderStyle;
        public Shadow shadowStyle;
        public RadialGradient gradientStyle;
    }

#endif
}
