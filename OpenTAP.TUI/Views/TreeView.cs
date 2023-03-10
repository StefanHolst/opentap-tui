using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace OpenTap.Tui
{
    public class TreeView<T> : ListView
    {
        private Func<T, string> getTitle;
        private Func<T, List<string>> getGroups;
        private Func<T, List<T>> getChildren;
        private Func<T, T> getParent;
        private Func<T, TreeViewNode<T>> createNode;
        private Func<TreeViewNode<T>, string, TreeViewNode<T>> createGroupNode;
        private IList<T> items;
        private Dictionary<T, TreeViewNode<T>> nodes;
        private Dictionary<string, TreeViewNode<T>> groups = new Dictionary<string, TreeViewNode<T>>();
        private List<TreeViewNode<T>> renderedItems;
        public bool EnableFilter { get; set; }
        public string Filter { get; set; } = "";
        public Action<string> FilterChanged { get; set; }
        internal event Action<TreeViewNode<T>, bool> NodeVisibilityChanged;

        public bool IsGhostNodeChild { get; private set; }
        public List<string> GhostNodes { get; } = new List<string>();
        
        public T SelectedObject
        {
            get => renderedItems == null ? default : SelectedItem < renderedItems.Count ? renderedItems[SelectedItem].Item : default;
            set => SelectedItem = renderedItems.IndexOf(nodes[value]);
        }

        public TreeView(Func<T, string> getTitle, Func<T, List<string>> getGroups, Func<T, TreeViewNode<T>> createNode = null, Func<TreeViewNode<T>, string, TreeViewNode<T>> createGroupNode = null)
        {
            this.getTitle = getTitle;
            this.getGroups = getGroups;
            this.createNode = createNode;
            this.createGroupNode = createGroupNode;
        }
        public TreeView(Func<T, string> getTitle, Func<T, List<T>> getChildren, Func<T, T> getParent, Func<T, TreeViewNode<T>> createNode)
        {
            this.getTitle = getTitle;
            this.getChildren = getChildren;
            this.getParent = getParent;
            this.createNode = createNode;
        }

        public TreeViewNode<T> GetNodeFromItem(T item)
        {
            TreeViewNode<T> node;
            if (nodes.TryGetValue(item, out node) == false)
            {
                node = createNode?.Invoke(item) ?? new TreeViewNode<T>(item, this);
                nodes[item] = node;
            }
            node.Title = getTitle(item);
            node.Children = getChildren?.Invoke(item).Select(i => new TreeViewNode<T>(i, this)).ToList() ?? new List<TreeViewNode<T>>();

            if (getParent != null)
            {
                var parent = getParent(item);
                if (parent != null && nodes.ContainsKey(parent))
                    node.Parent = nodes[parent];
                else
                    node.Parent = null;
            }

            return node;
        }

        private void BuildGroupTree(TreeViewNode<T> node)
        {
            TreeViewNode<T> lastGroup = null;
            foreach (var group in node.Groups)
            {
                TreeViewNode<T> groupNode = null;
                if (groups.TryGetValue(group, out groupNode) == false)
                {
                    // add the group
                    groupNode = createGroupNode?.Invoke(node, group) ?? new TreeViewNode<T>(default(T), this)
                    {
                        Title = group,
                    };
                    groupNode.IsGroup = true;
                    groups[group] = groupNode;
                }
                    
                if (lastGroup != null)
                {
                    groupNode.Parent = lastGroup;
                    if (lastGroup.Children.Contains(groupNode) == false)
                        lastGroup.Children.Add(groupNode);
                }
                lastGroup = groupNode;
            }

            if (lastGroup != null)
            {
                node.Parent = lastGroup;
                if (lastGroup.Children.Contains(node) == false)
                    lastGroup.Children.Add(node);
            }
        }

        public void ExpandObject(T item)
        {
            nodes[item].IsExpanded = true;
            RenderTreeView();
        }
        
        public void SetTreeViewSource(IList<T> items)
        {
            this.items = items;
            nodes = new Dictionary<T, TreeViewNode<T>>();
            groups = new Dictionary<string, TreeViewNode<T>>();
            RenderTreeView();
        }
        
        List<TreeViewNode<T>> GetItemsToRenderWithGroup(bool noCache)
        {
            // Build groups tree
            foreach (var item in items)
            {
                TreeViewNode<T> node;
                bool existed = nodes.TryGetValue(item, out node);
                if (existed == false || noCache)
                {
                    var _groups = getGroups(item);
                    node = GetNodeFromItem(item);
                    node.Groups = _groups ?? new List<string>();
                }
                BuildGroupTree(node);
            }

            List<TreeViewNode<T>> printGroup(TreeViewNode<T> groupNode)
            {
                var _list = new List<TreeViewNode<T>>();

                foreach (var node in groupNode.Children)
                {
                    if (node.IsVisible == false && Filter?.Length > 0 != true)
                        continue;
                    
                    _list.Add(node);
                    
                    if ((node.Children.Any() && node.IsExpanded) || Filter?.Length > 0)
                        _list.AddRange(printGroup(node));
                }

                return _list;
            }

            // print nodes and groups
            var list = new List<TreeViewNode<T>>();
            foreach (var item in items)
            {
                var node = nodes[item];
                if (node.Groups.Any())
                {
                    var firstGroup = groups[node.Groups.First()];
                    if (list.Contains(firstGroup))
                        continue;

                    list.Add(firstGroup);
                    list.AddRange(printGroup(firstGroup));
                }
                else
                    list.Add(node);
            }
            
            return list;
        }
        List<TreeViewNode<T>> GetItemsToRender(T item, bool noCache)
        {
            var list = new List<TreeViewNode<T>>();
            TreeViewNode<T> node;
            bool existed = nodes.TryGetValue(item, out node);
            if (existed == false || noCache)
                node = GetNodeFromItem(item);

            list.Add(node);
            if (node.IsExpanded || Filter?.Length > 0)
            {
                foreach (var child in node.Children)
                    list.AddRange(GetItemsToRender(child.Item, noCache));
            }

            return list;
        }

        private List<string> listSource;

        public void RenderTreeView(bool noCache = false)
        {
            if (items == null)
                return;

            // Render all items
            var list = new List<TreeViewNode<T>>();
            if (getGroups != null)
                list = GetItemsToRenderWithGroup(noCache);
            else
            {
                foreach (var item in items)
                    list.AddRange(GetItemsToRender(item, noCache));
            }

            // Filter the list
            if (string.IsNullOrEmpty(Filter) == false)
                renderedItems = list.Where(i => i.Title.ToLower().Contains(Filter.ToLower())).ToList();
            else
                renderedItems = list;
            
            // Save old selected indexes to keep layout
            var index = SelectedItem;
            int newIndex;
            var oldTop = TopItem;
            int newTop;
            listSource = renderedItems.Select(n => n.ToString()).ToList();


            // check if the saved selected index can still be used
            if (index >= listSource.Count)
                newIndex = listSource.Count - 1;
            else
                newIndex = index;

            // check if the saved top index can still be used
            if (listSource.Count == 0)
                newTop = 0;
            else if (oldTop > 0 && oldTop >= listSource.Count)
                newTop = listSource.Count - 1;
            else
                newTop = oldTop;

            if (GhostNodes.Any() && renderedItems.Any())
            {
                int insertIndex = SelectedItem + 1;
                string indent = new string(' ', renderedItems[newIndex].Indent + (IsGhostNodeChild ? 3 : 2));
                if (insertIndex >= listSource.Count)
                {
                    listSource.AddRange(GhostNodes.Select(g => indent + g));
                }
                else
                {
                    listSource.InsertRange(insertIndex, GhostNodes.Select(g => indent + g));
                }
            }
            SetSource(listSource);

            SelectedItem = newIndex;
            TopItem = newTop;

            OnSelectedChanged();
        }

        // Redraw is re-implemented here to draw the ghostnode in a seperate color to the rest of the nodes.
        public override void Redraw(Rect bounds)
        {
            var current = ColorScheme.Focus;
            Driver.SetAttribute(current);
            Move(0, 0);
            var item = top;
            int start = Bounds.Left;

            for (int row = 0; row < Frame.Height; row++, item++)
            {
                bool isGhostNode = row > selected && row <= selected + GhostNodes.Count;
                bool isSelected = item == selected;


                Terminal.Gui.Attribute newcolor;
                if (isSelected)
                {
                    newcolor = ColorScheme.Focus;
                }
                else if (isGhostNode)
                {
                    newcolor = ColorScheme.HotNormal;
                }
                else
                { 
                    newcolor = GetNormalColor(); 
                }

                if (newcolor != current)
                {
                    Driver.SetAttribute(newcolor);
                    current = newcolor;
                }

                Move(0, row);
                if (listSource == null || item >= listSource.Count)
                {
                    for (int c = 0; c < Frame.Width; c++)
                        Driver.AddRune(' ');
                }
                else
                {
                    var rowEventArgs = new ListViewRowEventArgs(item);
                    OnRowRender(rowEventArgs);
                    if (rowEventArgs.RowAttribute != null && current != rowEventArgs.RowAttribute)
                    {
                        current = (Terminal.Gui.Attribute)rowEventArgs.RowAttribute;
                        Driver.SetAttribute(current);
                    }
                    Source.Render(this, Driver, isSelected, item, 0, row, Frame.Width, start);
                }
            }
        }

        public override bool ProcessKey(KeyEvent kb)
        {

            if (renderedItems != null && renderedItems.Any() && (kb.Key == Key.Enter || kb.Key == Key.CursorRight || kb.Key == Key.CursorLeft || kb.Key == Key.CursorUp || kb.Key == Key.CursorDown))
            {
                var selectedNode = renderedItems[SelectedItem];
                var hasChildren = selectedNode.AlwaysDisplayExpandState || selectedNode.Children.Any();

                if (kb.Key == Key.CursorUp || kb.Key == Key.CursorDown)
                {
                    IEnumerable<string> ghostNode = GhostNodes.ToList();
                    GhostNodes.Clear();
                    RenderTreeView(true);
                    GhostNodes.AddRange(ghostNode);
                    if (kb.Key == Key.CursorUp)
                        MoveUp();
                    else if (kb.Key == Key.CursorDown)
                        MoveDown();

                    selectedNode = renderedItems[SelectedItem];
                    hasChildren = selectedNode.AlwaysDisplayExpandState || selectedNode.Children.Any();

                    IsGhostNodeChild = hasChildren && selectedNode.IsExpanded;
                    RenderTreeView(true);
                    return true;
                }

                if (hasChildren && kb.Key == Key.CursorLeft && selectedNode.IsExpanded)
                {
                    selectedNode.IsExpanded = false;
                    IsGhostNodeChild = false;
                    NodeVisibilityChanged?.Invoke(selectedNode, false);
                }
                else if (hasChildren && kb.Key == Key.CursorRight)
                {
                    IsGhostNodeChild = true;
                    if (!selectedNode.IsExpanded)
                    {
                        selectedNode.IsExpanded = true;
                        NodeVisibilityChanged?.Invoke(selectedNode, true);
                    }
                }
                else if (selectedNode.Parent != null && kb.Key == Key.CursorLeft)
                {
                    SelectedItem = renderedItems.FindIndex(s => s == selectedNode.Parent);
                    IsGhostNodeChild = false;
                    OnSelectedChanged();
                }
                else if (SelectedItem != 0 && kb.Key == Key.CursorLeft)
                {
                    SelectedItem = 0;
                    IsGhostNodeChild = false;
                    OnSelectedChanged();
                }

                if (kb.Key == Key.Enter && selectedNode.IsGroup == false)
                    return base.ProcessKey(kb);
            
                RenderTreeView();
                return true;
            }

            if (EnableFilter)
            {
                if (kb.KeyValue >= 32 && kb.KeyValue < 127) // any non-special character is in this range
                {
                    Filter += (char) kb.KeyValue;
                    RenderTreeView();
                    FilterChanged?.Invoke(Filter);
                    return true;
                }
                if (kb.Key == (Key.Backspace|Key.CtrlMask) || kb.Key == (Key.Delete|Key.CtrlMask))
                {
                    Filter = Filter.TrimEnd();
                    var lastSpace = Filter.LastIndexOf(' ');
                    var length = lastSpace > 0 ? lastSpace + 1 : 0;
                    Filter = Filter.Substring(0, length);
                    RenderTreeView();
                    FilterChanged?.Invoke(Filter);
                    return true;
                }
                if ((kb.Key == Key.Backspace || kb.Key == Key.Delete) && Filter.Length > 0)
                {
                    Filter = Filter.Substring(0, Filter.Length - 1);
                    RenderTreeView();
                    FilterChanged?.Invoke(Filter);
                    return true;
                }
                if (kb.Key == Key.Esc)
                {
                    if (string.IsNullOrEmpty(Filter))
                        return base.ProcessKey(kb);
                    
                    Filter = "";
                    RenderTreeView();
                    FilterChanged?.Invoke(Filter);
                    return true;
                }
            }

            return base.ProcessKey(kb);
        }
        
        T lastSelectedObject;
        public override bool OnSelectedChanged ()
        {
            if (SelectedObject?.Equals(lastSelectedObject) != true)
            {
                SelectedItemChanged?.Invoke (new ListViewItemEventArgs (selected, SelectedObject));
                if (HasFocus) {
                    lastSelectedObject = SelectedObject;
                }
                return true;
            }

            return false;
        }
    }

    public class TreeViewNode<T>
    {
        public T Item { get; set; }
        public bool IsExpanded { get; set; }
        public bool AlwaysDisplayExpandState { get; set; }
        public bool IsGroup { get; set; }
        public string Title { get; set; }
        public List<string> Groups { get; set; }
        public TreeViewNode<T> Parent { get; set; }
        public List<TreeViewNode<T>> Children { get; set; }

        public bool IsVisible
        {
            get
            {
                if (Parent == null)
                    return true;

                var parent = Parent;
                while (parent != null)
                {
                    if (parent.IsExpanded == false)
                        return false;

                    parent = parent.Parent;
                }

                return true;
            }
        }

        private TreeView<T> Owner;
        public TreeViewNode(T item, TreeView<T> owner)
        {
            Item = item;
            Owner = owner;
            Children = new List<TreeViewNode<T>>();
            Groups = new List<string>();
        }

        public int Indent
        {
            get
            {

                int indent = 0;

                if (Groups?.Any() == true)
                    indent = Groups.Count;
                else
                {
                    var parent = Parent;
                    while (parent != null)
                    {
                        indent++;
                        parent = parent.Parent;
                    }
                }

                return indent;
            }
        }

        public override string ToString()
        {
            string text = new String(' ', Indent);
            if (AlwaysDisplayExpandState || Children.Any())
                text += IsExpanded || string.IsNullOrEmpty(Owner.Filter) == false ? "- " : "+ ";
            else
                text += "  ";
            
            text += Title;
            
            return text;
        }
    }
}