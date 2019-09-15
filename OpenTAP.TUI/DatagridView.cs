using System;
using System.Collections.ObjectModel;
using OpenTap;
using Terminal.Gui;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace OpenTAP.TUI
{
    public class DatagridView : Window
    {
        public int Rows { get; private set; }
        public int Columns { get { return columns.Count; } }

        Func<int, int, View> CellProvider;
        List<(View header, View column)> columns = new List<(View header, View column)>();
        Dictionary<(int, int), View> cells = new Dictionary<(int, int), View>();
        MenuBar menu;

        public DatagridView(string title, string[] headers, Func<int, int, View> CellProvider) : base(title)
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


            var cFrame = new Rect(1, 1, 20, 20);

            var list = new List<View>();
            for (int i = 0; i < 1; i++)
            {
                list.Add(new TextField("hej" + i) { Y = i });
                list.Add(new Label("hej" + i) { Y = i });
            }

            var scroll = new ListView(new MyListWrapper(list))
            {
                //Height = Dim.Fill(),
                //Width = Dim.Fill()
                Y = 1,
                //X = 1
            };
            Add(scroll);

            

            //SetColumns(headers);
            //SetFocus(columns[0].column);

            //// testing
            //for (int i = 0; i < 50; i++)
            //{
            //    AddRow();
            //}
        }

        public void AddRow()
        {
            Rows++;

            for (int i = 0; i < Columns; i++)
            {
                if (cells.ContainsKey((i, Rows - 1)) == false)
                    cells[(i, Rows - 1)] = CellProvider.Invoke(i, Rows - 1);

                var cell = cells[(i, Rows - 1)];

                var column = columns[i].column;
                cell.Y = Rows - 1;
                cell.Width = Dim.Fill();
                cell.Height = 1;

                column.Add(cell);
            }

            SetFocus(cells[(0, Rows - 1)]);
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
                var columnFrame = new FrameView(null)
                {
                    Y = Pos.Bottom(headerFrame),
                    X = preColumn == null ? 0 : Pos.Right(preColumn),
                    Height = Dim.Fill(),
                    Width = preColumn == null ? Dim.Percent((float)100 / headers.Length) : Dim.Width(preColumn),
                    CanFocus = true
                };

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

        /// TODO: 
        /// Scroll

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