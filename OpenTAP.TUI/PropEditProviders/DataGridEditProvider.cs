using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using OpenTap.Tui.Views;
using OpenTap.Tui.Windows;
using Terminal.Gui;
using Attribute = System.Attribute;

namespace OpenTap.Tui.PropEditProviders
{
    public class DataGridEditProvider : IPropEditProvider
    {
        public int Order { get; } = 15;

        (List<string> Headers, List<string> Columns, bool IsComplicatedType) getColumnNames(AnnotationCollection annotation, AnnotationCollection[] items)
        {
            var Columns = new List<string>();
            var Headers = new List<string>();
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
                    Columns.Add(name);
                    Headers.Add(realNames[name]);
                }
            }
            else
            {
                var name = annotation.ToString();
                Columns.Add(name);
                Headers.Add(name);
            }

            
            // Check is complicated type
            bool isComplicatedType = false;
            var col = annotation.Get<ICollectionAnnotation>();
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

            return (Columns, Headers, isComplicatedType);

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

        public View Edit(AnnotationCollection annotation)
        {
            var collectionAnnotation = annotation.Get<ICollectionAnnotation>();
            if (collectionAnnotation == null) return null;
            if (annotation.Get<ReadOnlyMemberAnnotation>() != null) return null;
            var isReadOnly = annotation.Get<IAccessAnnotation>()?.IsReadOnly == true;
            var fixedSize = annotation.Get<IFixedSizeCollectionAnnotation>()?.IsFixedSize ?? false;
            if (fixedSize == false)
                fixedSize = annotation.Get<IMemberAnnotation>()?.Member.Attributes.Any(a => a is FixedSizeAttribute) ?? false;
            
            var items = collectionAnnotation.AnnotatedElements.ToArray();
            bool placeholderElementAdded = false;
            
            // placeholder element is added just to do some reflection to figure out which columns to create.
            if (items.Length == 0)
            {
                placeholderElementAdded = true;
                items = new[] { collectionAnnotation.NewElement() };
                collectionAnnotation.AnnotatedElements = items;
                if (items[0] == null)
                    return null;
            }
            
            // Get headers and columns
            bool isComplicatedType = false;
            List<string> Headers = new List<string>();
            List<string> Columns = new List<string>();
            (Columns, Headers, isComplicatedType) = getColumnNames(annotation, items);
            
            // remove the added prototype element.
            if (placeholderElementAdded)
            {
                annotation.Read();
                items = collectionAnnotation.AnnotatedElements.ToArray();
            }

            var viewWrapper = new View();
            var tableView = new TableView()
            {
                Height = Dim.Fill(1)
            };
            viewWrapper.Add(tableView);

            var table = LoadTable(Headers, Columns, items);
            tableView.Table = table;

            tableView.CellActivated += args =>
            {
                if (isReadOnly || args.Row > items.Length || args.Row < 0)
                    return;

                var item = items[args.Row];
                AnnotationCollection cell;
                var members = item.Get<IMembersAnnotation>()?.Members;
                // If the item does not have an IMembersAnnotation, it is a single value, and not a collection
                // This means we should edit the item directly
                if (members == null)
                    cell = item;
                else
                    cell = members.FirstOrDefault(x2 => calcMemberName(x2.Get<IMemberAnnotation>().Member) == Columns[args.Col]);

                if (cell == null) return;

                // Find edit provider
                var propEditor = PropEditProvider.GetProvider(cell, out var provider);
                if (propEditor == null)
                    TUI.Log.Warning($"Cannot edit properties of type: {cell.Get<IMemberAnnotation>()?.ReflectionInfo.Name}");
                else
                {
                    var win = new EditWindow(cell.ToString().Split(':')[0]);
                    win.Add(propEditor);
                    Application.Run(win);
                }
                
                // Save values to reference object
                cell.Write();
                cell.Read();
                
                // Update table value
                var cellValue = cell.Get<IStringReadOnlyValueAnnotation>()?.Value ?? cell.Get<IObjectValueAnnotation>().Value?.ToString();
                table.Rows[args.Row][args.Col] = cellValue;
                tableView.Update();
            };

            // Add helper buttons
            var helperButtons = new HelperButtons()
            {
                Y = Pos.Bottom(tableView)
            };
            
            viewWrapper.Add(helperButtons);
            
            // Create action to create new and remove rows
            var actions = new List<MenuItem>();
            actions.Add(new MenuItem("New Row", "", () =>
            {
                var list = items.ToList();
                if (isComplicatedType)
                {
                    Type type = (annotation.Get<IReflectionAnnotation>().ReflectionInfo as TypeData).Load();
                    var win = new NewPluginWindow(TypeData.FromType(GetEnumerableElementType(type)), "Add Element", null);
                    Application.Run(win);
                    if (win.PluginType == null)
                        return;

                    try
                    {
                        var instance = win.PluginType.CreateInstance();
                        list.Add(AnnotationCollection.Annotate(instance));
                    }
                    catch
                    {
                        return;
                    }
                }
                else
                {
                    list.Add(collectionAnnotation.NewElement());
                }
                collectionAnnotation.AnnotatedElements = list;
                annotation.Write();
                annotation.Read();
                items = collectionAnnotation.AnnotatedElements.ToArray();
                (Columns, Headers, isComplicatedType) = getColumnNames(annotation, items);

                // Refresh table
                table = LoadTable(Headers, Columns, items);
                tableView.Table = table;
                tableView.Update();
                helperButtons.SetActions(actions, viewWrapper);
                Application.Refresh();
            }, () => fixedSize == false, shortcut: KeyMapHelper.GetShortcutKey(KeyTypes.TableAddRow)));
            actions.Add(new MenuItem("Remove Row", "", () =>
            {
                var index = tableView.SelectedRow;
                var list = items.ToList();
                list.RemoveAt(index);
                collectionAnnotation.AnnotatedElements = list;
                items = collectionAnnotation.AnnotatedElements.ToArray();

                // Refresh table
                table = LoadTable(Headers, Columns, items);
                tableView.Table = table;
                tableView.Update();
                helperButtons.SetActions(actions, viewWrapper);
                Application.Refresh();
            }, () => tableView.SelectedRow >= 0 && tableView.SelectedRow < items.Length && fixedSize == false, shortcut: KeyMapHelper.GetShortcutKey(KeyTypes.TableRemoveRow)));
            helperButtons.SetActions(actions, viewWrapper);
            
            return viewWrapper;
        }

        DataTable LoadTable(List<string> Headers, List<string> Columns, AnnotationCollection[] items)
        {
            var table = new DataTable();
            table.Columns.AddRange(Headers.Select(c => new DataColumn(c)).ToArray());

            foreach (var item in items)
            {
                var row = table.NewRow();
                for (int i = 0; i < Columns.Count; i++)
                {
                    var members = item.Get<IMembersAnnotation>()?.Members;
                    AnnotationCollection cell;
                    if (members == null) cell = item;
                    else cell = members.FirstOrDefault(x2 => calcMemberName(x2.Get<IMemberAnnotation>().Member) == Columns[i]);

                    var cellValue = cell?.Get<IStringReadOnlyValueAnnotation>()?.Value ?? cell?.Get<IObjectValueAnnotation>().Value?.ToString();

                    if (string.IsNullOrEmpty(cellValue))
                        row[i] = DBNull.Value;
                    else
                        row[i] = cellValue;
                }

                table.Rows.Add(row);
            }

            return table;
        }
        
        public static Type GetEnumerableElementType(System.Type enumType)
        {
            if (enumType.IsArray)
                return enumType.GetElementType();

            var ienumInterface = enumType.GetInterface("IEnumerable`1") ?? enumType;
            if (ienumInterface != null)
                return ienumInterface.GetGenericArguments().FirstOrDefault();

            return typeof(object);
        }

    }

    public class FixedSizeAttribute : Attribute
    {
        
    }
}