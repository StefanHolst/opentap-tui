using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using OpenTap.Tui.PropEditProviders;
using OpenTap.Tui.Views;
using Terminal.Gui;

namespace OpenTap.Tui
{
    [Display("TUI Settings")]
    public class TuiSettings : ComponentSettings<TuiSettings>
    {
        private Theme theme;
        [Display("Color Theme", Group: "Colors", Order: 0)]
        public Theme Theme
        {
            get { return theme; }
            set
            {
                theme = value;
                SetTheme();
                LoadSettings();
                OnPropertyChanged(nameof(Theme));
            }
        }
        
        [Display("Base Color", Group: "Colors", Order: 1)]
        public ColorSchemeViewmodel BaseColor
        {
            get { return baseColor; }
            set
            {
                baseColor = value;
                OnPropertyChanged("Color");
            }
        }
        private ColorSchemeViewmodel baseColor;
        
        [Display("Dialog Color", Group: "Colors", Order: 1)]
        public ColorSchemeViewmodel DialogColor
        {
            get { return dialogColor; }
            set
            {
                dialogColor = value;
                OnPropertyChanged("Color");
            }
        }
        private ColorSchemeViewmodel dialogColor;
        
        [Display("Error Color", Group: "Colors", Order: 1)]
        public ColorSchemeViewmodel ErrorColor
        {
            get { return errorColor; }
            set
            {
                errorColor = value;
                OnPropertyChanged("Color");
            }
        }
        private ColorSchemeViewmodel errorColor;

        [Display("Menu Color", Group: "Colors", Order: 1)]
        public ColorSchemeViewmodel MenuColor
        {
            get { return menuColor; }
            set
            {
                menuColor = value;
                OnPropertyChanged("Color");
            }
        }
        private ColorSchemeViewmodel menuColor;

        [Display("Use Log Level Colors", Group: "Colors", Order: 2)]
        public bool UseLogColors { get; set; } = true;

        [Browsable(true)]
        [XmlIgnore]
        [Display("Restore Colors", Group: "Colors", Order: 3)]
        public Action Reset { get; set; }

        private void SetTheme()
        {
            switch (Theme)
            {
                case Theme.Dark:
                {
                    baseColor = new ColorSchemeViewmodel(Color.Gray, Color.Black, Color.White, Color.DarkGray, Color.Gray, Color.Black, Color.Gray, Color.Black);
                    dialogColor = new ColorSchemeViewmodel(Color.White, Color.DarkGray, Color.Black, Color.Gray, Color.BrightRed, Color.Gray, Color.BrightRed, Color.DarkGray);
                    menuColor = new ColorSchemeViewmodel(Color.White, Color.DarkGray, Color.Black, Color.Gray, Color.BrightRed, Color.Gray, Color.BrightRed, Color.DarkGray);
                    errorColor = new ColorSchemeViewmodel(Color.Red, Color.DarkGray, Color.Black, Color.Gray, Color.BrightRed, Color.Gray, Color.BrightRed, Color.DarkGray);
                    break;
                }
                case Theme.Light:
                {
                    baseColor = new ColorSchemeViewmodel(Color.Black, Color.White, Color.Black, Color.Gray, Color.Black, Color.Gray, Color.Black, Color.Gray);
                    dialogColor = new ColorSchemeViewmodel(Color.Black, Color.Gray, Color.Black, Color.White, Color.BrightRed, Color.White, Color.BrightRed, Color.Gray);
                    menuColor = new ColorSchemeViewmodel(Color.Black, Color.Gray, Color.Black, Color.White, Color.BrightRed, Color.White, Color.BrightRed, Color.Gray);
                    errorColor = new ColorSchemeViewmodel(Color.Red, Color.Gray, Color.Black, Color.White, Color.BrightRed, Color.White, Color.BrightRed, Color.Gray);
                    break;
                }
                case Theme.Blue:
                {
                    baseColor = new ColorSchemeViewmodel(Color.White, Color.Blue, Color.Black, Color.Gray, Color.Blue, Color.Gray, Color.Cyan, Color.Blue);
                    dialogColor = new ColorSchemeViewmodel(Color.Black, Color.Gray, Color.Black, Color.DarkGray, Color.Blue, Color.DarkGray, Color.Blue, Color.Gray);
                    menuColor = new ColorSchemeViewmodel(Color.White, Color.DarkGray, Color.White, Color.Black, Color.BrightYellow, Color.Black, Color.BrightYellow, Color.DarkGray);
                    errorColor = new ColorSchemeViewmodel(Color.Red, Color.White, Color.White, Color.Red, Color.Black, Color.Red, Color.Black, Color.White);
                    break;
                }
                case Theme.Gray:
                {
                    baseColor = new ColorSchemeViewmodel(Color.White, Color.DarkGray, Color.Black, Color.Gray, Color.Black, Color.Gray, Color.Black, Color.Gray);
                    dialogColor = new ColorSchemeViewmodel(Color.Black, Color.Gray, Color.Black, Color.White, Color.BrightRed, Color.White, Color.BrightRed, Color.Gray);
                    menuColor = new ColorSchemeViewmodel(Color.Black, Color.Gray, Color.Black, Color.White, Color.BrightRed, Color.White, Color.BrightRed, Color.Gray);
                    errorColor = new ColorSchemeViewmodel(Color.Red, Color.Gray, Color.Black, Color.White, Color.BrightRed, Color.White, Color.BrightRed, Color.Gray);
                    break;
                }
                case Theme.Hacker:
                case Theme.Default:
                {
                    baseColor = new ColorSchemeViewmodel(Color.BrightGreen, Color.Black, Color.BrightRed, Color.Black, Color.Cyan, Color.Black, Color.Cyan, Color.Black);
                    dialogColor = new ColorSchemeViewmodel(Color.BrightGreen, Color.Black, Color.BrightRed, Color.Black, Color.Cyan, Color.Black, Color.Cyan, Color.Black);
                    menuColor = new ColorSchemeViewmodel(Color.Cyan, Color.Black, Color.BrightRed, Color.Black, Color.BrightGreen, Color.Black, Color.BrightGreen, Color.Black);
                    errorColor = new ColorSchemeViewmodel(Color.BrightRed, Color.Black, Color.Red, Color.Black, Color.Cyan, Color.Black, Color.Cyan, Color.Black);
                    break;
                }
            }
        }

        [Browsable(false)]
        [Display("", "", "")]
        public FocusModeUnlocks FocusModeProgress { get; set; }

        
        private int testPlanGridWidth = 75;
        [Unit("%")]
        [Display("Width", "The relative width of the Test Plan Grid. The Settings Panel will use the remaining space.", "Test Plan Panel Size", Order: 4)]
        public int TestPlanGridWidth
        {
            get => testPlanGridWidth;
            set
            {
                if (value >= 15 && value <= 85)
                {
                    testPlanGridWidth = value;
                    OnPropertyChanged("Size");
                }
            }
        }

        private int testPlanGridHeight = 70;
        [Unit("%")]
        [Display("Height", "The relative height of the Test Plan Grid. The Log Panel will use the remaining space.", "Test Plan Panel Size", Order: 4)]
        public int TestPlanGridHeight
        {
            get => testPlanGridHeight;
            set
            {
                if (value >= 15 && value <= 85)
                {
                    testPlanGridHeight = value;
                    OnPropertyChanged("Size");
                }
            }
        }

        [Browsable(true)]
        [XmlIgnore]
        [Display("Reset Size", "Restore the default size of all panels.", "Test Plan Panel Size", Order: 5)]
        public Action ResetSize { get; set; }

        [Display("Map", Group: "Key Mapping")]
        public List<KeyMap> KeyMap { get; set; } = new List<KeyMap>();

        [Browsable(true)]
        [XmlIgnore]
        [Display("Restore Key Map", Group: "Key Mapping")]
        public Action ResetKeyMapping { get; set; }


        [Display("Scrollback Limit", Group: "Log Panel")]
        public Enabled<int> LogScrollbackLimit { get; set; } = new Enabled<int>() { IsEnabled = true, Value = 100000 };

        [Display("Clear on Run", Group: "Log Panel")]
        public bool ClearOnRun { get; set; }

        public TuiSettings()
        {
            // make sure a default theme is configured.
            // normally this will be overwritten when the 
            // settings are loaded from XML.
            Theme = Theme.Default;
            KeyMap = KeyMapHelper.DefaultKeys;
            SetTheme();
            Reset += () =>
            {
                Theme = Theme.Default;
                SetTheme();
            };
            ResetSize += () =>
            {
                TestPlanGridHeight = 70;
                TestPlanGridWidth = 75;
            };
            ResetKeyMapping += () =>
            {
                KeyMap = KeyMapHelper.DefaultKeys;
            };
        }

        public void LoadSettings()
        {
            if (baseColor == null)
                SetTheme();
            
            Colors.Base = BaseColor.ToColorScheme();
            Colors.Dialog = DialogColor.ToColorScheme();
            Colors.Error = ErrorColor.ToColorScheme();
            Colors.Menu = MenuColor.ToColorScheme();
            // Colors.TopLevel = baseColor.ToColorScheme();
            
            Application.RefreshColorSchemes();
        }
    }

    public enum Theme
    {
        Default,
        Gray,
        Blue,
        Dark,
        Light,
        // This has been changed to the default theme. It should no longer be selectable, but
        // if someone updates the TUI and their previously selected theme was 'Hacker',
        // it should still deserialize without error.
        [Browsable(false)]
        Hacker
    }
    
    public class ColorSchemeViewmodel
    {
        public override string ToString()
        {
            return $"{NormalForeground} | {NormalBackground} | {FocusForeground} | {FocusBackground} | {HotNormalForeground} | {HotNormalBackground} | {HotFocusForeground} | {HotFocusBackground}";
        }

        public ColorSchemeViewmodel()
        {
            
        }

        public ColorSchemeViewmodel(Color NormalForeground, Color NormalBackground, Color FocusForeground, Color FocusBackground, Color HotFocusForeground, Color HotFocusBackground, Color HotNormalForeground, Color HotNormalBackground)
        {
            this.NormalForeground = NormalForeground;
            this.NormalBackground = NormalBackground;
            this.HotNormalForeground = HotNormalForeground;
            this.HotNormalBackground = HotNormalBackground;
            this.FocusForeground = FocusForeground;
            this.FocusBackground = FocusBackground;
            this.HotFocusForeground = HotFocusForeground;
            this.HotFocusBackground = HotFocusBackground;
        }

        public ColorScheme ToColorScheme()
        {
            if (Application.Driver == null)
                return new ColorScheme();
            return new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(NormalForeground, NormalBackground),
                Focus = Application.Driver.MakeAttribute(FocusForeground, FocusBackground),
                HotNormal = Application.Driver.MakeAttribute(HotNormalForeground, HotNormalBackground),
                HotFocus = Application.Driver.MakeAttribute(HotFocusForeground, HotFocusBackground)
            };
        }

        [Display("Normal Foreground")]
        public Color NormalForeground { get; set; }

        [Display("Normal Background")]
        public Color NormalBackground { get; set; }
        
        [Display("Focus Foreground")]
        public Color FocusForeground { get; set; }
        [Display("Focus Background")]
        public Color FocusBackground { get; set; }
        
        [Display("HotNormal Foreground")]
        public Color HotNormalForeground { get; set; }
        [Display("HotNormal Background")]
        public Color HotNormalBackground { get; set; }
        
        [Display("HotFocus Foreground")]
        public Color HotFocusForeground { get; set; }
        [Display("HotFocus Background")]
        public Color HotFocusBackground { get; set; }
    }
}