using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace OpenTap.Tui
{
    public class TreeView : ListView
    {
        public void SetVisible(Func<TreeViewItem, bool> predicate)
        {
            foreach (var item in source)
            {
                item.SetVisible(predicate);
            }
        }
        
        public void SelectFirstMatch(Func<TreeViewItem, bool> predicate)
        {
            void getIndexOfFirstMatch(List<TreeViewItem> items, ref int index)
            {
                var item = items.FirstOrDefault(x => x.Visible);
                if (item != null)
                {
                    index++;
                    if (predicate(item))
                        return;
                    getIndexOfFirstMatch(item.SubItems, ref index);
                }
            }

            var selected = -1;
            getIndexOfFirstMatch(source, ref selected);
            if (selected >= 0)
                SelectedItem = selected;
        }
        
        private Func<object, string> getTitle;
        private Func<object, string[]> getGroup;
        public List<TreeViewItem> source { get; set; }
        public TreeViewItem SelectedObject
        {
            get
            {
                var index = SelectedItem;
                if (source == null)
                    return null;

                return FindItem(source, ref index);
            }
        }

        public TreeView(Func<object, string> getTitle, Func<object, string[]> getGroup)
        {
            this.getTitle = getTitle;
            this.getGroup = getGroup;

            CanFocus = true;
        }

        public void SetTreeViewSource<T>(List<T> items)
        {
            var list = new List<TreeViewItem>();
            foreach (var item in items)
            {
                InsertInTree(list, item, getTitle(item), getGroup(item));
            }

            if (_expandedItems.Any())
                RestoreExpansion(list);

            source = list;
            UpdateListView();
        }

        private HashSet<TreeViewItem> _expandedItems { get; } = new HashSet<TreeViewItem>();

        void RestoreExpansion(List<TreeViewItem> tree)
        {
            foreach (var item in tree)
            {
                var existing = _expandedItems.FirstOrDefault(t => t.ToString() == item.ToString());
                if (existing != null && existing.IsExpanded)
                {
                    item.IsExpanded = true;
                    RestoreExpansion(item.SubItems);
                }
            }
        }


        void InsertInTree(List<TreeViewItem> tree, object item, string title, string[] group)
        {
            if (group?.Length > 0)
            {
                if (tree.Any(t => t.Title == group[0]))
                    InsertInTree(tree.FirstOrDefault(t => t.Title == group[0]).SubItems, item, title, group.Skip(1).ToArray());
                else
                {
                    var groupItem = new TreeViewItem(group[0], null, group);
                    InsertInTree(groupItem.SubItems, item, title, group.Skip(1).ToArray());
                    tree.Add(groupItem);
                }
            }
            else
            {
                if (!tree.Any(t => t.Title == title))
                    tree.Add(new TreeViewItem(title, item));
            }
        }

        public void UpdateListView()
        {
            if (source == null)
                return;

            List<string> displayList(List<TreeViewItem> items, int level = 0)
            {
                var list = new List<string>();
                foreach (var item in items)
                {
                    if (item.Visible)
                        list.Add($"{new String(' ', level)}{(item.SubItems.Any() ? (item.IsExpanded ? "- " : "+ ") : "  ")}{ (item.obj != null ? getTitle(item.obj) : item.Title)}");
                    if (item.IsExpanded)
                    {
                        list.AddRange(displayList(item.SubItems, level + 1));
                        _expandedItems.Add(item);
                    }
                    else
                    {
                        _expandedItems.Remove(item);
                    }
                }

                return list;
            }

            var index = SelectedItem;
            SetSource(displayList(source));

            if (source.Count > 0)
                SelectedItem = index > Source.Count - 1 ? Source.Count -1 : index;
        }

        TreeViewItem FindItem(List<TreeViewItem> items, ref int index)
        {
            foreach (var item in items)
            {
                if (item.Visible == false)
                    continue;
                if (index == 0)
                    return item;

                index--;

                if (item.IsExpanded)
                {
                    var test = FindItem(item.SubItems, ref index);
                    if (test != null)
                        return test;
                }
            }

            return null;
        }

        public override bool ProcessKey(KeyEvent kb)
        {
            if ((kb.Key == Key.Enter || kb.Key == Key.CursorRight || kb.Key == Key.CursorLeft) && SelectedObject?.SubItems?.Any() == true)
            {
                if (SelectedObject != null)
                {
                    if (kb.Key == Key.Enter)
                        SelectedObject.IsExpanded = !SelectedObject.IsExpanded;
                    if (kb.Key == Key.CursorLeft)
                        SelectedObject.IsExpanded = false;
                    if (kb.Key == Key.CursorRight)
                        SelectedObject.IsExpanded = true;
                }

                UpdateListView();
                return true;
            }

            return base.ProcessKey(kb);
        }

        public class TreeViewItem
        {
            public override bool Equals(object o)
            {
                if (o is TreeViewItem t) return Equals(t);
                return false;
            }

            private bool Equals(TreeViewItem other)
            {
                return ToString().Equals(other.ToString());
            }

            public override int GetHashCode()
            {
                return ToString().GetHashCode();
            }

            private string _toString;
            public override string ToString()
            {
                return _toString ?? (_toString = string.Join(" \\ ", groups) + Title);
            }

            public string[] groups { get; }
            public string Title { get; }
            public object obj { get; set; }
            public bool IsExpanded { get; set; }
            public bool Visible { get; set; } = true;

            public List<TreeViewItem> SubItems { get; set; }

            public TreeViewItem(string Title, object obj, params string[] groups)
            {
                this.Title = Title;
                this.obj = obj;
                this.groups = groups;

                SubItems = new List<TreeViewItem>();
            }

            public void ShowAll(TreeViewItem item)
            {
                item.Visible = true;
                item.IsExpanded = true;

                foreach (var subItem in item.SubItems)
                {
                    ShowAll(subItem);
                }
            }

            public bool SetVisible(Func<TreeViewItem, bool> predicate)
            {
                Visible = false;
                foreach (var item in SubItems)
                {
                    Visible |= item.SetVisible(predicate);
                }

                var matchesPred = predicate(this);
                Visible |= matchesPred;

                // If a group is matched by the predicate, show all children of the group
                if (matchesPred)
                    ShowAll(this);

                IsExpanded = Visible;
                return Visible;
            }
        }
    }
}