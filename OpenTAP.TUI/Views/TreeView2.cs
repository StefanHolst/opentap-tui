using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace OpenTap.Tui
{
    public class TreeView2<T> : ListView
    {
        private Func<T, string> getTitle;
        private Func<T, List<string>> getGroups;
        private Func<T, string, T> createItem;
        private Func<T, List<T>> getChildren;
        private Func<T, T> getParent;
        private IList<T> items;
        private Dictionary<T, TreeViewNode<T>> nodes;
        private Dictionary<string, TreeViewNode<T>> groups = new Dictionary<string, TreeViewNode<T>>();
        private List<TreeViewNode<T>> renderedItems;
        
        public T SelectedObject
        {
            get => SelectedItem < renderedItems.Count ? renderedItems[SelectedItem].Item : default;
            set => SelectedItem = renderedItems.IndexOf(nodes[value]);
        }

        public TreeView2(Func<T, string> getTitle, Func<T, List<string>> getGroups, Func<T, string, T> createItem)
        {
            this.getTitle = getTitle;
            this.getGroups = getGroups;
            this.createItem = createItem;

            CanFocus = true;
        }
        
        public TreeView2(Func<T, string> getTitle, Func<T, List<T>> getChildren, Func<T, T> getParent)
        {
            this.getTitle = getTitle;
            this.getChildren = getChildren;
            this.getParent = getParent;

            CanFocus = true;
        }

        private TreeViewNode<T> GetNodeFromItem(T item)
        {
            var node = new TreeViewNode<T>(item);
            node.Title = getTitle(item);
            node.Children = getChildren?.Invoke(item) ?? new List<T>();

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
            var list = new List<TreeViewNode<T>>();
            foreach (var item in items)
            {
                TreeViewNode<T> node;
                bool existed = nodes.TryGetValue(item, out node);
                if (existed == false || noCache)
                {
                    var _groups = getGroups(item);
                    node = GetNodeFromItem(item);
                    node.Groups = _groups;
                    nodes[item] = node;
                }
                
                if (node.IsVisible)
                {
                    int index = -1;
                    TreeViewNode<T> groupNode = null;
                    
                    // Add group
                    foreach (var group in node.Groups)
                    {
                        if (groups.TryGetValue(group, out groupNode) == false)
                        {
                            // add the group
                            groupNode = new TreeViewNode<T>(default(T))
                            {
                                Title = group,
                                IsExpanded = true
                            };
                            groupNode.Children.Add(item);
                            list.Add(groupNode);
                            groups[group] = groupNode;
                        }
                        index = list.IndexOf(groupNode);
                    }
                    
                    if (groupNode?.IsExpanded ?? true)
                        list.Insert(index == -1 ? 0 : index + 1, node);
                }
            }
            
            return list;
        }
        List<TreeViewNode<T>> GetItemsToRender(T item, bool noCache)
        {
            var list = new List<TreeViewNode<T>>();
            TreeViewNode<T> node;
            bool existed = nodes.TryGetValue(item, out node);
            if (existed == false || noCache)
            {
                node = GetNodeFromItem(item);
                nodes[item] = node;
            }

            if (node.IsVisible)
            {
                list.Add(node);
                if (node.IsExpanded)
                {
                    foreach (var child in node.Children)
                        list.AddRange(GetItemsToRender(child, noCache));
                }
            }

            return list;
        }

        public void RenderTreeView(bool noCache = false)
        {
            if (this.items == null)
                return;

            var list = new List<TreeViewNode<T>>();
            if (getGroups != null)
            {
                list = GetItemsToRenderWithGroup(noCache);
                // foreach (var item in items)
                //     list.AddRange(GetItemsToRender(item, getGroups(item), noCache));
            }
            else
            {
                foreach (var item in items)
                    list.AddRange(GetItemsToRender(item, noCache));
            }

            renderedItems = list;
            var index = SelectedItem;
            var oldTop = TopItem;
            SetSource(renderedItems);
            
            // can select next item
            if (index >= renderedItems.Count)
                SelectedItem = renderedItems.Count - 1;
            else
                SelectedItem = index;

            TopItem = oldTop;
        }
        
        public override bool ProcessKey(KeyEvent kb)
        {
            if (kb.Key == Key.Enter || kb.Key == Key.CursorRight || kb.Key == Key.CursorLeft)
            {
                var selectedNode = renderedItems[SelectedItem];
                if (selectedNode.Children.Any())
                {
                    if (kb.Key == Key.Enter)
                        selectedNode.IsExpanded = !selectedNode.IsExpanded;
                    if (kb.Key == Key.CursorLeft)
                        selectedNode.IsExpanded = false;
                    if (kb.Key == Key.CursorRight)
                        selectedNode.IsExpanded = true;
                }
            
                RenderTreeView();
                return true;
            }

            return base.ProcessKey(kb);
        }
        
        int lastSelectedItem = -1;
        public override bool OnSelectedChanged ()
        {
            if (selected != lastSelectedItem) {
                SelectedItemChanged?.Invoke (new ListViewItemEventArgs (selected, SelectedObject));
                if (HasFocus) {
                    lastSelectedItem = selected;
                }
                return true;
            }

            return false;
        }

    }

    class TreeViewNode<T>
    {
        public T Item { get; set; }
        public bool IsExpanded { get; set; }
        public string Title { get; set; }
        public List<string> Groups { get; set; }
        public bool IsVisible { get; set; } = true;
        public TreeViewNode<T> Parent { get; set; }
        public List<T> Children { get; set; }

        public TreeViewNode(T item)
        {
            this.Item = item;
            Children = new List<T>();
        }

        public override string ToString()
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
            
            string text = new String(' ', indent);
            if (Children.Any())
                text += IsExpanded ? "- " : "+ ";
            else
                text += "  ";
            
            text += Title;
            
            return text;
        }
    }
}