using UnityEngine.UIElements;

[UxmlElement]
public partial class VerticalSplitView : TwoPaneSplitView
{
    // public new class UxmlFactory : UxmlFactory<SplitView, UxmlTraits> {}

    public VerticalSplitView() : base(0, 500, TwoPaneSplitViewOrientation.Vertical){

    }
}
