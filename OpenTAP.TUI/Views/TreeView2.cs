﻿using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace OpenTap.Tui
{
    public class TreeView2<T> : ListView
    {
        private Func<T, string> getTitle;
        private Func<T, List<string>> getGroups;
        private Func<T, List<T>> getChildren;
        private Func<T, T> getParent;
        private IList<T> items;
        private Dictionary<T, TreeViewNode<T>> nodes;
        private Dictionary<string, TreeViewNode<T>> groups = new Dictionary<string, TreeViewNode<T>>();
        private List<TreeViewNode<T>> renderedItems;
        public bool EnableFilter { get; set; }
        public string Filter { get; set; } = "";
        public Action<string> FilterChanged { get; set; }
        
        public T SelectedObject
        {
            get => SelectedItem < renderedItems.Count ? renderedItems[SelectedItem].Item : default;
            set => SelectedItem = renderedItems.IndexOf(nodes[value]);
        }

        public TreeView2(Func<T, string> getTitle, Func<T, List<string>> getGroups)
        {
            this.getTitle = getTitle;
            this.getGroups = getGroups;

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
            TreeViewNode<T> node;
            if (nodes.TryGetValue(item, out node) == false)
            {
                node = new TreeViewNode<T>(item, this);
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
                }
                
                // Build groups tree
                
                
                if (node.IsVisible)
                {
                    int index = -1;
                    TreeViewNode<T> groupNode = null;
                    TreeViewNode<T> lastGroup = null;
                    
                    // Add group
                    foreach (var group in node.Groups)
                    {
                        if (groups.TryGetValue(group, out groupNode) == false)
                        {
                            // add the group
                            groupNode = new TreeViewNode<T>(default(T), this)
                            {
                                Title = group,
                                IsGroup = true
                            };
                            groups[group] = groupNode;
                        }
                        
                        if (list.Contains(groupNode) == false)
                            list.Add(groupNode);

                        if (lastGroup != null)
                        {
                            groupNode.Parent = lastGroup;
                            lastGroup.Children.Add(groupNode);
                        }

                        lastGroup = groupNode;
                        index = list.IndexOf(groupNode);
                    }
                    
                    lastGroup.Children.Add();
                    
                    if (Filter?.Length > 0 || (groupNode?.IsExpanded ?? true))
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
                node = GetNodeFromItem(item);

            if (node.IsVisible)
            {
                list.Add(node);
                if (node.IsExpanded || Filter?.Length > 0)
                {
                    foreach (var child in node.Children)
                        list.AddRange(GetItemsToRender(child.Item, noCache));
                }
            }

            return list;
        }

        public void RenderTreeView(bool noCache = false)
        {
            if (items == null)
                return;

            var list = new List<TreeViewNode<T>>();
            if (getGroups != null)
                list = GetItemsToRenderWithGroup(noCache);
            else
            {
                foreach (var item in items)
                    list.AddRange(GetItemsToRender(item, noCache));
            }

            if (string.IsNullOrEmpty(Filter) == false)
                renderedItems = list.Where(i => i.Title.ToLower().Contains(Filter.ToLower())).ToList();
            else
                renderedItems = list;
            
            var index = SelectedItem;
            var oldTop = TopItem;
            SetSource(renderedItems);
            
            // can select next item
            if (index >= renderedItems.Count)
                SelectedItem = renderedItems.Count - 1;
            else
                SelectedItem = index;

            if (oldTop > 0 && oldTop >= renderedItems.Count)
                TopItem = renderedItems.Count - 1;
            else
                TopItem = oldTop;
        }
        
        public override bool ProcessKey(KeyEvent kb)
        {
            if (kb.Key == Key.Enter || kb.Key == Key.CursorRight || kb.Key == Key.CursorLeft)
            {
                var selectedNode = renderedItems[SelectedItem];
                if (selectedNode.Children.Any())
                {
                    if (kb.Key == Key.CursorLeft)
                        selectedNode.IsExpanded = false;
                    if (kb.Key == Key.CursorRight)
                        selectedNode.IsExpanded = true;
                }

                if (kb.Key == Key.Enter && selectedNode.IsGroup == false)
                    base.ProcessKey(kb);
            
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
        public bool IsGroup { get; set; }
        public string Title { get; set; }
        public List<string> Groups { get; set; }
        public bool IsVisible { get; set; } = true;
        public TreeViewNode<T> Parent { get; set; }
        public List<TreeViewNode<T>> Children { get; set; }

        private TreeView2<T> Owner;
        public TreeViewNode(T item, TreeView2<T> owner)
        {
            Item = item;
            Owner = owner;
            Children = new List<TreeViewNode<T>>();
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
                text += IsExpanded || string.IsNullOrEmpty(Owner.Filter) == false ? "- " : "+ ";
            else
                text += "  ";
            
            text += Title;
            
            return text;
        }
    }
}