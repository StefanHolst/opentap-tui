using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using NStack;
using OpenTap.Tui.Windows;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    static class FocusMode
    {
        public static void StartFocusMode(FocusModeUnlocks unlock, bool useLogMessage)
        {

            if (TuiSettings.Current.FocusModeProgress.HasFlag(unlock) == false)
            {
                TuiSettings.Current.FocusModeProgress |= unlock;
                TuiSettings.Current.Save();
                if (useLogMessage)
                {
                    AnnotationCollection annotations = AnnotationCollection.Annotate(unlock);
                    TUI.Log.Debug($"You found an easteregg unlock: {annotations.Get<IStringValueAnnotation>().Value}");
                    return;
                }
            }
            else if (useLogMessage)
                return;

            foreach (FocusModeUnlocks value in Enum.GetValues(typeof(FocusModeUnlocks)))
            {
                if (!TuiSettings.Current.FocusModeProgress.HasFlag(value))
                {
                    var focusModeUnlock = new FocusModeUnlock();
                    Application.Run(focusModeUnlock);
                    return;
                }
            }
            var focusModeWindow = new FocusModeWindow();
            Application.Run(focusModeWindow);
        }
    }

    class FocusModeWindow : EditWindow
    {
        public static StatsView Stats { get; set; }
        public static Figure Figure { get; set; }
        public static Shop Shop { get; set; }

        public FocusModeWindow(): base("Diiiiig!")
        {
            Stats = new StatsView();
            Figure = new Figure();
            Shop = new Shop();

            // Add close button
            var closeButton = new Button("Close");
            closeButton.X = Pos.Right(this) - 12;
            closeButton.Clicked += () => { Application.RequestStop(); };
            Add(closeButton);

            // Add stats
            Stats.X = 1;
            Stats.Height = Dim.Fill();
            Stats.Width = Dim.Fill();
            Add(Stats);

            Shop.X = Pos.Percent(50) - 8;
            Add(Shop);

            // Add canvas
            var canvas = new Canvas();
            canvas.X = 1;
            canvas.Y = 4;
            canvas.Width = Dim.Fill();
            canvas.Height = Dim.Fill();
            canvas.Add(Figure);
            Add(canvas);


            // Reset
            var resetButton = new Button("Reset");
            resetButton.Y = 2;
            resetButton.X = Pos.Right(this) - 12;
            resetButton.Clicked += () => { Reset(); };
            Add(resetButton);
        }

        public static void NextMission()
        {
            if (Stats.NextMission())
            {
                Map.Reset();
                Figure.Reset();
                Shop.EnableShop(false);
            }
        }

        public static void Reset()
        {
            Map.Reset();
            Figure.Reset();
            Stats.Reset();
            Shop.EnableShop(false);
        }

        public static void GameOver(string message)
        {
            MessageBox.Query("Game Over!", message, "Try again.");
            Reset();
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.Esc)
                Application.RequestStop();

            return base.ProcessKey(keyEvent);
        }
    }

    class Canvas : FrameView
    {
        private List<Figure> objects = new List<Figure>();
        ColorScheme rockScheme = new ColorScheme();
        ColorScheme silverScheme = new ColorScheme();
        ColorScheme diamondScheme =  new ColorScheme();
        ColorScheme bombScheme = new ColorScheme();
        ColorScheme dollarScheme = new ColorScheme();

        public Canvas()
        {
            var backgroundColor = TuiSettings.Current.BaseColor.NormalBackground;
            rockScheme.Normal = Application.Driver.MakeAttribute(Color.Brown, backgroundColor);
            silverScheme.Normal = Application.Driver.MakeAttribute(Color.Gray, backgroundColor);
            diamondScheme.Normal = Application.Driver.MakeAttribute(Color.White, backgroundColor);
            bombScheme.Normal = Application.Driver.MakeAttribute(Color.BrightRed, backgroundColor);
            dollarScheme.Normal = Application.Driver.MakeAttribute(Color.BrightGreen, backgroundColor);

            LayoutComplete += (e) =>
            {
                if (Map.Initiated == false)
                {
                    // Generate a map
                    Map.GenerateMap(e.OldBounds.Width, e.OldBounds.Height);
                }
            };
        }

        public void Add(Figure view)
        {
            objects.Add(view);
            base.Add(view);
        }

        public override void Redraw(Rect bounds)
        {
            base.Redraw(bounds);

            Driver.SetAttribute(ColorScheme.Normal);

            for (int y = 0; y < Frame.Height; y++)
            {
                for (int x = 0; x < Frame.Width; x++) 
                {
                    var material = Map.Get(new Point(x, y));
                    if (material.HasValue)
                    {
                        Move(x, y);

                        switch (material)
                        {
                            case Material.Rock:
                                Driver.SetAttribute(rockScheme.Normal);
                                break;
                            case Material.Silver:
                                Driver.SetAttribute(silverScheme.Normal);
                                break;
                            case Material.Bomb:
                                Driver.SetAttribute(bombScheme.Normal);
                                break;
                            case Material.Diamonds:
                                Driver.SetAttribute(diamondScheme.Normal);
                                break;
                            case Material.Dollars:
                                Driver.SetAttribute(dollarScheme.Normal);
                                break;
                            default:
                                Driver.SetAttribute(ColorScheme.Normal);
                                break;
                        }

                        var rune = new Rune((char)material);
                        Driver.AddRune(rune);
                    }
                }
            }
        }
    }

    class Figure : View
    {
        Dictionary<string, string> figures = new Dictionary<string, string>();
        string hOrientation = "right";
        string vOrientation = "";
        public Action<Point> FigureMoved;

        public Figure()
        {
            Height = 3;
            Width = 8;
            CanFocus = true;

            figures["right"] =
                @" ^_^    " +
                @"[###]//>" +
                @"(:::)   ";
            figures["left"] =
                @"    ^_^ " +
                @"<\\[###]" +
                @"   (:::)";
            figures["rightdown"] =
                @" ^_^    " +
                @"[###]\\ " +
                @"(:::) V ";
            figures["rightup"] =
                @" ^_^  A " +
                @"[###]// " +
                @"(:::)   ";
            figures["leftdown"] =
                @"    ^_^ " +
                @" //[###]" +
                @" V (:::)";
            figures["leftup"] =
                @" A  ^_^ " +
                @" \\[###]" +
                @"   (:::)";
        }
        public override void PositionCursor()
        {
            Move(0, 0);
        }

        public void Reset()
        {
            x = 0;
            X = 0;
            y = 0;
            Y = 0;
            // direction = new Direction();
            hOrientation = "right";
            vOrientation = "";
            SetNeedsDisplay();
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.CursorUp)
            {
                MoveFigure(new Point(0, -1));
                return true;
            }
            if (keyEvent.Key == Key.CursorDown)
            {
                MoveFigure(new Point(0, 1));
                return true;
            }
            if (keyEvent.Key == Key.CursorLeft)
            {
                MoveFigure(new Point(-1, 0));
                return true;
            }
            if (keyEvent.Key == Key.CursorRight)
            {
                MoveFigure(new Point(1, 0));
                return true;
            }

            return base.ProcessKey(keyEvent);
        }

        private int x = 0;
        private int y = 0;
        public void MoveFigure(Point direction)
        {
            var newX = direction.X + x;
            var newY = direction.Y + y;
            var parentFrame = SuperView.Bounds;

            // Return if we are not moving
            if (direction.X == 0 && direction.Y == 0)
                return;

            // make sure we don't leave the parent
            if (newX != x && newX >= 0 && newX + Frame.Width <= parentFrame.Width)
                x = newX;
            if (newY != y && newY >= 0 && newY + Frame.Height <= parentFrame.Height)
                y = newY;

            // Update position
            X = x;
            Y = y;

            // Change figure based on direction
            if (direction.X > 0)
                hOrientation = "right";
            if (direction.X < 0)
                hOrientation = "left";
            if (direction.Y > 0)
                vOrientation =  "down";
            if (direction.Y < 0)
                vOrientation = "up";
            if (direction.Y == 0)
                vOrientation = "";

            CheckCollision();

            SetNeedsDisplay();

            FigureMoved?.Invoke(new Point(x, y));
        }

        void CheckCollision()
        {
            var startX = x + 1;
            var startY = y + 1;
            var endX = Frame.Width + startX - 1;
            var endY = Frame.Height + startY - 1;

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    // Check collision
                    var p = new Point(x, y);
                    var item = Map.Get(p);
                    if (item.HasValue)
                    {
                        // Collision
                        Map.Remove(p);
                        FocusModeWindow.Stats.UpdateStats(item.Value);
                    }
                }
            }
        }
        
        public override void Redraw(Rect bounds)
        {
            Driver.SetAttribute (ColorScheme.Normal);
            Clear();

            var figure = figures[hOrientation + vOrientation];

            for (int y = 0; y < Frame.Height; y++)
            {
                for (int x = 0; x < Frame.Width; x++)
                {
                    Move (x, y);
                    var test = figure[x + y * Frame.Width];
                    Driver.AddRune(test);
                }
            }
        }
    }

    enum Material
    {
        Rock = '+',
        Silver = 'X',
        Bomb = 'ó',
        Diamonds = '◊',
        Dollars = '$'
    }
    
    static class Map
    {
        private static Random rand = new Random();
        private static int _height;
        private static int _width;
        private static Dictionary<Point, Material> map = new Dictionary<Point, Material>();
        public static bool Initiated = false;

        public static Material? Get(Point p)
        {
            if (map.ContainsKey(p))
                return map[p];

            return null;
        }

        public static void Remove(Point p)
        {
            if (map.ContainsKey(p))
                map.Remove(p);
        }

        public static void GenerateMap(int width, int height)
        {
            Initiated = true;
            _height = height;
            _width = width;

            var area = _height * _width;
            int radio = area / 1300;

            // Generate Silver
            var count = rand.Next(10 * radio, 15 * radio);
            for (int i = 0; i < count; i++)
                GenerateMaterial(Material.Silver, 0.5);
            
            // Generate bombs
            count = rand.Next(10 * radio, 20 * radio);
            for (int i = 0; i < count; i++)
                GenerateMaterial(Material.Bomb, 0.15);

            // Generate diamonds
            count = rand.Next(2 * radio, 4 * radio);
            for (int i = 0; i < count; i++)
                GenerateMaterial(Material.Diamonds, 0.2);

            // Generate dollars
            count = rand.Next(1 * radio, 5 * radio);
            for (int i = 0; i < count; i++)
                GenerateMaterial(Material.Dollars, 0.2);

            // Generate rock
            for (int y = 1; y < _height - 1; y++)
            {
                for (int x = 1; x < _width - 1; x++)
                {
                    if (x <= 8 && y <= 3)
                    {
                        Remove(new Point(x, y));
                        continue;
                    }
                    
                    var p = new Point(x, y);
                    if (map.ContainsKey(p) == false)
                        map[p] = Material.Rock;
                }
            }
        }

        static void GenerateMaterial(Material material, double chance)
        {
            var x = rand.Next(1, _width - 1);
            var y = rand.Next(1, _height - 1);
            var p = new Point(x, y);

            GenerateSurrounding(p, material, chance);
        }
        static void GenerateSurrounding(Point p, Material material, double chance)
        {
            map[p] = material;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) // center
                        continue;

                    var p2 = new Point(p.X + x, p.Y + y);
                    if (p2.X < 1 || p2.X > _width - 2 || p2.Y < 1 || p2.Y > _height - 2) // out of bounds
                        continue;
                    
                    if (rand.NextDouble() < chance)
                        GenerateSurrounding(p2, material, chance / 2);
                }
            }
        }

        public static void Reset()
        {
            map.Clear();
            GenerateMap(_width, _height);
        }
    }

    class Shop : View
    {
        private string figure =
            @"   ______   " +
            @"  / SHOP \  " +
            @"_/        \_" +
            @" | □ Π  П | ";

        private Button sellButton;
        private Button repairButton;
        private Button nextButton;

        public Shop()
        {
            Height = 4;
            Width = 12 + 20;
            FocusModeWindow.Figure.FigureMoved += FigureMoved;

            sellButton = new Button("Offload");
            sellButton.X = 14;
            sellButton.Clicked += FocusModeWindow.Stats.Sell;
            Add(sellButton);

            repairButton = new Button("Repair (1000g)");
            repairButton.X = 14;
            repairButton.Y = 1;
            repairButton.Clicked += FocusModeWindow.Stats.Repair;
            Add(repairButton);

            nextButton = new Button("Next Mission (10000g)");
            nextButton.X = 14;
            nextButton.Y = 2;
            nextButton.Clicked += FocusModeWindow.NextMission;
            Add(nextButton);

            EnableShop(false);
        }
        
        public override void Redraw(Rect bounds)
        {
            base.Redraw(bounds);
            Driver.SetAttribute (ColorScheme.Normal);

            for (int y = 0; y < Frame.Height; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    Move (x, y);
                    var test = figure[x + y * 12];
                    Driver.AddRune(test);
                }
            }
        }

        void FigureMoved(Point p)
        {
            if (p.X > Frame.X - 2 && p.X < Frame.X + 2 && p.Y == 0)
                EnableShop(true);
            else
                EnableShop(false);
        }

        public void EnableShop(bool enabled)
        {
            sellButton.Visible = enabled;
            sellButton.CanFocus = enabled;
            repairButton.Visible = enabled;
            repairButton.CanFocus = enabled;
            nextButton.Visible = enabled;
            nextButton.CanFocus = enabled;

            SetNeedsDisplay();
            Application.Refresh();
        }
    }

    class StatsView : View
    {
        public int Money { get; set; } = 10;
        public double Load { get; set; } = 0.0;
        public int Health { get; set; } = 100;
        public double Wear { get; set; } = 1.0;

        private TextView MoneyView;
        private TextView LoadView;
        private TextView HealthView;
        private TextView WearView;

        private ColorScheme normalScheme;
        private ColorScheme yellowScheme;
        private ColorScheme redScheme;

        public StatsView()
        {
            var backgroundColor = TuiSettings.Current.BaseColor.NormalBackground;
            normalScheme = new ColorScheme();
            normalScheme.Normal = Application.Driver.MakeAttribute(Color.White, backgroundColor);
            yellowScheme = new ColorScheme();
            yellowScheme.Normal = Application.Driver.MakeAttribute(Color.BrightYellow, backgroundColor);
            redScheme = new ColorScheme();
            redScheme.Normal = Application.Driver.MakeAttribute(Color.BrightRed, backgroundColor);

            var moneyLabel = new Label("Money: ")
            {
                Y = 0,
                X = 0,
                Width = 8
            };
            MoneyView = new TextView
            {
                Y = 0,
                X = Pos.Right(moneyLabel),
                Width = 10,
                Height = 1,
                CanFocus = false
            };
            Add(moneyLabel);
            Add(MoneyView);

            var LoadLabel = new Label("Load: ")
            {
                Y = 1,
                X = 0,
                Width = 8
            };
            LoadView = new TextView
            {
                Y = 1,
                X = Pos.Right(LoadLabel),
                Width = 10,
                Height = 1,
                CanFocus = false
            };
            Add(LoadLabel);
            Add(LoadView);


            var HealthLabel = new Label("Health: ")
            {
                Y = 2,
                X = 0,
                Width = 8
            };
            HealthView = new TextView
            {
                Y = 2,
                X = Pos.Right(HealthLabel),
                Width = 10,
                Height = 1,
                CanFocus = false
            };
            Add(HealthLabel);
            Add(HealthView);

            var WearLabel = new Label("Wear: ")
            {
                Y = 3,
                X = 0,
                Width = 8
            };
            WearView = new TextView
            {
                Y = 3,
                X = Pos.Right(WearLabel),
                Width = 10,
                Height = 1,
                CanFocus = false
            };
            Add(WearLabel);
            Add(WearView);
            
            UpdateViews();
        }
        
        public void UpdateStats(Material material)
        {
            switch (material)
            {
                case Material.Rock:
                    Load += 0.0001;
                    Wear -= 0.001;
                    Money += 1;
                    break;
                case Material.Silver:
                    Load += 0.01;
                    Money += 10;
                    break;
                case Material.Bomb:
                    Health -= 10;
                    Wear -= 0.1;
                    break;
                case Material.Diamonds:
                    Load += 0.001;
                    Money += 1000;
                    Wear -= 0.001;
                    break;
                case Material.Dollars:
                    Money += 100;
                    break;
            }

            UpdateViews();

            CheckViolations();
        }

        private void CheckViolations()
        {
            if (Load >= 1.0)
                FocusModeWindow.GameOver($"Your digger got crushed by the load!\nYou can retire with {Money} on your account.");
            else if (Wear <= 0.0)
                FocusModeWindow.GameOver($"Your digger broke down, you are forever stuck...\nYou can retire with {Money} on your account.");
            else if (Health <= 0)
                FocusModeWindow.GameOver($"Your digger blew up. You ded...\nYou can retire with {Money} on your account.");
        }

        private void UpdateViews()
        {
            // Set color for health
            if (Health <= 20)
                HealthView.ColorScheme = redScheme;
            else if (Health <= 50)
                HealthView.ColorScheme = yellowScheme;
            else if (Health > 50)
                HealthView.ColorScheme = normalScheme;

            // Set color for load
            if (Load >= 0.75)
                LoadView.ColorScheme = redScheme;
            else if (Load >= 0.5)
                LoadView.ColorScheme = yellowScheme;
            else if (Load < 0.5)
                LoadView.ColorScheme = normalScheme;

            // Set color for wear
            if (Wear <= 0.25)
                WearView.ColorScheme = redScheme;
            else if (Wear <= 0.5)
                WearView.ColorScheme = yellowScheme;
            else if (Wear > 0.5)
                WearView.ColorScheme = normalScheme;

            MoneyView.Text = Money.ToString();
            LoadView.Text = Load.ToString("P1");
            HealthView.Text = Health.ToString();
            WearView.Text = Wear.ToString("P1");
        }

        public void Reset()
        {
            Money = 10;
            Load = 0.0;
            Health = 100;
            Wear = 1.0;
            UpdateViews();
        }

        public void Sell()
        {
            Load = 0.0;
            UpdateViews();
        }
        public void Repair()
        {
            if (Money >= 1000)
            {
                Money -= 1000;
                Wear = 1.0;
                UpdateViews();
            }
        }
        public bool NextMission()
        {
            if (Money >= 10000)
            {
                Money -= 10000;
                return true;
            }

            return false;
        }
    }

    [Flags]
    public enum FocusModeUnlocks
    {
        [Display("Help I'm bored and I wanna play a game.")]
        HelpMenu = 1 << 0,
        [Display("I need to search for a solution.")]
        Search = 1 << 1,
        [Display("Without a delay i must run for a minute.")]
        Wait = 1 << 2,
        [Display("But my boss commands that i focus.")]
        Command = 1 << 3,
    }

    class FocusModeUnlock : EditWindow
    {
        private static readonly string checkedPrefix = "[X] ";
        private static readonly string uncheckedPrefix = "[ ] ";

        public FocusModeUnlock() : base("Bored?")
        {
            TextView textView = new TextView();
            textView.ReadOnly = true;
            FocusModeUnlocks unlock = TuiSettings.Current.FocusModeProgress;
            foreach (Enum value in Enum.GetValues(typeof(FocusModeUnlocks)))
            {
                AnnotationCollection annotations = AnnotationCollection.Annotate(value);
                textView.Text += (unlock.HasFlag(value) ? checkedPrefix : uncheckedPrefix) + annotations.Get<IStringValueAnnotation>().Value + '\n';
            }
            Add(textView);
        }
    }
}
