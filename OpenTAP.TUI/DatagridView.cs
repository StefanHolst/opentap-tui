using System;
using System.Collections.ObjectModel;
using OpenTap;
using Terminal.Gui;
using System.Linq;

namespace OpenTAP.TUI
{
    public class DatagridView : Window
    {
        Func<Point,View,View> CellProvider;
        public int Rows { get; private set; }
        public int Column { get; private set; }

        Lookup<Point, View> ViewLookup;// = new Lookup<Point, View>();

        public DatagridView(Func<Point, View, View> CellProvider) : base("Datagrid")
        {
            this.CellProvider = CellProvider;

            // Add menu
            var menu = new MenuBar(new MenuBarItem[]{
                new MenuBarItem("Edit", new MenuItem[]{
                    new MenuItem("Add Row", "", () => {}),
                    new MenuItem("Remove Row", "", () => {})
                })
            });
            Add(menu);
        }

        public void AddRow()
        {
            Rows++;

        }
        public void AddColumn()
        {

        }



        public override void Redraw(Rect region)
        {
            // call redraw on all cells
        }
    }
}