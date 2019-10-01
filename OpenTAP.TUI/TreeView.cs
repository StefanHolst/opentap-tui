﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Terminal.Gui;

namespace OpenTap.Tui
{
    public class TreeView : ListView
    {
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

            source = list;
            UpdateListView();
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
                    list.Add($"{new String(' ', level)}{(item.SubItems.Any() ? (item.IsExpanded ? "- " : "+ ") : "  ")}{ (item.obj != null ? getTitle(item.obj) : item.Title)}");
                    if (item.IsExpanded)
                        list.AddRange(displayList(item.SubItems, level + 1));
                }

                return list;
            }

            var index = SelectedItem;
            SetSource(displayList(source));
            SelectedItem = index;
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

            public List<TreeViewItem> SubItems { get; set; }

            public TreeViewItem(string Title, object obj)
            {
                this.Title = Title;
                this.obj = obj;

                SubItems = new List<TreeViewItem>();
            }
        }
    }
}