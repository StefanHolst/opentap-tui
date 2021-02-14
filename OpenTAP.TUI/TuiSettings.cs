using System;
using System.ComponentModel;
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

        [Display("Restore Colors", Group: "Colors", Order: 2)]
        public Action Reset { get; set; }

        private void SetTheme()
        {
            switch (Theme)
            {
                case Theme.Dark:
                {
                    baseColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.Gray, Color.Black),
                        Focus = Application.Driver.MakeAttribute(Color.White, Color.DarkGray),
                        HotFocus = Application.Driver.MakeAttribute(Color.Gray, Color.Black),
                        HotNormal = Application.Driver.MakeAttribute(Color.Gray, Color.Black)
                    });
                    dialogColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray),
                        Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                        HotFocus = Application.Driver.MakeAttribute(Color.BrightRed, Color.Gray),
                        HotNormal = Application.Driver.MakeAttribute(Color.BrightRed, Color.DarkGray)
                    });
                    menuColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray),
                        Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                        HotFocus = Application.Driver.MakeAttribute(Color.BrightRed, Color.Gray),
                        HotNormal = Application.Driver.MakeAttribute(Color.BrightRed, Color.DarkGray)
                    });
                    errorColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.Red, Color.DarkGray),
                        Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                        HotFocus = Application.Driver.MakeAttribute(Color.BrightRed, Color.Gray),
                        HotNormal = Application.Driver.MakeAttribute(Color.BrightRed, Color.DarkGray)
                    });
                    break;
                }
                case Theme.Light:
                {
                    baseColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.Black, Color.White),
                        Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                        HotFocus = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                        HotNormal = Application.Driver.MakeAttribute(Color.Black, Color.Gray)
                    });
                    dialogColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                        Focus = Application.Driver.MakeAttribute(Color.Black, Color.White),
                        HotFocus = Application.Driver.MakeAttribute(Color.BrightRed, Color.White),
                        HotNormal = Application.Driver.MakeAttribute(Color.BrightRed, Color.Gray)
                    });
                    menuColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                        Focus = Application.Driver.MakeAttribute(Color.Black, Color.White),
                        HotFocus = Application.Driver.MakeAttribute(Color.BrightRed, Color.White),
                        HotNormal = Application.Driver.MakeAttribute(Color.BrightRed, Color.Gray)
                    });
                    errorColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.Red, Color.Gray),
                        Focus = Application.Driver.MakeAttribute(Color.Black, Color.White),
                        HotFocus = Application.Driver.MakeAttribute(Color.BrightRed, Color.White),
                        HotNormal = Application.Driver.MakeAttribute(Color.BrightRed, Color.Gray)
                    });
                    break;
                }
                case Theme.Blue:
                {
                    baseColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.White, Color.Blue),
                        Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                        HotFocus = Application.Driver.MakeAttribute(Color.Blue, Color.Gray),
                        HotNormal = Application.Driver.MakeAttribute(Color.Cyan, Color.Blue)
                    });
                    dialogColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                        Focus = Application.Driver.MakeAttribute(Color.Black, Color.DarkGray),
                        HotFocus = Application.Driver.MakeAttribute(Color.Blue, Color.DarkGray),
                        HotNormal = Application.Driver.MakeAttribute(Color.Blue, Color.Gray)
                    });
                    menuColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray),
                        Focus = Application.Driver.MakeAttribute(Color.White, Color.Black),
                        HotFocus = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black),
                        HotNormal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.DarkGray)
                    });
                    errorColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.Red, Color.White),
                        Focus = Application.Driver.MakeAttribute(Color.White, Color.Red),
                        HotFocus = Application.Driver.MakeAttribute(Color.Black, Color.Red),
                        HotNormal = Application.Driver.MakeAttribute(Color.Black, Color.White)
                    });
                    break;
                }
                case Theme.Hacker:
                {
                    baseColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black),
                        Focus = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black),
                        HotFocus = Application.Driver.MakeAttribute(Color.Cyan, Color.Black),
                        HotNormal = Application.Driver.MakeAttribute(Color.Cyan, Color.Black)
                    });
                    dialogColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black),
                        Focus = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black),
                        HotFocus = Application.Driver.MakeAttribute(Color.Cyan, Color.Black),
                        HotNormal = Application.Driver.MakeAttribute(Color.Cyan, Color.Black)
                    });
                    menuColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.Cyan, Color.Black),
                        Focus = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black),
                        HotFocus = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black),
                        HotNormal = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black)
                    });
                    errorColor = new ColorSchemeViewmodel(new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black),
                        Focus = Application.Driver.MakeAttribute(Color.Red, Color.Black),
                        HotFocus = Application.Driver.MakeAttribute(Color.Cyan, Color.Black),
                        HotNormal = Application.Driver.MakeAttribute(Color.Cyan, Color.Black)
                    });
                    break;
                }
                case Theme.Default:
                    baseColor = null;
                    dialogColor = null;
                    menuColor = null;
                    errorColor = null;
                    SetDefaultColors();
                    break;
            }
        }

        public TuiSettings()
        {
            Reset += () =>
            {
                baseColor = null;
                dialogColor = null;
                errorColor = null;
                menuColor = null;
                theme = Theme.Default;
                
                SetDefaultColors();
            };
        }

        void SetDefaultColors()
        {
            if (baseColor == null)
            {
                baseColor = new ColorSchemeViewmodel(new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray),
                    Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                    HotFocus = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                    HotNormal = Application.Driver.MakeAttribute(Color.Black, Color.Gray)
                });
            }
            if (dialogColor == null)
            {
                dialogColor = new ColorSchemeViewmodel(new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                    Focus = Application.Driver.MakeAttribute(Color.Black, Color.White),
                    HotFocus = Application.Driver.MakeAttribute(Color.BrightRed, Color.White),
                    HotNormal = Application.Driver.MakeAttribute(Color.BrightRed, Color.Gray)
                });
            }
            if (menuColor == null)
            {
                menuColor = new ColorSchemeViewmodel(new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                    Focus = Application.Driver.MakeAttribute(Color.Black, Color.White),
                    HotFocus = Application.Driver.MakeAttribute(Color.BrightRed, Color.White),
                    HotNormal = Application.Driver.MakeAttribute(Color.BrightRed, Color.Gray)
                });
            }
            if (errorColor == null)
            {
                errorColor = new ColorSchemeViewmodel(new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.Red, Color.Gray),
                    Focus = Application.Driver.MakeAttribute(Color.Black, Color.White),
                    HotFocus = Application.Driver.MakeAttribute(Color.BrightRed, Color.White),
                    HotNormal = Application.Driver.MakeAttribute(Color.BrightRed, Color.Gray)
                });
            }
        }

        public void LoadSettings()
        {
            SetDefaultColors();
            
            Colors.Base = BaseColor.ToColorScheme();
            Colors.Dialog = DialogColor.ToColorScheme();
            Colors.Error = ErrorColor.ToColorScheme();
            Colors.Menu = MenuColor.ToColorScheme();
            
            foreach (var view in Application.Top.Subviews)
            {
                if (view is Toplevel)
                    view.ColorScheme = Colors.Base;
                if (view is MenuBar)
                    view.ColorScheme = Colors.Menu;
            }
            
            Application.Refresh();
        }
    }

    public enum Theme
    {
        Default,
        Blue,
        Dark,
        Light,
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
        
        public ColorSchemeViewmodel(ColorScheme colorScheme)
        {
            if (colorScheme == null)
                colorScheme = new ColorScheme();
            
            NormalForeground = colorScheme.Normal.Foreground;
            NormalBackground = colorScheme.Normal.Background;

            FocusForeground = colorScheme.Focus.Foreground;
            FocusBackground = colorScheme.Focus.Background;
            
            HotNormalForeground = colorScheme.HotNormal.Foreground;
            HotNormalBackground = colorScheme.HotNormal.Background;
            
            HotFocusForeground = colorScheme.HotFocus.Foreground;
            HotFocusBackground = colorScheme.HotFocus.Background;
        }

        public ColorScheme ToColorScheme()
        {
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