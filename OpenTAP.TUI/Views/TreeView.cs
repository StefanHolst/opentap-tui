using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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

            if (source != null)
                MatchExpansion(source, list);

            source = list;
            UpdateListView();
        }

        void MatchExpansion(List<TreeViewItem> existingTree, List<TreeViewItem> tree)
        {
            foreach (var item in tree)
            {
                var existing = existingTree.FirstOrDefault(t => t.Title == item.Title);
                if (existing != null && existing.IsExpanded)
                {
                    item.IsExpanded = true;
                    MatchExpansion(existing.SubItems, item.SubItems);
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
                    var groupItem = new TreeViewItem(group[0], null);
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
            List<string> displayList(List<TreeViewItem> items, int level = 0)
            {
                var list = new List<string>();
                foreach (var item in items)
                {
                    if (item.Visible)
                        list.Add($"{new String(' ', level)}{(item.SubItems.Any() ? (item.IsExpanded ? "- " : "+ ") : "  ")}{ (item.obj != null ? getTitle(item.obj) : item.Title)}");
                    if (item.IsExpanded)
                        list.AddRange(displayList(item.SubItems, level + 1));
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
            public string Title { get; set; }
            public object obj { get; set; }
            public bool IsExpanded { get; set; }
            public bool Visible { get; set; } = true;

            public List<TreeViewItem> SubItems { get; set; }

            public TreeViewItem(string Title, object obj)
            {
                this.Title = Title;
                this.obj = obj;

                SubItems = new List<TreeViewItem>();
            }

            public bool SetVisible(Func<TreeViewItem, bool> predicate)
            {
                Visible = false;
                foreach (var item in SubItems)
                {
                    Visible |= item.SetVisible(predicate);
                }

                Visible = Visible || predicate(this);
                IsExpanded = Visible;
                return Visible;
            }
        }
    }
}