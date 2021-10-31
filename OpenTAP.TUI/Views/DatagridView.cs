using System;
using Terminal.Gui;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using NStack;
using OpenTap.Tui.PropEditProviders;
using OpenTap.Tui.Windows;

namespace OpenTap.Tui.Views
{
    public class DatagridView : View
    {
        public int Rows { get; private set; }
        public int Columns { get { return columns.Count; } }

        Func<int, int, AnnotationCollection> CellProvider;
        List<(View header, FramedListView column)> columns = new List<(View header, FramedListView column)>();
        Dictionary<(int, int), AnnotationCollection> cells = new Dictionary<(int, int), AnnotationCollection>();
        MenuBar menu;
        Action<int> deleteRow;
        
        /// <summary>  Number of elements cannot be changed. </summary>
        public bool IsIsFixedSize { get; }

        bool isReadOnly;
        public bool IsReadOnly
        {
            get => isReadOnly;
            set
            {
                isReadOnly = value;
                Remove(menu);
            }
        }

        public static AnnotationCollection FlushColumns = new AnnotationCollection();

        void addMenu()
        {
            // Add menu
            if (IsIsFixedSize == false)
            {
                menu = new MenuBar(new MenuBarItem[]
                {
                    new MenuBarItem("_Edit", new MenuItem[]
                    {
                        new MenuItem("_Add Row", "", () => AddRow()),
                        new MenuItem("_Remove Row", "", () => RemoveCurrentRow())
                    })
                });
                Add(menu);
            }
        }
        public DatagridView(bool isFixedSize, string[] headers, Func<int, int, AnnotationCollection> CellProvider, Action<int> deleteRow) : base()
        {
            IsIsFixedSize = isFixedSize;
            this.deleteRow = deleteRow;
            this.CellProvider = CellProvider;

            SetColumns(headers);
        }

        void RemoveCurrentRow()
        {
            if (MostFocused is ListView listview)
            {
                RemoveRow(listview.SelectedItem);
            }
        }
        
        public bool AddRow()
        {
            Rows++;

            for (int i = 0; i < Columns; i++)
            {
                if (cells.ContainsKey((i, Rows - 1)) == false)
                {
                    var cell = CellProvider.Invoke(i, Rows - 1);
                    if (cell == null || cell == FlushColumns)
                    {
                        Rows--;
                        return false;
                    }
                    cells[(i, Rows - 1)] = cell;
                }

                UpdateColumn(i);
            }

            if (columns.Any())
                columns[0].column.SetFocus(); // TODO: Test
            LayoutSubviews();

            return true;
        }

        public void RemoveRow(int index)
        {
            if (Rows <= 0)
                return;
            deleteRow(index);
            Rows--;
            for (int i = 0; i < Columns; i++)
            {
                for (int j = index; j < Rows; j++)
                {
                    cells[(i, j)] = cells[(i, j + 1)];
                }

                cells.Remove((i, Rows));
                UpdateColumn(i);
            }

            LayoutSubviews();
        }

        public void SetColumns(string[] headers)
        {
            ClearDatagrid();

            addMenu();
            for (int i = 0; i < headers.Length; i++)
            {
                var preColumn = i > 0 ? columns[i - 1].header : null;

                // Add headers
                var headerFrame = new FrameView(null)
                {
                    X = preColumn == null ? 0 : Pos.Right(preColumn),
                    Y = 1,
                    Height = 3,
                    Width = preColumn == null ? Dim.Percent((float)100 / headers.Length) : Dim.Width(preColumn)
                };
                headerFrame.Add(new Label(headers[i]) {  });
                Add(headerFrame);

                // Add cells frame
                var columnFrame = new FramedListView(null)
                {
                    Y = 4,  // this did not seem to work after reloading columns:  Pos.Bottom(headerFrame),
                    X = preColumn == null ? 0 : Pos.Right(preColumn),
                    Height = Dim.Fill(),
                    Width = preColumn == null ? Dim.Percent((float)100 / headers.Length) : Dim.Width(preColumn),
                    CanFocus = true
                };

                columnFrame.Source.SelectedItemChanged += args => Scroll(columnFrame);

                Add(columnFrame);
                columns.Add((headerFrame, columnFrame));
            }

            LayoutSubviews();
        }

        public void ClearDatagrid()
        {
            Clear();
            columns.Clear();
            cells.Clear();
            Rows = 0;
            RemoveAll();
        }

        private void Scroll(FramedListView list)
        {
            foreach (var column in columns)
            {
                if (list != column.column && list.Source.TopItem != column.column.Source.TopItem)
                    column.column.Source.TopItem = list.Source.TopItem;
            }
        }

        private void UpdateColumn(int i)
        {
            var list = new List<string>();
            for (int j = 0; j < Rows; j++)
            {
                var annotation = cells[(i, j)];
                list.Add(annotation.Get<IStringReadOnlyValueAnnotation>()?.Value ?? annotation.Get<IObjectValueAnnotation>().Value?.ToString() ?? "");
            }

            var column = columns[i].column;
            column.SetSource(list);
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (IsReadOnly)
            {
                return base.ProcessKey(keyEvent);
            }
            if (keyEvent.Key == Key.Tab && columns.Any())
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    if (columns[i].column.HasFocus)
                    {
                        var column = i >= columns.Count - 1 ? columns[0].column : columns[i + 1].column;
                        column.Subviews[0].FocusFirst();
                        return true;
                    }
                }
            }

            if (MostFocused is ListView listview)
            {
                if (keyEvent.Key == Key.Enter)
                {
                    for (int i = 0; i < columns.Count; i++)
                    {
                        if (columns[i].column.HasFocus)
                        {
                            if (cells.ContainsKey((i, listview.SelectedItem)) == false)
                            {
                                while (cells.ContainsKey((i, listview.SelectedItem)) == false)
                                {
                                    if (AddRow() == false)
                                        return false;
                                }

                                return true;
                            }
                            
                            // Find edit provider
                            var cell = cells[(i, listview.SelectedItem)];
                            var propEditor = PropEditProvider.GetProvider(cell, out var provider);
                            if (propEditor == null)
                                TUI.Log.Warning($"Cannot edit properties of type: {cell.Get<IMemberAnnotation>()?.ReflectionInfo.Name}");
                            else
                            {
                                var win = new EditWindow(cell.ToString());
                                win.Add(propEditor);
                                Application.Run(win);
                            }

                            // Save values to reference object
                            cell.Write();
                            cell.Read();

                            UpdateColumn(i);
                            return true;
                        }
                    }
                }

                if (keyEvent.Key == Key.DeleteChar)
                {
                    RemoveRow(listview.SelectedItem);
                    return true;
                }
            }

            return base.ProcessKey(keyEvent);
        }
    }

    class FramedListView : FrameView
    {
        public ListView Source { get; set; }

        public FramedListView(ustring title = null) : base(title)
        {
            Source = new ListView();
            Source.CanFocus = true;

            Add(Source);
        }

        public void SetSource(IList source)
        {
            Source.SetSource(source);
        }
    }    
}