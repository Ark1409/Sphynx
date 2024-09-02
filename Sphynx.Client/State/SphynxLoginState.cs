using Spectre.Console;
using Spectre.Console.Rendering;
using Sphynx.Client.UI;
using Sphynx.Client.Utils;
using Sphynx.Core;
using Color = Spectre.Console.Color;

namespace Sphynx.Client.State
{
    internal class SphynxLoginState : ISphynxState
    {
        /// <summary>
        /// Holds the potentially logged-in user
        /// </summary>
        public SphynxSessionUser? User { get; private set; }

        /// <summary>
        /// Holds the <see cref="SphynxApp"/> instance
        /// </summary>
        private readonly SphynxApp _app;

        private Screen? _currentScreen;
        private LoginScreen _loginScreen = new();
        private RegisterScreen _registerScreen = new();

        private bool _running = true;

        private Task? _inputTask;
        private bool _inputTaskRunning = true;

        public SphynxLoginState(SphynxApp app)
        {
            _app = app;
        }

        public void OnEnter()
        {
            _loginScreen = new LoginScreen();
            _registerScreen = new RegisterScreen();
            _loginScreen.Refresh();
            _registerScreen.Refresh();

            _loginScreen.LoginButton.OnClick += OnLoginSubmit;
            _registerScreen.RegisterButton.OnClick += OnRegisterSubmit;
            _registerScreen.GoToLoginButton.OnClick += () => { ChangeScreen(_loginScreen); };
            _loginScreen.GoToRegisterButton.OnClick += () => { ChangeScreen(_registerScreen); };

            ChangeScreen(_loginScreen);
        }

        public void OnExit()
        {
            DestroyUI();
        }

        public void Dispose()
        {
            DestroyUI();
        }

        private void ChangeScreen(Screen s)
        {
            _currentScreen?.OnLeave();
            _currentScreen = s;
            _currentScreen.OnFocus();
        }

        public ISphynxState? Run()
        {
            while (_running)
            {
                Repaint();

                {
                    int windowWidth = Console.WindowWidth, windowHeight = Console.WindowHeight;
                    while (windowWidth == Console.WindowWidth && windowHeight == Console.WindowHeight)
                    {
                        Thread.Yield();
                        Thread.Sleep(1000 / 144);
                    }
                    windowWidth = Console.WindowWidth;
                    windowHeight = Console.WindowHeight;
                }

                // TODO Do better than just clear the whole console
                // AnsiConsole.Clear();
            }

            return null;
        }

        private void Repaint()
        {
            AnsiConsole.Cursor.Hide();
            AnsiConsole.Reset();
            ConsoleUtils.ResetColors();
            AnsiConsole.Cursor.SetPosition(1, 1);

            // Hide cursor while drawing
            // AnsiConsole.Cursor.Show();

            if (_currentScreen is not null)
            {
                AnsiConsole.Write(_currentScreen);
                var cursorPos = _currentScreen.CalculateCursorPosition();
                AnsiConsole.Cursor.SetPosition(1 + cursorPos.X, 1 + cursorPos.Y);
            }

            _inputTask ??= Task.Run(HandleInput);

            // TODO Calculate cursor position
            // TODO Add exit button to login menu
            // TODO Finish implementation of login/register button functionality
        }

        private void HandleInput()
        {
            _inputTaskRunning = true;
            while (_inputTaskRunning)
            {
                if (_currentScreen?.Items.Target is TextBox)
                {
                    // TODO Calculate cursor position
                    AnsiConsole.Cursor.Show();
                }
                else
                {
                    // Must be a button, hide cursor
                    AnsiConsole.Cursor.Hide();
                }
                
                while (!Console.KeyAvailable)
                {
                    if (!_inputTaskRunning) break;
                    Thread.Sleep(1000 / 60);
                }

                if (!_inputTaskRunning) break;
                var keyInfo = Console.ReadKey(true);

                if (_currentScreen?.HandleKey(keyInfo) ?? true)
                {
                    if (_currentScreen?.Items.Target == _registerScreen.ConfirmPasswordTextBox || _currentScreen?.Items.Target == _registerScreen.PasswordTextBox)
                    {
                        var confirmPlainText = _registerScreen.ConfirmPasswordTextBox.Buffer.PlainText;
                        if (_registerScreen.PasswordTextBox.Buffer.PlainText != confirmPlainText)
                        {
                            if (confirmPlainText.Length > 0)
                            {
                                _registerScreen.PasswordTextBox.HiddenCharacter.Item2 = ColorExtensions.FromHex("FF5A5F");
                                _registerScreen.ConfirmPasswordTextBox.HiddenCharacter.Item2 = ColorExtensions.FromHex("FF5A5F");
                            }
                            else
                            {
                                _registerScreen.PasswordTextBox.HiddenCharacter.Item2 = _registerScreen.PasswordTextBox.TextColor!;
                                _registerScreen.ConfirmPasswordTextBox.HiddenCharacter.Item2 = _registerScreen.ConfirmPasswordTextBox.TextColor!;
                            }
                        }
                        else
                        {
                            _registerScreen.PasswordTextBox.HiddenCharacter.Item2 = Color.LightGreen_1;
                            _registerScreen.ConfirmPasswordTextBox.HiddenCharacter.Item2 = Color.LightGreen_1;
                        }
                    }
                    Repaint();
                }
                else
                {
                    var items = _currentScreen.Items;
                    switch (keyInfo.KeyChar)
                    {
                        case '\r' or '\n':
                            {
                                var originalTarget = items.Target;

                                for (items.ShiftFocus(); items.Target != originalTarget; items.ShiftFocus())
                                {
                                    if (items.Target is TextField field)
                                    {
                                        if (string.IsNullOrEmpty(field.Buffer.PlainText))
                                        {
                                            break;
                                        }
                                    }
                                }

                                if (items.Target == originalTarget)
                                {
                                    items.ShiftFocus();
                                    // or submit form (log into server)
                                }
                                Repaint();
                            }
                            break;
                        default: break;
                    }
                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.UpArrow:
                            items.ShiftFocus(-1);
                            Repaint();
                            break;
                        case ConsoleKey.DownArrow:
                            items.ShiftFocus();
                            Repaint();
                            break;
                        default: break;
                    }
                }
            }
        }

        private async void OnLoginSubmit()
        {
            Console.WriteLine("Login Button Submit!");
            Thread.Sleep(1000);
        }

        private async void OnRegisterSubmit()
        {
            Console.WriteLine("Register Button Submit!");
            Thread.Sleep(1000);
        }

        private async Task<SphynxSessionUser?> ConnectToServer(string username, string password)
        {
            // TODO Connect to server with proper credentials
            if (false) { }

            return null;
        }

        private void DestroyUI()
        {
            _loginScreen.Destroy();
            _registerScreen.Destroy();

            _loginScreen = null;
            _registerScreen = null;
            _currentScreen = null;

            GC.Collect();
        }
        
        private abstract class Screen : Renderable, IFocusable
        {
            /// <summary>
            /// Main layout for the whole login screen interface
            /// </summary>
            protected IRenderable _rootLayout;
            protected Panel _titlePanel, _mainPanel;

            protected FigletFont _titleFont;
            protected FigletText _titleFiglet;

            /// <summary>
            /// Holds the list of items which can be focused upon,
            /// those being the items in the login form.
            /// </summary>
            internal FocusGroup<IFocusable> Items { get; set; }

            internal virtual void Refresh()
            {
                _titleFont = FigletFont.Default;
                _titleFiglet = new FigletText(_titleFont, "/SPHYNX/").Color(ColorExtensions.FromHex("35A7FF"));
                _titlePanel = new Panel(Align.Center(_titleFiglet, VerticalAlignment.Middle))
                              .Expand()
                              .RoundedBorder()
                              .BorderColor(ColorExtensions.FromHex("ffffff"))
                              .SafeBorder();
            }

            internal virtual void Destroy()
            {
                _rootLayout = null!;
                _mainPanel = null!;
                _titlePanel = null!;
                Items = null!;
            }

            public virtual bool HandleKey(in ConsoleKeyInfo key) => Items.HandleKey(key);
            public virtual void OnFocus() { }
            public virtual void OnLeave() { }

            protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => _rootLayout.Render(options, maxWidth);

            public abstract Point2i CalculateCursorPosition();
        }

        private class LoginScreen : Screen
        {
            /// <summary>
            /// <see cref="TextField"/> representing the username text box
            /// </summary>
            internal TextField UsernameTextBox { get; set; }

            /// <summary>
            /// <see cref="TextField"/> representing the password text box
            /// </summary>
            internal TextField PasswordTextBox { get; set; }

            /// <summary>
            /// <see cref="Button"/> representing the login button
            /// </summary>
            internal Button LoginButton { get; set; }

            internal Button GoToRegisterButton { get; set; }

            private Aligner _loginButtonAligner;

            protected const int _mainPanelRatio = 4;

            /// <summary>
            /// Holds the width of the center box
            /// </summary>
            private int _centerBoxWidth;

            internal override void Refresh()
            {
                base.Refresh();
                const int textFieldWidth = 40;

                UsernameTextBox = new TextField();
                UsernameTextBox.Ellipsis();
                UsernameTextBox.TextColor = ColorExtensions.FromHex("7796CB");
                UsernameTextBox.Width = textFieldWidth;

                PasswordTextBox = new TextField();
                PasswordTextBox.Ellipsis();
                PasswordTextBox.Width = textFieldWidth;
                PasswordTextBox.TextColor = ColorExtensions.FromHex("7796CB");
                PasswordTextBox.HiddenCharacter.Item2 = PasswordTextBox.TextColor;
                PasswordTextBox.Hidden = true;

                LoginButton = new Button("Login");
                LoginButton.SelectedBorderStyle = ColorExtensions.FromHex("35A7FF");
                LoginButton.SelectedTextStyle = LoginButton.SelectedBorderStyle;
                LoginButton.RoundedBorder().SafeBorder();

                GoToRegisterButton = new Button("Register");
                GoToRegisterButton.SelectedBorderStyle = ColorExtensions.FromHex("35A7FF");
                GoToRegisterButton.SelectedTextStyle = GoToRegisterButton.SelectedBorderStyle;
                GoToRegisterButton.NoBorder().SafeBorder();

                var usernameColumns = new Columns(new Markup("[#35A7FF]User:    [/]"), UsernameTextBox);
                var passwordColumns = new Columns(new Markup("[#35A7FF]Password:[/]"), PasswordTextBox);

                Columns submitColumn;
                {
                    _centerBoxWidth = textFieldWidth + "Password:".Length + 1;
                    _loginButtonAligner = Aligner.Center(LoginButton, VerticalAlignment.Middle);
                    _loginButtonAligner.Width = _centerBoxWidth;
                    submitColumn = new Columns(_loginButtonAligner);
                }

                submitColumn.Collapse();
                submitColumn.Padding = new Padding(0, 0, 0, 0);

                var centerPanel = new Panel(new Rows(
                                          usernameColumns,
                                          passwordColumns,
                                          submitColumn,
                                          new Padder(GoToRegisterButton,
                                              new Padding(_centerBoxWidth - GoToRegisterButton.Text.Length - 1, 0, 0, 0))
                                          )
                                      )
                                  .Collapse()
                                  .HeavyBorder()
                                  .BorderColor(ColorExtensions.FromHex("ffffff"))
                                  .SafeBorder()
                                  .Padding(2, 1);

                _mainPanel = new Panel(Align.Center(centerPanel, VerticalAlignment.Middle))
                             .Expand()
                             .RoundedBorder()
                             .BorderColor(ColorExtensions.FromHex("ffffff"))
                             .SafeBorder();


                _rootLayout = new Layout("root")
                    .SplitRows(
                        new Layout("top", _titlePanel).MinimumSize(_titleFont.Height + 1 * 2), // one space on top/bottom (Minimum height of 2 for border)
                        new Layout("main", _mainPanel).Ratio(_mainPanelRatio).MinimumSize(2) // Minimum height of 2 for border
                        );

                Items = new();
                Items.AddObject(UsernameTextBox)
                     .AddObject(PasswordTextBox)
                     .AddObject(LoginButton)
                     .AddObject(GoToRegisterButton);
            }

            internal override void Destroy()
            {
                LoginButton = null!;
                _loginButtonAligner = null!;
                UsernameTextBox = null!;
                PasswordTextBox = null!;
                GoToRegisterButton = null!;
                base.Destroy();
            }

            public override Point2i CalculateCursorPosition() => new Point2i(0, 0);
        }

        private class RegisterScreen : Screen
        {
            /// <summary>
            /// <see cref="TextField"/> representing the username text box
            /// </summary>
            internal TextField UsernameTextBox { get; set; }

            /// <summary>
            /// <see cref="TextField"/> representing the password text box
            /// </summary>
            internal TextField PasswordTextBox { get; set; }

            /// <summary>
            /// <see cref="TextField"/> representing the confirm password text box
            /// </summary>
            internal TextField ConfirmPasswordTextBox { get; set; }

            /// <summary>
            /// <see cref="Button"/> representing the login button
            /// </summary>
            internal Button RegisterButton { get; set; }

            internal Button GoToLoginButton { get; set; }

            private Aligner _registerButtonAligner;

            protected const int _mainPanelRatio = 4;

            /// <summary>
            /// Holds the width of the center box
            /// </summary>
            private int _centerBoxWidth;

            internal override void Refresh()
            {
                base.Refresh();
                const int textFieldWidth = 40;

                UsernameTextBox = new TextField();
                UsernameTextBox.Ellipsis();
                UsernameTextBox.TextColor = ColorExtensions.FromHex("7796CB");
                UsernameTextBox.Width = textFieldWidth;

                PasswordTextBox = new TextField();
                PasswordTextBox.Ellipsis();
                PasswordTextBox.Width = textFieldWidth;
                PasswordTextBox.TextColor = ColorExtensions.FromHex("7796CB");
                PasswordTextBox.HiddenCharacter.Item2 = PasswordTextBox.TextColor;
                PasswordTextBox.Hidden = true;

                ConfirmPasswordTextBox = new TextField();
                ConfirmPasswordTextBox.Ellipsis();
                ConfirmPasswordTextBox.Width = textFieldWidth;
                ConfirmPasswordTextBox.TextColor = ColorExtensions.FromHex("7796CB");
                ConfirmPasswordTextBox.HiddenCharacter.Item2 = ConfirmPasswordTextBox.TextColor;
                ConfirmPasswordTextBox.Hidden = true;

                RegisterButton = new Button("Register");
                RegisterButton.SelectedBorderStyle = ColorExtensions.FromHex("35A7FF");
                RegisterButton.SelectedTextStyle = RegisterButton.SelectedBorderStyle;
                RegisterButton.RoundedBorder().SafeBorder();

                GoToLoginButton = new Button("Go Back");
                GoToLoginButton.SelectedBorderStyle = Color.Red;
                GoToLoginButton.SelectedTextStyle = GoToLoginButton.SelectedBorderStyle;
                GoToLoginButton.NoBorder().SafeBorder();

                var usernameColumns
                    = new Columns(new Markup("[#35A7FF]User:            [/]"), UsernameTextBox);
                var passwordColumns
                    = new Columns(new Markup("[#35A7FF]Password:        [/]"), PasswordTextBox);
                var confirmPasswordColumns
                    = new Columns(new Markup("[#35A7FF]Confirm Password:[/]"), ConfirmPasswordTextBox);
                _centerBoxWidth = textFieldWidth + "Confirm Password:".Length + 1;

                Columns submitColumn = new Columns(RegisterButton);

                submitColumn.Collapse();

                var centerPanel = new Panel(new Rows(
                                      usernameColumns,
                                      passwordColumns,
                                      confirmPasswordColumns,
                                      new Padder(submitColumn,
                                          new Padding((_centerBoxWidth - RegisterButton.Text.Length - 4) / 2, 0)),
                                      new Padder(GoToLoginButton,
                                          new Padding(_centerBoxWidth - GoToLoginButton.Text.Length - 1, 0, 0, 0))
                                      ))
                                  .Collapse()
                                  .HeavyBorder()
                                  .BorderColor(ColorExtensions.FromHex("ffffff"))
                                  .SafeBorder()
                                  .Padding(2, 1);

                _mainPanel = new Panel(Align.Center(centerPanel, VerticalAlignment.Middle))
                             .Expand()
                             .RoundedBorder()
                             .BorderColor(ColorExtensions.FromHex("ffffff"))
                             .SafeBorder();

                _rootLayout = new Layout("root")
                    .SplitRows(
                        new Layout("top", _titlePanel).MinimumSize(_titleFont.Height + 1 * 2), // one space on top/bottom (Minimum height of 2 for border)
                        new Layout("main", _mainPanel).Ratio(_mainPanelRatio).MinimumSize(2) // Minimum height of 2 for border
                        );

                Items = new();
                Items.AddObject(UsernameTextBox)
                     .AddObject(PasswordTextBox)
                     .AddObject(ConfirmPasswordTextBox)
                     .AddObject(RegisterButton)
                     .AddObject(GoToLoginButton);
            }

            internal override void Destroy()
            {
                RegisterButton = null!;
                _registerButtonAligner = null!;
                UsernameTextBox = null!;
                PasswordTextBox = null!;
                ConfirmPasswordTextBox = null!;
                base.Destroy();
            }

            public override Point2i CalculateCursorPosition() => new Point2i(0, 0);
        }
    }
}
