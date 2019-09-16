using System;
using System.Collections.ObjectModel;
using OpenTap;
using Terminal.Gui;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using NStack;

namespace OpenTAP.TUI
{
    public class DatagridView : Window
    {
        public int Rows { get; private set; }
        public int Columns { get { return columns.Count; } }

        Func<int, int, AnnotationCollection> CellProvider;
        List<(View header, FramedListView column)> columns = new List<(View header, FramedListView column)>();
        Dictionary<(int, int), AnnotationCollection> cells = new Dictionary<(int, int), AnnotationCollection>();
        MenuBar menu;

        public DatagridView(string title, string[] headers, Func<int, int, AnnotationCollection> CellProvider) : base(title)
        {
            this.CellProvider = CellProvider;

            // Add menu
            menu = new MenuBar(new MenuBarItem[]{
                new MenuBarItem("Edit", new MenuItem[]{
                    new MenuItem("Add Row", "", () =>{ AddRow(); }),
                    new MenuItem("Remove Row", "", () => { SetColumns(new []{ "key", "value", "something else" }); })
                })
            });
            Add(menu);

            SetColumns(headers);
        }

        public void AddRow()
        {
            Rows++;

            for (int i = 0; i < Columns; i++)
            {
                if (cells.ContainsKey((i, Rows - 1)) == false)
                    cells[(i, Rows - 1)] = CellProvider.Invoke(i, Rows - 1);

                var list = new List<string>();
                for (int j = 0; j < Rows; j++)
                {
                    list.Add(cells[(i, j)].Get<IObjectValueAnnotation>().Value.ToString());
                }

                var column = columns[i].column;
                column.SetSource(list);
            }

            SetFocus(columns[0].column);
            LayoutSubviews();
        }

        public void SetColumns(string[] headers)
        {
            ClearDatagrid();

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
                headerFrame.Add(new Label(headers[i]) { TextColor = ColorScheme.HotNormal });
                Add(headerFrame);

                // Add cells frame
                var columnFrame = new FramedListView(null)
                {
                    Y = Pos.Bottom(headerFrame),
                    X = preColumn == null ? 0 : Pos.Right(preColumn),
                    Height = Dim.Fill(),
                    Width = preColumn == null ? Dim.Percent((float)100 / headers.Length) : Dim.Width(preColumn),
                    CanFocus = true
                };

                columnFrame.Source.SelectedChanged += () => Scroll(columnFrame);

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
        }

        public void Scroll(FramedListView list)
        {
            foreach (var column in columns)
            {
                if (list != column.column)
                    column.column.Source.TopItem = list.Source.TopItem;
            }
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
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

            return base.ProcessKey(keyEvent);
        }
    }

    public class FramedListView : FrameView
    {
        public ListView Source { get; set; }

        public FramedListView(ustring title) : base(title)
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

    class MyListWrapper : IListDataSource
    {
        IList src;
        int count;

        public MyListWrapper(IList source)
        {
            count = source.Count;
            this.src = source;
        }

        public int Count => src.Count;

        public void Render(ListView container, ConsoleDriver driver, bool marked, int item, int col, int line, int width)
        {
            container.Move(col, line);
            var t = src[item];

            if (t is View v)
            {
                if (v.ColorScheme == null)
                    v.ColorScheme = Colors.Base;

                v.Frame = new Rect(col + container.Frame.X + 1, line + container.Frame.Y + 1, width, 1);
                v.Redraw(new Rect(0, 0, width, 1));
            }
        }

        public bool IsMarked(int item)
        {
            return false;
        }

        public void SetMark(int item, bool value)
        {
        }
    }
    
}