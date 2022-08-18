using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace OpenTap.Tui
{
    public enum KeyTypes
    {
        Save,
        SaveAs,
        Open,
        Copy,
        Paste,
        Select,
        Cancel,
        Close,
        AddNewStep,
        InsertNewStep,
        DeleteStep,
        SwapView,
        FocusTestPlan,
        FocusStepSettings,
        FocusDescription,
        FocusLog,
        RunTestPlan,
        TestPlanSettings,
        TableAddRow,
        TableRemoveRow,
        StringEditorInsertFilePath,
        HelperButton1,
        HelperButton2,
        HelperButton3,
        HelperButton4,
        HelperButton5,
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
            new KeyMap(KeyTypes.Close, Key.X, ctrl: true, shift: false, alt: false),
            new KeyMap(KeyTypes.Close, Key.C, ctrl: true, shift: false, alt: false),
            new KeyMap(KeyTypes.AddNewStep, Key.T, ctrl: true, shift: false, alt: false),
            new KeyMap(KeyTypes.InsertNewStep, Key.T, ctrl: true, shift: true, alt: false),
            new KeyMap(KeyTypes.DeleteStep, Key.Backspace, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.DeleteStep, Key.DeleteChar, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.SwapView, Key.Tab, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.SwapView, Key.BackTab, ctrl: false, shift: true, alt: false),
            new KeyMap(KeyTypes.FocusTestPlan, Key.F1, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.FocusStepSettings, Key.F2, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.FocusDescription, Key.F3, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.FocusLog, Key.F4, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.RunTestPlan, Key.F5, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.TestPlanSettings, Key.F8, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.TableAddRow, Key.F1, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.TableRemoveRow, Key.F2, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.StringEditorInsertFilePath, Key.F1, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.StringEditorInsertFilePath, Key.O, ctrl: false, shift: true, alt: false),
            new KeyMap(KeyTypes.HelperButton1, Key.F5, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.HelperButton2, Key.F6, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.HelperButton3, Key.F7, ctrl: false, shift: false, alt: false),
            new KeyMap(KeyTypes.HelperButton4, Key.F8, ctrl: false, shift: false, alt: false),
        }.ToList();

        public static KeyMap GetFirstKeymap(KeyTypes keyType)
        {
            return TuiSettings.Current.KeyMap.FirstOrDefault(k => k.KeyType == keyType);
        }

        public static Key GetShortcutKey(KeyTypes keyType)
        {
            return GetFirstKeymap(keyType).KeyEvent.Key;
        }
        
        public static bool IsKey(KeyEvent keyEvent, KeyTypes keyType)
        {
            var maps = TuiSettings.Current.KeyMap.Where(k => k.KeyType == keyType);

            foreach (var map in maps)
            {
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
            s.Add(key.ToString());
            return string.Join("-", s);
        }
    }

    public class KeyMap
    {
        public KeyTypes KeyType { get; set; }
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