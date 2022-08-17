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
        AddNewStep,
        InsertNewStep,
        Copy,
        Paste
    }

    public static class KeyMapHelper
    {
        public static List<KeyMap> DefaultKeys = new[]
        {
            new KeyMap(new KeyEvent(Key.S | Key.CtrlMask, new KeyModifiers() { Ctrl = true }), KeyTypes.Save),
            new KeyMap(new KeyEvent(Key.S | Key.CtrlMask, new KeyModifiers() { Ctrl = true, Shift = true}), KeyTypes.SaveAs),
            new KeyMap(new KeyEvent(Key.O | Key.CtrlMask, new KeyModifiers() { Ctrl = true }), KeyTypes.Open),
            new KeyMap(new KeyEvent(Key.T | Key.CtrlMask, new KeyModifiers() { Ctrl = true }), KeyTypes.AddNewStep),
            new KeyMap(new KeyEvent(Key.T | Key.CtrlMask | Key.ShiftMask, new KeyModifiers() { Ctrl = true, Shift = true}), KeyTypes.InsertNewStep),
            new KeyMap(new KeyEvent(Key.C, new KeyModifiers() { Shift = true}), KeyTypes.Copy),
            new KeyMap(new KeyEvent(Key.V, new KeyModifiers() { Shift = true}), KeyTypes.Paste),
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

        public override string ToString()
        {
            return KeyEvent.ToString();
        }
    }
}