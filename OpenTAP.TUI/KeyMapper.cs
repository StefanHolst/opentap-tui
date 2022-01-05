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
            new KeyMap(new KeyEvent(Key.S, new KeyModifiers() { Ctrl = true }), KeyTypes.Save),
            new KeyMap(new KeyEvent(Key.S, new KeyModifiers() { Ctrl = true, Shift = true}), KeyTypes.SaveAs),
            new KeyMap(new KeyEvent(Key.O, new KeyModifiers() { Ctrl = true }), KeyTypes.Open),
            new KeyMap(new KeyEvent(Key.T, new KeyModifiers() { Ctrl = true }), KeyTypes.AddNewStep),
            new KeyMap(new KeyEvent(Key.T, new KeyModifiers() { Ctrl = true, Shift = true}), KeyTypes.InsertNewStep),
            new KeyMap(new KeyEvent(Key.C, new KeyModifiers() { Shift = true}), KeyTypes.Copy),
            new KeyMap(new KeyEvent(Key.V, new KeyModifiers() { Shift = true}), KeyTypes.Paste),
        }.ToList();
        
        public static bool IsKey(KeyEvent keyEvent, KeyTypes keyType)
        {
            var map = TuiSettings.Current.KeyMap.FirstOrDefault(k => k.KeyType == keyType);
            if (map == null)
                return false;

            var cleanedKeyEvent = keyEvent.Key;
            if ((keyEvent.Key & Key.CtrlMask) == Key.CtrlMask)
                cleanedKeyEvent ^= Key.CtrlMask;
            if ((keyEvent.Key & Key.ShiftMask) == Key.ShiftMask)
                cleanedKeyEvent ^= Key.ShiftMask;
            if ((keyEvent.Key & Key.AltMask) == Key.AltMask)
                cleanedKeyEvent ^= Key.AltMask;
            
            if (cleanedKeyEvent != map.KeyEvent.Key)
                return false;
            
            return keyEvent.IsAlt == map.KeyEvent.IsAlt &&
                   keyEvent.IsCtrl == map.KeyEvent.IsCtrl &&
                   keyEvent.IsShift == map.KeyEvent.IsShift;
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
    }
}