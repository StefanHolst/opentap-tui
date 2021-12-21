using System;
using System.Xml.Linq;
using Terminal.Gui;

namespace OpenTap.Tui
{
    public class KeyEventSerializer : TapSerializerPlugin
    {
        public override bool Deserialize(XElement node, ITypeData t, Action<object> setter)
        {
            if (t is TypeData t2 && t2.Type == typeof(KeyEvent))
            {
                var keyNode = node.Element("Key");
                var modifierNode = node.Element("Modifiers");

                if (keyNode?.Value == null || modifierNode?.Value == null)
                    return false;
                
                var keyValue = (Key)Enum.Parse(typeof(Key), keyNode.Value);
                var modifierValue = toModifiers(modifierNode.Value);

                setter(new KeyEvent(keyValue, modifierValue));
                
                return true;
            }

            return false;
        }

        public override bool Serialize(XElement node, object obj, ITypeData _expectedType)
        {
            if (_expectedType is TypeData expectedType2 && expectedType2.Type is Type expectedType && obj is KeyEvent keyEvent)
            {
                var key = new XElement("Key");
                var modifier = new XElement("Modifiers");
                bool keyok = Serializer.Serialize(key, keyEvent.Key, TypeData.FromType(typeof(Key)));
                modifier.Value = fromModifiers(keyEvent);
                
                if (!keyok)
                    return false;
                node.Add(key);
                node.Add(modifier);
                return true;
            }
            
            return false;
        }

        public double Order => 0;

        public static string fromModifiers(KeyEvent keyEvent)
        {
            var bits = new char[6];
            bits[0] = keyEvent.IsShift ? '1' : '0';
            bits[1] = keyEvent.IsAlt ? '1' : '0';
            bits[2] = keyEvent.IsCtrl ? '1' : '0';
            bits[3] = keyEvent.IsCapslock ? '1' : '0';
            bits[4] = keyEvent.IsNumlock ? '1' : '0';
            bits[5] = keyEvent.IsScrolllock ? '1' : '0';
            return new string(bits);
        }

        public static KeyModifiers toModifiers(string bits)
        {
            var modifiers = new KeyModifiers();
            modifiers.Shift = bits[0] == '1';
            modifiers.Alt = bits[1] == '1';
            modifiers.Ctrl = bits[2] == '1';
            modifiers.Capslock = bits[3] == '1';
            modifiers.Numlock = bits[4] == '1';
            modifiers.Scrolllock = bits[5] == '1';
            return modifiers;
        }
    }
}