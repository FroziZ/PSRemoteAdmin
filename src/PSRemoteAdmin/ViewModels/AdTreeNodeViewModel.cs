using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PSRemoteAdmin.Core.Models;

namespace PSRemoteAdmin.ViewModels;

public partial class AdTreeNodeViewModel : ObservableObject
{
    private readonly Action<AdTreeNodeViewModel> _onSelectionChanged;

    [ObservableProperty] private bool _isLoadingChildren;
    [ObservableProperty] private bool _isExpanded;

    private bool? _isSelected = false;
    private bool _updatingChildren;
    private WeakReference<AdTreeNodeViewModel>? _parent;

    public AdNode Node { get; }
    public string Name => Node.Name;
    public AdNodeType NodeType => Node.NodeType;
    public bool IsLeaf => Node.NodeType == AdNodeType.Computer;
    public ObservableCollection<AdTreeNodeViewModel> Children { get; } = new();

    // Dummy child used to show the expand arrow before lazy-load
    public bool HasDummyChild => Children.Count == 1 && Children[0].Node.Name == "__dummy__";

    public bool? IsSelected
    {
        get => _isSelected;
        set
        {
            if (_updatingChildren) return;
            SetIsSelectedInternal(value);
            _onSelectionChanged(this);
        }
    }

    public AdTreeNodeViewModel(AdNode node, Action<AdTreeNodeViewModel> onSelectionChanged,
        AdTreeNodeViewModel? parent = null)
    {
        Node = node;
        _onSelectionChanged = onSelectionChanged;
        _parent = parent != null ? new WeakReference<AdTreeNodeViewModel>(parent) : null;

        // Add a dummy child so the TreeViewItem shows the expand arrow
        if (node.NodeType == AdNodeType.OrganizationalUnit && node.HasChildren)
        {
            Children.Add(new AdTreeNodeViewModel(
                new AdNode { Name = "__dummy__", DistinguishedName = "", NodeType = AdNodeType.OrganizationalUnit },
                _ => { }));
        }
    }

    public void SetChildren(IEnumerable<AdTreeNodeViewModel> children)
    {
        Children.Clear();
        foreach (var child in children)
        {
            child._parent = new WeakReference<AdTreeNodeViewModel>(this);
            Children.Add(child);
        }
        // After loading children, cascade current selection state down
        if (_isSelected == true)
            CascadeToChildren(true);
        else if (_isSelected == false)
            CascadeToChildren(false);
    }

    private void SetIsSelectedInternal(bool? value)
    {
        if (_isSelected == value) return;
        _updatingChildren = true;
        _isSelected = value;
        OnPropertyChanged(nameof(IsSelected));

        // Cascade definite values (true/false) down to children
        if (value.HasValue)
            CascadeToChildren(value.Value);

        _updatingChildren = false;

        // Bubble upward
        if (_parent?.TryGetTarget(out var parent) == true)
            parent.RecalculateFromChildren();
    }

    private void CascadeToChildren(bool value)
    {
        foreach (var child in Children)
        {
            if (child.Node.Name == "__dummy__") continue;
            child._updatingChildren = true;
            child._isSelected = value;
            child.OnPropertyChanged(nameof(IsSelected));
            child.CascadeToChildren(value);
            child._updatingChildren = false;
        }
    }

    internal void RecalculateFromChildren()
    {
        if (Children.Count == 0 || HasDummyChild) return;

        int trueCount = Children.Count(c => c._isSelected == true);
        int falseCount = Children.Count(c => c._isSelected == false);

        bool? newValue;
        if (trueCount == Children.Count) newValue = true;
        else if (falseCount == Children.Count) newValue = false;
        else newValue = null; // indeterminate

        if (newValue == _isSelected) return;
        _isSelected = newValue;
        OnPropertyChanged(nameof(IsSelected));
        _onSelectionChanged(this);

        if (_parent?.TryGetTarget(out var parent) == true)
            parent.RecalculateFromChildren();
    }
}
