using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Text;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sphynx.Client;

internal class Program
{
    static void CreateScreen(Toplevel top)
    {
        TextField usernameTextBox;
        TextField passwordTextBox;

        var mainView = new View
        {
            Width = Dim.Percent(60),
            Height = Dim.Percent(80),
            X = Pos.Align(Alignment.Center),
            Y = Pos.Align(Alignment.Center),
            BorderStyle = LineStyle.Single,
            TabStop = TabBehavior.NoStop
        }.Children(
            new View
            {
                Width = Dim.Fill(),
                Height = Dim.Percent(20),
                // BorderStyle = LineStyle.Single,
                TabStop = TabBehavior.NoStop
            }.Children(new Label
            {
                Text = """
                       .d8888. d8888b. db   db db    db d8b   db db    db
                       88'  YP 88  `8D 88   88 `8b  d8' 888o  88 `8b  d8'
                       `8bo.   88oodD' 88ooo88  `8bd8'  88V8o 88  `8bd8'
                         `Y8b. 88~~~   88~~~88    88    88 V8o88  .dPYb.
                       db   8D 88      88   88    88    88  V888 .8P  Y8.
                       `8888Y' 88      YP   YP    YP    VP   V8P YP    YP
                       """,
                X = Pos.Center(),
                Y = Pos.Center()
            }),
            new View
            {
                Y = 339,
                Width = Dim.Fill(),
                Height = Dim.Absolute(3),
                TabStop = TabBehavior.NoStop,
            }.Children(new Label { Text = "v1.0.0", X = Pos.Center(), Y = Pos.Center() }),
            new View
            {
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                // BorderStyle = LineStyle.Single,
                TabStop = TabBehavior.NoStop,
            }.Children(
                new View
                {
                    Width = Dim.Fill(),
                    Height = Dim.Percent(13),
                    TabStop = TabBehavior.NoStop,
                }.Children(new Label { Text = "┌———————————————————————————————————AUTHENTICATION———————————————————————————————————┐", X = Pos.Center() }),
                new View
                {
                    X = Pos.Align(Alignment.Center),
                    Y = Pos.Align(Alignment.Center),
                    Width = Dim.Percent(60),
                    Height = Dim.Percent(90),
                    TabStop = TabBehavior.NoStop,
                    // BorderStyle = LineStyle.Dashed
                }.Children(
                    new View
                    {
                        Width = Dim.Fill(),
                        Height = Dim.Auto(),
                        TabStop = TabBehavior.NoStop,
                    }.Children(
                        new Label { Text = " Username:" },
                        usernameTextBox = new TextField
                        {
                            Title = "Username Textbox",
                            Caption = "Enter username...",
                            CursorVisibility = CursorVisibility.Box,
                            Width = Dim.Fill(),
                            Height = 1,
                        }
                    ).WithMarginVertical(1),
                    new View
                    {
                        Width = Dim.Fill(),
                        Height = Dim.Auto(),
                        TabStop = TabBehavior.NoStop,
                    }.Children(
                        new Label { Text = " Password:" },
                        passwordTextBox = new TextField
                        {
                            Title = "Password Textbox",
                            Caption = "Enter password...",
                            CursorVisibility = CursorVisibility.Box,
                            Width = Dim.Fill(),
                            Height = 1,
                            Secret = true,
                            Y = 1
                        }
                    ).WithMarginVertical(1),
                    new Button
                    {
                        X = Pos.Align(Alignment.Center),
                        Title = "Login",
                    },
                    new View
                    {
                        Width = Dim.Fill(),
                        Height = Dim.Auto(),
                        // BorderStyle = LineStyle.Dashed
                    }.Children(
                            new Label
                            {
                                Text = "Need an account?",
                                X = Pos.Center()
                            },
                            new Label
                            {
                                Text = "Register here",
                                X = Pos.Center()
                            }
                      )
                )
            )
          );

        usernameTextBox.Accepting += (source, e) =>
        {
            Console.WriteLine("Username textbox has been entered!");
            e.Handled = true;
        };

        passwordTextBox.Accepting += (source, e) =>
        {
            Console.WriteLine("Password textbox has been entered!");
            e.Handled = true;
        };

        top.Add(mainView);

        mainView.CanFocus = true;
    }

    static void Main(string[] args)
    {
        try
        {
            Application.Force16Colors = true;
            Application.Init(driverName: "CursesDriver");

            var s = new Scheme
            {
                Normal = new(ColorName16.Black),
            };

            SchemeManager.AddScheme("DefaultScheme", s);
            var w = new Toplevel
            {
                Title = "Title for window",
                SchemeName = "DefaultScheme"
            };

            CreateScreen(w);

            Application.Run(w);
        }
        finally
        {
            Application.Shutdown();
        }
    }
}

public static class ViewExtensions
{
    public static View Children(this View view, params View[] others)
    {
        view.CanFocus = true;
        View? lastView = null;
        foreach (var v in others)
        {
            if (lastView != null)
            {
                v.Y = Pos.Bottom(lastView);
            }

            view.Add(v);
            lastView = v;
        }
        return view;
    }

    public static View With(this View view, Action<View> func)
    {
        func(view);
        return view;
    }

    public static View WithMarginVertical(this View view, Dim dim)
    {
        return view.WithMarginBottom(dim).WithMarginTop(dim);
    }

    public static View WithMarginBottom(this View view, Dim dim)
    {
        var paddingView = new View { Width = Dim.Fill(), Height = dim, CanFocus = false };

        var ret = new View
        {
            Width = Dim.Fill(),
            Height = Dim.Auto(),
            CanFocus = view.CanFocus
        }.Children(view, paddingView);

        return ret;
    }

    public static View WithMarginTop(this View view, Dim dim)
    {
        var paddingView = new View { Width = Dim.Fill(), Height = dim, CanFocus = false };

        var ret = new View
        {
            Width = Dim.Fill(),
            Height = Dim.Auto(),
            CanFocus = view.CanFocus
        }.Children(paddingView, view);

        return ret;

    }
}
