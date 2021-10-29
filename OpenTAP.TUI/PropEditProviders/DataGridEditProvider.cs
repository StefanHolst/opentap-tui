using System;
using System.Collections.Generic;
using System.Linq;
using OpenTap.Tui.Views;
using OpenTap.Tui.Windows;
using Terminal.Gui;

namespace OpenTap.Tui.PropEditProviders
{
    public class DataGridEditProvider : IPropEditProvider
    {
        public int Order { get; } = 15;

        (List<string>,List<string>) getColumnNames(AnnotationCollection[] items)
        {
            var Columns = new List<string>();
            var Columns2 = new List<string>();
            bool isMultiColumns = items.Any(x => x.Get<IMembersAnnotation>() != null);
            if (isMultiColumns)
            {
                HashSet<string> names = new HashSet<string>();
                Dictionary<string, double> orders = new Dictionary<string, double>();
                Dictionary<string, string> realNames = new Dictionary<string, string>();

                foreach (var mcol in items)
                {
                    var aggregate = mcol.Get<IMembersAnnotation>();
                    if (aggregate != null)
                    {
                        foreach (var a in aggregate.Members)
                        {
                            var disp = a.Get<DisplayAttribute>();
                            var mem = a.Get<IMemberAnnotation>().Member;
                            if (PropertiesView.FilterMember(mem) == false)
                                continue;
                            if (disp == null) continue;
                            var name = disp.GetFullName() + mem.TypeDescriptor.Name;
                            names.Add(name);
                            realNames[name] = disp.GetFullName();
                            orders[name] = disp.Order;
                        }
                    }
                }

                foreach (var name in names.OrderBy(x => x).OrderBy(x => orders[x]).ToArray())
                {
                    Columns2.Add(name);
                    Columns.Add(realNames[name]);
                }
            }

            return (Columns, Columns2);

        }

        public View Edit(AnnotationCollection annotation)
        {
            var col = annotation.Get<ICollectionAnnotation>();
            if (col == null) return null;
            if (annotation.Get<ReadOnlyMemberAnnotation>() != null) return null;
            bool fixedSize = annotation.Get<IFixedSizeCollectionAnnotation>()?.IsFixedSize ?? false;

            var items = col.AnnotatedElements.ToArray();
            bool placeholderElementAdded = false;
            if (items.Length == 0)
            {
                // placeholder element is added just to do some reflection to figure out which columns to create.
                placeholderElementAdded = true;
                items = new[] { col.NewElement() };
                col.AnnotatedElements = items;
                if (items[0] == null)
                    return null;
            }

            string calcMemberName(IMemberData member)
            {
                var disp = member.GetDisplayAttribute();
                if (PropertiesView.FilterMember(member) == false)
                    return null;
                if (disp == null) return null;
                var name = disp.GetFullName() + member.TypeDescriptor.Name;
                return name;
            }
            
            bool isComplicatedType = false;
            List<string> Columns = new List<string>();
            List<string> Columns2 = new List<string>();

            items = col.AnnotatedElements.ToArray();
            bool isMultiColumns = items.Any(x => x.Get<IMembersAnnotation>() != null);
            
            if (isMultiColumns)
            {
                var type = col.NewElement().Get<IReflectionAnnotation>().ReflectionInfo;
                if (type != null)
                {
                    if (type is TypeData cst)
                    {
                        if ((cst.DerivedTypes?.Count() ?? 0) > 0)
                        {
                            isComplicatedType = true;
                        }
                    }else if (type.CanCreateInstance == false)
                    {
                        isComplicatedType = true;
                    }
                }


                HashSet<string> names = new HashSet<string>();
                Dictionary<string, double> orders = new Dictionary<string, double>();
                Dictionary<string, string> realNames = new Dictionary<string, string>();
                

                foreach(var mcol in items)
                {
                    var aggregate = mcol.Get<IMembersAnnotation>();
                    if(aggregate != null)
                    {
                        foreach(var a in aggregate.Members)
                        {
                            var disp = a.Get<DisplayAttribute>();
                            var mem = a.Get<IMemberAnnotation>().Member;
                            if (PropertiesView.FilterMember(mem) == false)
                                continue;
                            if (disp == null) continue;
                            var name = disp.GetFullName() + mem.TypeDescriptor.Name;
                            names.Add(name);
                            realNames[name] = disp.GetFullName();
                            orders[name] = disp.Order;
                        }
                    }
                }
                
                

                foreach(var name in names.OrderBy(x => x).OrderBy(x => orders[x]).ToArray())
                {
                    Columns2.Add(name);
                    Columns.Add(realNames[name]);
                }
            }
            else
            {
                var name = annotation.ToString();
                Columns2.Add(name);
                Columns.Add(name);
            }
            if (placeholderElementAdded)
            {
                
                // remove the added prototype element.
                annotation.Read();
                items = col.AnnotatedElements.ToArray();
            }

            DatagridView view = null;
            view = new DatagridView(fixedSize, Columns.ToArray(), (x, y) =>
            {
                if (y >= items.Length)
                {
                    var lst = items.ToList();
                    for (int i = items.Length; i <= y; i++)
                    {
                        if (isComplicatedType)
                        {
                            Type type = (annotation.Get<IReflectionAnnotation>().ReflectionInfo as TypeData).Load();
                            var win = new NewPluginWindow(TypeData.FromType(GetEnumerableElementType(type)), "Add Element");
                            Application.Run(win);
                            if (win.PluginType == null) return null;

                            try
                            {
                                var instance = win.PluginType.CreateInstance();
                                lst.Add(AnnotationCollection.Annotate(instance));
                            }
                            catch
                            {
                                return null;
                            }

                        }
                        else
                        {
                            lst.Add(col.NewElement());    
                        }
                    }

                    col.AnnotatedElements = lst;
                    annotation.Write();
                    annotation.Read();
                    items = col.AnnotatedElements.ToArray();
                    var (columns, columns2) = getColumnNames(items);
                    if (Columns.Count != columns.Count)
                    {
                        // reload the grid with new columns.
                        Columns = columns;
                        Columns2 = columns2;
                        view.SetColumns(columns.ToArray());
                        for (int i = 0; i < items.Length; i++)
                            view.AddRow();

                        return DatagridView.FlushColumns;
                    }
                }

                var row = items[y];
                if (isMultiColumns == false)
                    return row;
                
                var cell = row.Get<IMembersAnnotation>().Members
                    .FirstOrDefault(x2 => calcMemberName(x2.Get<IMemberAnnotation>().Member) == Columns2[x]);

                if (cell == null)
                    cell = row;
                
                return cell;
            },
            i =>
            {
                var lst = items.ToList();
                lst.RemoveAt(i);
                col.AnnotatedElements = lst;
                items = col.AnnotatedElements.ToArray();
            }
            );
            for (int i = 0; i < items.Length; i++)
                view.AddRow();

            if (annotation.Get<IAccessAnnotation>()?.IsReadOnly == true)
                view.IsReadOnly = true;
            return view;
        }
        
        static public Type GetEnumerableElementType(System.Type enumType)
        {
            if (enumType.IsArray)
                return enumType.GetElementType();

            var ienumInterface = enumType.GetInterface("IEnumerable`1") ?? enumType;
            if (ienumInterface != null)
                return ienumInterface.GetGenericArguments().FirstOrDefault();

            return typeof(object);
        }

    }
}