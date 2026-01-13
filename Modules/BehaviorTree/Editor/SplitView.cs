using UnityEngine.UIElements;

[UxmlElement]
public partial class SplitView : TwoPaneSplitView
{
    // public new class UxmlFactory : UxmlFactory<SplitView, UxmlTraits> {}

    public SplitView() : base(0, 300, TwoPaneSplitViewOrientation.Horizontal)  {

    }
}
