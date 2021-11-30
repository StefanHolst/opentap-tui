using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace OpenTap.Tui
{
    public class TreeView2<T> : ListView
    {
        private Func<T, string> getTitle;
        private Func<T, List<string>> getGroup;
        private Func<T, string, T> createItem;
        private Func<T, List<T>> getChildren;
        private Func<T, T> getParent;
        private IList<T> items;
        private Dictionary<T, TreeViewNode<T>> nodes;
        private List<TreeViewNode<T>> renderedItems;
        
        public T SelectedObject
        {
            get => SelectedItem < renderedItems.Count ? renderedItems[SelectedItem].Item : default;
            set => SelectedItem = renderedItems.IndexOf(nodes[value]);
        }

        public TreeView2(Func<T, string> getTitle, Func<T, List<string>> getGroup, Func<T, string, T> createItem)
        {
            this.getTitle = getTitle;
            this.getGroup = getGroup;
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

        private TreeViewNode<T> GetNodeFromItemGroup(T item, string group)
        {
            TreeViewNode<T> node;
            if (group != null)
            {
                var parentItem = createItem(item, group);
                if (nodes.TryGetValue(parentItem, out node) == false)
                {
                    node = new TreeViewNode<T>(item);
                    nodes[parentItem] = node;
                }
                
                if (node.Children.Contains(item) == false)
                    node.Children.Add(item);
            }
            else
            {
                if (nodes.TryGetValue(item, out node) == false)
                {
                    node = new TreeViewNode<T>(item);
                    nodes[item] = node;
                }
            }
            
            node.Title = group ?? getTitle(node.Item);

            return node;
        }
        
        private TreeViewNode<T> GetNodeFromItem(T item)
        {
            TreeViewNode<T> node;
            if (nodes.TryGetValue(item, out node) == false)
            {
                node = new TreeViewNode<T>(item);
                nodes[item] = node;
            }

            node.Title = getTitle(item);
            node.Children = getChildren(item);

            var parent = getParent(item);
            if (parent != null && nodes.ContainsKey(parent))
                node.Parent = nodes[parent];
            else
                node.Parent = null;

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
            RenderTreeView();
        }
        
        List<TreeViewNode<T>> GetItemsToRender(T item, List<string> groups, bool noCache)
        {
            var list = new List<TreeViewNode<T>>();
            TreeViewNode<T> node;
            bool existed = nodes.TryGetValue(item, out node);
            if (existed == false || noCache)
            {
                node = GetNodeFromItemGroup(item, null);
                while (groups.Any())
                {
                    var group = groups.FirstOrDefault();
                    node = GetNodeFromItemGroup(item, group);
                    groups = groups.Skip(1).ToList();
                }
            }

            if (node.IsVisible)
            {
                list.Add(node);
                if (node.IsExpanded)
                {
                    foreach (var child in node.Children)
                        list.AddRange(GetItemsToRender(child, groups.Skip(1).ToList(), noCache));
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
                node = GetNodeFromItem(item);

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
            if (getGroup != null)
            {
                foreach (var item in items)
                    list.AddRange(GetItemsToRender(item, getGroup(item), noCache));
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
                var selectedNode = nodes[SelectedObject];
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
            var parent = Parent;
            while (parent != null)
            {
                indent++;
                parent = parent.Parent;
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