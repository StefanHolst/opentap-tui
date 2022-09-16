using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace OpenTap.Tui
{
    public enum KeyTypes
    {
        Save,
        [Display("Save As")]
        SaveAs,
        Open,
        Copy,
        Paste,
        Select,
        Cancel,
        Close,
        Kill,
        [Display("Swap Selected View")]
        SwapView,
        [Display("Swap Selected View Backwards")]
        SwapViewBack,
        [Display("Focus Test Plan view")]
        FocusTestPlan,
        [Display("Focus Settings view")]
        FocusStepSettings,
        [Display("Focus Description box")]
        FocusDescription,
        [Display("Focus Log view")]
        FocusLog,
        [Display("Focus Menu bar")]
        FocusMenu,
        [Display("Open Help menu")]
        Help,
        [Display("Run Test Plan")]
        RunTestPlan,
        [Display("Test Plan Settings")]
        TestPlanSettings,
        [Display("Table - New Row")]
        TableAddRow,
        [Display("Table - Remove Row")]
        TableRemoveRow,
        [Display("Test Plan - Insert New Step")]
        AddNewStep,
        [Display("Test Plan - Insert New Step Child")]
        InsertNewStep,
        [Display("Test Plan - Delete Step")]
        DeleteStep,
        [Display("String Editor - Insert File Path")]
        StringEditorInsertFilePath,
        [Display("Helper Menu - Button 1")]
        HelperButton1,
        [Display("Helper Menu - Button 2")]
        HelperButton2,
        [Display("Helper Menu - Button 3")]
        HelperButton3,
        [Display("Helper Menu - Button 4")]
        HelperButton4,
        [Display("Helper Menu - Button 5")]
        HelperButton5,
        [Display("Select Step")]
        SelectStep,
        [Display("Insert Selected Steps")]
        InsertSelectedSteps,
        [Display("Insert Selected Steps As Children")]
        InsertSelectedStepsAsChildren,
    }

    public static class KeyMapHelper
    {
        public static List<KeyMap> DefaultKeys = new[]
        {
            new KeyMap(KeyTypes.Save, Key.S, ctrl: true, shift: false, alt: false),
            new KeyMap(KeyTypes.SaveAs, Key.S, ctrl: true, shift: true, alt: false),
            new KeyMap(KeyTypes.Open, Key.O, ctrl: true, shift: false, alt: false),
            new KeyMap(KeyTypes.Copy, Key.C, ctrl: false, shift: true, alt: false),
            new KeyMap(KeyTypes.Paste, Key.V, ctrl: false, shift: true, alt: false),
            new KeyMap(KeyTypes.Select, Key.Enter, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.Cancel, Key.Esc, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.Close, Key.Esc, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.Close, Key.C, ctrl: true, shift: false, alt: false),
            new KeyMap(KeyTypes.Kill, Key.Q, ctrl: true, shift: false, alt: false),
            new KeyMap(KeyTypes.SwapView, Key.Tab, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.SwapViewBack, Key.Tab, ctrl: false, shift: true, alt: false),
            new KeyMap(KeyTypes.SwapViewBack, Key.BackTab, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.FocusTestPlan, Key.F6, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.FocusStepSettings, Key.F7, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.FocusLog, Key.F8, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.FocusMenu, Key.F9, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.FocusDescription, Key.Null, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.Help, Key.F12, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.RunTestPlan, Key.F5, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.TestPlanSettings, Key.F1, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.TableAddRow, Key.N, ctrl: true, shift: false, alt: false),
            new KeyMap(KeyTypes.TableRemoveRow, Key.DeleteChar, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.AddNewStep, Key.F2, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.InsertNewStep, Key.F3, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.DeleteStep, Key.DeleteChar, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.StringEditorInsertFilePath, Key.O, ctrl: true, shift: false, alt: false),
            new KeyMap(KeyTypes.HelperButton1, Key.F1, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.HelperButton2, Key.F2, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.HelperButton3, Key.F3, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.HelperButton4, Key.F4, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.SelectStep, Key.Space, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.InsertSelectedSteps, Key.F2, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.InsertSelectedStepsAsChildren, Key.F3, ctrl: false, shift: false, alt: false),
        }.ToList();

        public static KeyMap GetFirstKeymap(KeyTypes keyType)
        {
            return TuiSettings.Current.KeyMap.FirstOrDefault(k => k.KeyType == keyType);
        }

        public static Key GetShortcutKey(KeyTypes keyType)
        {
            return GetFirstKeymap(keyType)?.KeyEvent.Key ?? Key.Null;
        }

        public static string GetKeyName(KeyTypes keyType)
        {
            return KeyToString(GetShortcutKey(keyType));
        }


        public static string GetKeyName(KeyTypes keyType, string name)
        {
            Key shortcut = GetShortcutKey(keyType);
            if (shortcut != Key.Null)
                return $"[ {KeyToString(shortcut)} {name} ]";
            return name;
        }

        public static bool IsKey(KeyEvent keyEvent, KeyTypes keyType)
        {
            var maps = TuiSettings.Current.KeyMap.Where(k => k.KeyType == keyType);

            foreach (var map in maps)
            {
                if (keyEvent.IsShift == map.KeyEvent.IsShift && (keyEvent.Key | Key.ShiftMask) == map.KeyEvent.Key)
                    return true;
                if (keyEvent.IsCtrl && keyEvent.Key != (map.KeyEvent.Key | Key.CtrlMask) && keyEvent.Key != map.KeyEvent.Key)
                    continue;
                if (keyEvent.Key != map.KeyEvent.Key)
                    continue;

                if (keyEvent.IsAlt == map.KeyEvent.IsAlt &&
                       keyEvent.IsCtrl == map.KeyEvent.IsCtrl &&
                       keyEvent.IsShift == map.KeyEvent.IsShift)
                    return true;
            }
            return false;
        }

        public static string KeyToString(Key key)
        {
            List<string> s = new List<string>();
            if (key.HasFlag(Key.CtrlMask))
            {
                s.Add("Ctrl");
                key -= Key.CtrlMask;
            }
            if (key.HasFlag(Key.ShiftMask))
            {
                s.Add("Shift");
                key -= Key.ShiftMask;
            }
            if (key.HasFlag(Key.AltMask))
            {
                s.Add("Alt");
                key -= Key.AltMask;
            }
            if (key.HasFlag(Key.SpecialMask))
            {
                s.Add("Win");
                key -= Key.SpecialMask;
            }
            if (key != Key.Null)
            {
                switch(key)
                {
                    case Key.Backspace:
                        s.Add("<--");
                        break;
                    case Key.Delete:
                    case Key.DeleteChar:
                        s.Add("Del");
                        break;
                    default:
                        s.Add(key.ToString());
                        break;
                }
            }
            return string.Join("-", s);
        }
    }

    public class KeyMap
    {
        [Display("Key Type", Order: 0)]
        public KeyTypes KeyType { get; set; }
        [Display("Key Event", Order: 100)]
        public KeyEvent KeyEvent { get; set; }

        public KeyMap()
        {
            KeyType = KeyTypes.Open;
            KeyEvent = new KeyEvent();
        }

        public KeyMap(KeyEvent keyEvent, KeyTypes type)
        {
            KeyType = type;
            KeyEvent = keyEvent;
        }

        public KeyMap(Key key, KeyTypes type)
        {
            KeyType = type;
            KeyEvent = new KeyEvent(key, 
                new KeyModifiers() { 
                    Alt = key.HasFlag(Key.AltMask), 
                    Shift = key.HasFlag(Key.ShiftMask), 
                    Ctrl = key.HasFlag(Key.CtrlMask),
                });
        }

        public KeyMap(KeyTypes type, Key key, bool ctrl, bool shift, bool alt) :
            this(key | (ctrl ? Key.CtrlMask : 0) | (shift ? Key.ShiftMask : 0) | (alt ? Key.AltMask : 0), type)
        {

        }

        public override string ToString()
        {
            return KeyEvent.ToString();
        }
    }
}