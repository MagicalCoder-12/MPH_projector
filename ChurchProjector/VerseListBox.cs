namespace ChurchProjector;

public sealed class VerseListBox : ListBox
{
    private bool _dragSelecting;
    private int _dragAnchor = -1;

    public VerseListBox()
    {
        SelectionMode = SelectionMode.MultiExtended;
    }

    protected override void OnMouseDown(MouseEventArgs eventArgs)
    {
        base.OnMouseDown(eventArgs);
        if (eventArgs.Button != MouseButtons.Left || ModifierKeys != Keys.None) return;
        var index = IndexFromPoint(eventArgs.Location);
        if (index == NoMatches) return;
        _dragAnchor = index;
        _dragSelecting = true;
        Capture = true;
        ClearSelected();
        SetSelected(index, true);
    }

    protected override void OnMouseMove(MouseEventArgs eventArgs)
    {
        base.OnMouseMove(eventArgs);
        if (!_dragSelecting || _dragAnchor < 0 || eventArgs.Button != MouseButtons.Left) return;
        var index = IndexFromPoint(eventArgs.Location);
        if (index == NoMatches) return;
        var first = Math.Min(_dragAnchor, index);
        var last = Math.Max(_dragAnchor, index);
        BeginUpdate();
        ClearSelected();
        for (var itemIndex = first; itemIndex <= last; itemIndex++) SetSelected(itemIndex, true);
        EndUpdate();
    }

    protected override void OnMouseUp(MouseEventArgs eventArgs)
    {
        base.OnMouseUp(eventArgs);
        _dragSelecting = false;
        _dragAnchor = -1;
        Capture = false;
    }
}
