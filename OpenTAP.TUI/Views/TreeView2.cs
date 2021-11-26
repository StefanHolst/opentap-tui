using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace OpenTap.Tui
{
    public class TreeView2<T> : ListView
    {
        private Func<T, string> getTitle;
        private Func<T, List<T>> getChildren;
        private Func<T, T> getParent;
        private Func<T, bool> isVisible;
        private IList<T> items;
        private Dictionary<T, TreeViewNode<T>> nodes;
        private List<TreeViewNode<T>> renderedItems;
        
        public T SelectedObject
        {
            get => SelectedItem < renderedItems.Count ? renderedItems[SelectedItem].Item : default;
            set => SelectedItem = renderedItems.IndexOf(nodes[value]);
        }

        public TreeView2(Func<T, string> getTitle, Func<T, List<T>> getChildren, Func<T, T> getParent, Func<T, bool> isVisible)
        {
            this.getTitle = getTitle;
            this.getChildren = getChildren;
            this.getParent = getParent;
            this.isVisible = isVisible;

            CanFocus = true;
        }

        private TreeViewNode<T> GetTreeFromItem(T item)
        {
            TreeViewNode<T> node;
            if (nodes.TryGetValue(item, out node) == false)
            {
                node = new TreeViewNode<T>(item);
                nodes[item] = node;
            }

            node.IsVisible = isVisible?.Invoke(item) ?? true;
            node.Title = getTitle(item);
            node.Nodes = getChildren(item);

            var parent = getParent(item);
            if (parent != null)
                node.Parent = nodes[parent];
            else
                node.Parent = null;
            node.Cached = true;

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

        public void RenderTreeView(bool noCache = false)
        {
            if (this.items == null)
                return;

            var list = new List<TreeViewNode<T>>();
            void GetItemsToRender(T item)
            {
                TreeViewNode<T> node;
                bool existed = nodes.TryGetValue(item, out node);
                if (existed == false)
                    node = GetTreeFromItem(item);

                if (noCache && existed)
                    node = GetTreeFromItem(node.Item);

                if (node.IsVisible)
                {
                    list.Add(node);
                    if (node.IsExpanded)
                    {
                        foreach (var child in node.Nodes)
                            GetItemsToRender(child);
                    }
                }
            }
            
            foreach (var item in items)
                GetItemsToRender(item);

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
                if (selectedNode.Nodes.Any())
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
    public class TreeViewNode<T>
    {
        public bool Cached { get; set; }
        public string Title { get; set; }
        public T Item { get; set; }
        public bool IsExpanded { get; set; }
        public bool IsVisible { get; set; } = true;
        public TreeViewNode<T> Parent { get; set; }

        public List<T> Nodes { get; set; }

        public TreeViewNode(T item)
        {
            this.Item = item;
            Nodes = new List<T>();
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
            if (Nodes.Any())
                text += IsExpanded ? "- " : "+ ";
            else
                text += "  ";
            
            text += Title;
            
            return text;
        }
    }
}