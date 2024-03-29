using Spectre.Console;
using Spectre.Console.Rendering;
using Sphynx.Client.UI;
using Sphynx.Client.Utils;
using Sphynx.Core;

namespace Sphynx.Client.State
{
    internal class SphynxLoginState : ISphynxState
    {
        /// <summary>
        /// Holds the potentially logged-in user
        /// </summary>
        public SphynxSessionUser? User { get; private set; }

        /// <summary>
        /// Holds the <see cref="SphynxClient"/> instance
        /// </summary>
        private readonly SphynxClient _client;

        private bool _running = true;

        private Task? _inputTask;
        private bool _inputTaskRunning = true;

        /// <summary>
        /// Main layout for the whole login screen interface
        /// </summary>
        private IRenderable _rootLayout;
        private Panel _titlePanel, _mainPanel;
        private const int _mainPanelRatio = 4;

        private FigletFont _titleFont;
        private FigletText _titleFiglet;

        /// <summary>
        /// Holds the list of items which can be focused upon,
        /// those being the items in the login form.
        /// </summary>
        private FocusGroup<IFocusable> _items;
        
        /// <summary>
        /// <see cref="TextField"/> representing the username text box
        /// </summary>
        private TextField _usernameTextBox;
        
        /// <summary>
        /// <see cref="TextField"/> representing the password text box
        /// </summary>
        private TextField _passwordTextBox;
        
        /// <summary>
        /// <see cref="Button"/> representing the login button
        /// </summary>
        private Button _loginButton;
        
        private Aligner _loginButtonAligner;
        
        /// <summary>
        /// Holds the width of the center box
        /// </summary>
        private int _centerBoxWidth;

        public SphynxLoginState(SphynxClient client)
        {
            _client = client;
        }

        public void OnEnter()
        {
            InitUI();
        }

        public void OnExit()
        {
            DestroyUI();
        }

        public void Dispose()
        {
            DestroyUI();
        }

        public ISphynxState? Run()
        {
            while (_running)
            {
                AnsiConsole.Cursor.Hide();
                AnsiConsole.Reset();
                ConsoleUtils.ResetColors();
                AnsiConsole.Cursor.SetPosition(1, 1);
                AnsiConsole.Cursor.Show();
                Repaint();

                {
                    int windowWidth = Console.WindowWidth, windowHeight = Console.WindowHeight;
                    while (windowWidth == Console.WindowWidth && windowHeight == Console.WindowHeight)
                    {
                        Thread.Yield();
                        Thread.Sleep(1000 / 120);
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

            AnsiConsole.Write(_rootLayout);

            _inputTask ??= Task.Run(HandleInput);

            AnsiConsole.Cursor.SetPosition(1, 1);
        }

        private void HandleInput()
        {
            _inputTaskRunning = true;
            while (_inputTaskRunning)
            {
                while (!Console.KeyAvailable)
                {
                    if (!_inputTaskRunning) break;
                    Thread.Sleep(1000 / 30);
                }
                
                if (!_inputTaskRunning) break;
                var keyInfo = Console.ReadKey(true);

                if (_items.HandleKey(keyInfo))
                {
                    Repaint();
                }
                else
                {
                    switch (keyInfo.KeyChar)
                    {
                        case '\r' or '\n':
                            {
                                var originalTarget = _items.Target;

                                for (_items.ShiftFocus(); _items.Target != originalTarget; _items.ShiftFocus())
                                {
                                    if (_items.Target is TextField field)
                                    {
                                        if (string.IsNullOrEmpty(field.Buffer.PlainText))
                                        {
                                            break;
                                        }
                                    }
                                }

                                if (_items.Target == originalTarget)
                                {
                                    _items.ShiftFocus();
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
                            _items.ShiftFocus(-1);
                            Repaint();
                            break;
                        case ConsoleKey.DownArrow:
                            _items.ShiftFocus();
                            Repaint();
                            break;
                        default: break;
                    }
                }
                if (_items.Target is TextField f)
                {
                    // TODO Calculate cursor position
                    AnsiConsole.Cursor.Show();
                }
                else
                {
                    // Must be submit button, hide cursor
                    AnsiConsole.Cursor.Hide();
                }
            }
        }

        private async void OnSubmit()
        {
            Console.WriteLine("Hit Submit!");
            Thread.Sleep(1000);

            SphynxSessionUser? user = await ConnectToServer();

            if (user is not null)
            {
                _inputTaskRunning = false;
                _running = false;
                _inputTask = null;
            }
            else
            {
                _inputTaskRunning = true;
                _inputTask = null;
            }
        }

        private async Task<SphynxSessionUser?> ConnectToServer()
        {
            // TODO Connect to server with proper credentials
            if (false) { }

            return null;
        }

        private void InitUI()
        {
            const int textFieldWidth = 40;

            _usernameTextBox = new TextField();
            _usernameTextBox.Ellipsis();
            _usernameTextBox.TextColor = ColorExtensions.FromHex("7796CB");
            _usernameTextBox.Width = textFieldWidth;

            _passwordTextBox = new TextField();
            _passwordTextBox.Ellipsis();
            _passwordTextBox.Width = textFieldWidth;
            _passwordTextBox.TextColor = ColorExtensions.FromHex("7796CB");
            _passwordTextBox.HiddenCharacter.Item2 = _passwordTextBox.TextColor;
            _passwordTextBox.Hidden = true;

            _loginButton = new Button("Submit");
            _loginButton.SelectedBorderStyle = ColorExtensions.FromHex("35A7FF");
            _loginButton.SelectedTextStyle = _loginButton.SelectedBorderStyle;
            _loginButton.RoundedBorder().SafeBorder();
            _loginButton.OnClick += OnSubmit;

            var usernameColumns = new Columns(new Markup("[#35A7FF]Login:   [/]"), _usernameTextBox);
            var passwordColumns = new Columns(new Markup("[#35A7FF]Password:[/]"), _passwordTextBox);

            Columns submitColumn;
            {
                _centerBoxWidth = textFieldWidth + "Password:".Length + 1;
                _loginButtonAligner = Aligner.Center(_loginButton, VerticalAlignment.Middle);
                _loginButtonAligner.Width = _centerBoxWidth;
                submitColumn = new Columns(_loginButtonAligner);
            }

            submitColumn.Collapse();
            submitColumn.Padding = new Padding(0, 0, 0, 0);

            var centerPanel = new Panel(new Rows(
                                      usernameColumns,
                                      passwordColumns,
                                      submitColumn
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

            _titleFont = FigletFont.Default;
            _titleFiglet = new FigletText(_titleFont, "/SPHYNX/").Color(ColorExtensions.FromHex("FF5964"));
            _titlePanel = new Panel(Align.Center(_titleFiglet, VerticalAlignment.Middle))
                          .Expand()
                          .RoundedBorder()
                          .BorderColor(ColorExtensions.FromHex("ffffff"))
                          .SafeBorder();

            // _rootLayout = new Rows(
            //     _titlePanel,
            //     _mainPanel);

            _rootLayout = new Layout("root")
                .SplitRows(
                    new Layout("top", _titlePanel).MinimumSize(_titleFont.Height + 1 * 2), // one space on top/bottom (Minimum height of 2 for border)
                    new Layout("main", _mainPanel).Ratio(_mainPanelRatio).MinimumSize(2) // Minimum height of 2 for border
                    );

            _items = new();
            _items.AddObject(_usernameTextBox);
            _items.AddObject(_passwordTextBox);
            _items.AddObject(_loginButton);
        }

        private void DestroyUI()
        {
            _rootLayout = null!;
            _loginButton = null!;
            _mainPanel = null!;
            _titlePanel = null!;
            _loginButtonAligner = null!;
            _usernameTextBox = null!;
            _passwordTextBox = null!;
            _items = null!;
            GC.Collect();
        }
    }
}
