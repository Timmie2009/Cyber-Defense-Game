using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Linq;


using System;


namespace Cyber_Defense_Game.Views;

public partial class JoinView : UserControl
{
    private readonly MainView? _main;
    private string _enteredName = string.Empty;

    public JoinView()
    {
        InitializeComponent();
        NameInput.TextChanged += (_, _) => _enteredName = NameInput.Text ?? string.Empty;

        NameInput.PointerPressed += (_, _) =>
        {
            NameInput.Focus();

            if (OperatingSystem.IsBrowser() && string.IsNullOrWhiteSpace(NameInput.Text))
            {
                var prompted = BrowserInput.AskName("Enter your player name:", _enteredName);
                if (!string.IsNullOrWhiteSpace(prompted))
                {
                    NameInput.Text = prompted.Trim();
                    _enteredName = prompted.Trim();
                }
            }
        };
    }


    private string _enteredName = string.Empty;
    private string _enteredCode = string.Empty;

    private readonly TextBox _codeInput;
    private readonly TextBox _nameInput;
    private readonly TextBlock _statusText;
    private readonly Button _submitNameButton;
    private readonly StackPanel _lobbyControlsPanel;

    public JoinView()
    {
        _codeInput = new TextBox
        {
            Width = 150,
            MaxLength = 6,
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            TextAlignment = TextAlignment.Center,
            Watermark = "123456"
        };

        var scanQrBtn = new Button
        {
            Content = "Scan QR Code",
            Height = 38
        };
        scanQrBtn.Click += ScanQr_Click;

        _nameInput = new TextBox
        {
            MinWidth = 260,
            Watermark = "Enter your name",
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _submitNameButton = new Button
        {
            Content = "Join Game",
            Width = 260,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _submitNameButton.Click += Submit_Click;

        _statusText = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 320
        };

        var img1Btn = new Button { Content = "Image 1", Width = 110, Height = 45 };
        img1Btn.Click += Image1_Click;
        var img2Btn = new Button { Content = "Image 2", Width = 110, Height = 45 };
        img2Btn.Click += Image2_Click;
        var img3Btn = new Button { Content = "Image 3", Width = 110, Height = 45 };
        img3Btn.Click += Image3_Click;

        _lobbyControlsPanel = new StackPanel
        {
            IsVisible = false,
            Orientation = Orientation.Horizontal,
            Spacing = 15,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0),
            Children = { img1Btn, img2Btn, img3Btn }
        };

        var backBtn = new Button
        {
            Content = "Back",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        backBtn.Click += Back_Click;

        Content = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "Join Lobby",
                    FontSize = 26,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new StackPanel
                {
                    Spacing = 6,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "6-Digit Lobby Code:",
                            FontSize = 12,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 10,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Children = { _codeInput, scanQrBtn }
                        }
                    }
                },
                new StackPanel
                {
                    Spacing = 6,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Your Name:",
                            FontSize = 12,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        _nameInput
                    }
                },
                _submitNameButton,
                _statusText,
                _lobbyControlsPanel,
                backBtn
            }
        };

        if (!string.IsNullOrWhiteSpace(GameConnection.PendingJoinCode))
        {
            _enteredCode = GameConnection.PendingJoinCode;
            _codeInput.Text = _enteredCode;
            _statusText.Text = $"Lobby code {_enteredCode} loaded from QR code!";
            GameConnection.PendingJoinCode = string.Empty;
        }

        _codeInput.TextChanged += (_, _) =>
        {
            string digitsOnly = new string((_codeInput.Text ?? string.Empty).Where(char.IsDigit).ToArray());
            if (_codeInput.Text != digitsOnly)
                _codeInput.Text = digitsOnly;
            _enteredCode = digitsOnly;
        };

        _codeInput.PointerPressed += (_, _) =>
        {
            _codeInput.Focus();
            if (OperatingSystem.IsBrowser() && string.IsNullOrWhiteSpace(_codeInput.Text))
            {
                var prompted = BrowserInput.AskName("Enter 6-digit lobby code:", _enteredCode);
                if (!string.IsNullOrWhiteSpace(prompted))
                {
                    string clean = new string(prompted.Where(char.IsDigit).Take(6).ToArray());
                    _codeInput.Text = clean;
                    _enteredCode = clean;
                }
            }
        };

        _nameInput.TextChanged += (_, _) => _enteredName = _nameInput.Text ?? string.Empty;

        _nameInput.PointerPressed += (_, _) =>
        {
            _nameInput.Focus();
            if (OperatingSystem.IsBrowser() && string.IsNullOrWhiteSpace(_nameInput.Text))
            {
                var prompted = BrowserInput.AskName("Enter your player name:", _enteredName);
                if (!string.IsNullOrWhiteSpace(prompted))
                {
                    _nameInput.Text = prompted.Trim();
                    _enteredName = prompted.Trim();
                }
            }
        };
    }


    public JoinView() => InitializeComponent();
    public JoinView(MainView main) : this() => _main = main;

    private async void ScanQr_Click(object? sender, RoutedEventArgs e)
    {
        if (!OperatingSystem.IsBrowser())
        {
            _statusText.Text = "Scan the QR code with your phone camera or type the 6-digit code.";
            return;
        }

        try
        {
            _statusText.Text = "Opening camera scanner...";
            string? scanned = await BrowserInput.ScanQrCodeAsync();
            if (!string.IsNullOrWhiteSpace(scanned))
            {
                string code = ExtractSixDigitCode(scanned);
                if (code.Length == 6)
                {
                    _codeInput.Text = code;
                    _enteredCode = code;
                    _statusText.Text = $"Scanned lobby code: {code}";
                }
                else
                {
                    _statusText.Text = "QR code did not contain a valid 6-digit lobby code.";
                }
            }
            else
            {
                _statusText.Text = "QR scan cancelled.";
            }
        }
        catch (Exception ex)
        {
            _statusText.Text = $"Camera unavailable ({ex.Message}). Enter the 6-digit code manually.";
        }
    }

    private static string ExtractSixDigitCode(string raw)
    {
        int idx = raw.IndexOf("code=", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
            raw = raw[(idx + 5)..];
        return new string(raw.Where(char.IsDigit).Take(6).ToArray());
    }

    private void Image1_Click(object? sender, RoutedEventArgs e) => Select(1);
    private void Image2_Click(object? sender, RoutedEventArgs e) => Select(2);
    private void Image3_Click(object? sender, RoutedEventArgs e) => Select(3);

    private async void Select(int index)
    {


        GameState.SelectedImage = index;
        StatusText.Text = $"Host image set to {index}";
    }

    private void Back_Click(object? sender, RoutedEventArgs e) => _main?.ShowMenu();

        try
        {
            await GameConnection.ChangeImageAsync(index);
            _statusText.Text = index == 2
                ? "ALERT: Bjorn stole the frikandel!"
                : $"Image {index} sent to server!";
        }
        catch (Exception ex)
        {
            _statusText.Text = $"Connection failed: {ex.Message}";
        }
    }


        try
        {
            await GameConnection.ChangeImageAsync(index);

            StatusText.Text = index == 2
                ? "🚨 ALERT: Björn heeft de frikandel gestolen! 🌭"
                : $"Image {index} sent to server!";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Connection failed: {ex.Message}";
            Console.WriteLine(ex.ToString());
        }
    }


    private void Back_Click(object? sender, RoutedEventArgs e)
    {
        _main?.ShowMenu();
    }

    private async void Submit_Click(object? sender, RoutedEventArgs e)
    {

        _submitNameButton.Focus();

        string code = !string.IsNullOrWhiteSpace(_codeInput.Text)
            ? _codeInput.Text.Trim()
            : _enteredCode.Trim();

        if (code.Length != 6)
        {
            _statusText.Text = "Please enter a valid 6-digit lobby code (or scan the QR code).";
            return;
        }

        string playerName = !string.IsNullOrWhiteSpace(_nameInput.Text)
            ? _nameInput.Text.Trim()

        SubmitNameButton.Focus();

        string playerName = !string.IsNullOrWhiteSpace(NameInput.Text)
            ? NameInput.Text.Trim()

            : _enteredName.Trim();

        if (OperatingSystem.IsBrowser() && string.IsNullOrWhiteSpace(playerName))
        {
            var prompted = BrowserInput.AskName("Enter your player name:", "");
            if (!string.IsNullOrWhiteSpace(prompted))
            {
                playerName = prompted.Trim();

                _nameInput.Text = playerName;

                NameInput.Text = playerName;

            }
        }

        if (string.IsNullOrWhiteSpace(playerName))
            playerName = "Player";

        try
        {
            GameConnection.IsHost = false;


await GameConnection.ConnectAsync(GameConnection.DefaultServerUrl);
            bool ok = await GameConnection.JoinLobbyAsync(code, playerName);
            if (!ok)
            {
                _statusText.Text = $"Lobby '{code}' not found. Check the 6-digit code and try again.";
                return;
            }
            _lobbyControlsPanel.IsVisible = true;
            _statusText.Text = $"Joined lobby {code} as {playerName}!";
        }
        catch (Exception ex)
        {
            _statusText.Text = $"Connection failed: {ex.Message}";
        }
    }

}
           await GameConnection.ConnectAsync("https://cyberquiz.tail9822f1.ts.net");
            await GameConnection.JoinLobbyAsync(playerName);
            StatusText.Text = $"Joined the lobby as {playerName}!";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Connection failed: {ex.Message}";
        }
    }
}

