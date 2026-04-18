namespace SteamSwitcherGUI;
using SteamSwitcherCore;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

public partial class Form1 : Form
{
    private readonly SteamAccountManager manager;
    private readonly ListBox accountList;
    private readonly TextBox logBox;
    private readonly Label hintLabel;
    private readonly Panel titleBar;
    private readonly Label titleLabel;
    private readonly Button closeButton;
    private readonly Label infoIcon;
    private readonly ToolTip infoTooltip;
    private readonly Label themeToggleButton;
    private bool isSwitching;
    private bool isDarkTheme;
    private bool themeHover;
    private int hoverIndex = -1;
    private readonly string themeFile;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private static readonly IntPtr HTCAPTION = new IntPtr(2);

    public Form1()
    {
        InitializeComponent();
        Text = string.Empty;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;

        themeFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SteamSwitcherGUI", "theme.txt");
        LoadTheme();

        manager = new SteamAccountManager();
        int accountCount = Math.Max(1, manager.Accounts.Count);
        int itemHeight = 44;
        int maxVisible = 8;
        int visibleCount = Math.Min(accountCount, maxVisible);
        int listHeight = visibleCount * itemHeight + 4;
        int titleHeight = 48;
        int clientHeight = titleHeight + 10 + listHeight + 8 + 36 + 10;
        ClientSize = new Size(380, clientHeight);

        titleBar = new Panel
        {
            Left = 0,
            Top = 0,
            Width = ClientSize.Width,
            Height = titleHeight,
            BackColor = Color.Transparent
        };
        titleBar.MouseDown += TitleBar_MouseDown;

        titleLabel = new Label
        {
            Left = 16,
            AutoSize = true,
            Text = "Steam Account Switcher",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.White
        };
        titleLabel.MouseDown += TitleBar_MouseDown;
        titleLabel.Top = (titleHeight - titleLabel.PreferredHeight) / 2;

        closeButton = new Button
        {
            Width = 32,
            Height = 32,
            Left = ClientSize.Width - 40,
            Top = (titleHeight - 32) / 2,
            FlatStyle = FlatStyle.Flat,
            Text = string.Empty,
            Cursor = Cursors.Hand,
            TabStop = false
        };
        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.Paint += CloseButton_Paint;
        closeButton.Click += (sender, e) => Close();

        titleBar.Controls.Add(titleLabel);
        titleBar.Controls.Add(closeButton);

        hintLabel = new Label
        {
            Left = 20,
            Top = titleHeight + 8,
            Width = 280,
            Text = string.Empty,
            Visible = false,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular),
            ForeColor = Color.DarkSlateGray
        };

        infoTooltip = new ToolTip();
        infoIcon = new Label
        {
            Left = 0,
            Top = 0,
            Width = 22,
            Height = 22,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Help,
            BackColor = Color.Transparent,
            Text = string.Empty
        };
        infoIcon.Paint += InfoIcon_Paint;
        infoTooltip.SetToolTip(infoIcon, "Двойной клик для смены аккаунта");

        accountList = new ListBox
        {
            Left = 20,
            Top = titleBar.Bottom + 12,
            Width = 340,
            Height = listHeight,
            SelectionMode = SelectionMode.One,
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = itemHeight,
            Font = new Font("Segoe UI Semibold", 10F),
            BackColor = isDarkTheme ? Color.FromArgb(45, 45, 48) : Color.FromArgb(245, 245, 245),
            ForeColor = Color.Black
        };

        accountList.Items.AddRange(manager.Accounts.Keys.ToArray());
        accountList.DoubleClick += AccountList_DoubleClick;
        accountList.DrawItem += AccountList_DrawItem;
        accountList.MouseMove += AccountList_MouseMove;
        accountList.MouseLeave += AccountList_MouseLeave;
        if (accountList.Items.Count > 0)
            accountList.SelectedIndex = 0;

        infoIcon.Left = 20;
        infoIcon.Top = accountList.Bottom + 12;
        infoTooltip.SetToolTip(infoIcon, "Двойной клик для смены аккаунта");

        themeToggleButton = new Label
        {
            Width = 26,
            Height = 26,
            Left = accountList.Right - 26,
            Top = infoIcon.Top + (infoIcon.Height - 26) / 2,
            Font = new Font("Segoe UI", 10F),
            Cursor = Cursors.Hand,
            TabStop = false,
            BackColor = Color.Transparent,
            Text = string.Empty
        };
        themeToggleButton.Paint += ThemeToggleButton_Paint;
        themeToggleButton.Click += ThemeToggleButton_Click;
        themeToggleButton.MouseEnter += (sender, e) => { themeHover = true; themeToggleButton.Invalidate(); };
        themeToggleButton.MouseLeave += (sender, e) => { themeHover = false; themeToggleButton.Invalidate(); };

        logBox = new TextBox
        {
            Left = accountList.Left,
            Top = accountList.Top,
            Width = accountList.Width,
            Height = accountList.Height,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = isDarkTheme ? Color.FromArgb(45, 45, 48) : Color.FromArgb(245, 245, 245),
            ForeColor = Color.Black,
            Font = new Font("Segoe UI", 9F),
            Visible = false
        };

        Controls.Add(titleBar);
        Controls.Add(accountList);
        Controls.Add(infoIcon);
        Controls.Add(logBox);
        Controls.Add(themeToggleButton);

        ApplyTheme();
    }

    private void LoadTheme()
    {
        try
        {
            if (File.Exists(themeFile))
            {
                string text = File.ReadAllText(themeFile).Trim();
                isDarkTheme = text.Equals("dark", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                isDarkTheme = false;
                SaveTheme();
            }
        }
        catch
        {
            isDarkTheme = false;
        }
    }

    private void SaveTheme()
    {
        try
        {
            string dir = Path.GetDirectoryName(themeFile)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(themeFile, isDarkTheme ? "dark" : "light");
        }
        catch
        {
        }
    }

    private void ApplyTheme()
    {
        var backColor = isDarkTheme ? Color.FromArgb(30, 30, 30) : Color.White;
        var panelColor = isDarkTheme ? Color.FromArgb(45, 45, 48) : Color.FromArgb(245, 245, 245);
        var titleColor = isDarkTheme ? Color.FromArgb(35, 35, 35) : Color.FromArgb(235, 235, 235);
        var textColor = isDarkTheme ? Color.WhiteSmoke : Color.FromArgb(30, 30, 30);
        var accentColor = Color.FromArgb(0, 120, 215);

        BackColor = backColor;
        titleBar.BackColor = titleColor;
        titleLabel.ForeColor = textColor;
        closeButton.BackColor = titleColor;
        closeButton.ForeColor = textColor;
        hintLabel.ForeColor = isDarkTheme ? Color.LightGray : Color.DarkSlateGray;
        accountList.BackColor = panelColor;
        accountList.ForeColor = textColor;
        logBox.BackColor = panelColor;
        logBox.ForeColor = textColor;
        themeToggleButton.BackColor = Color.Transparent;
        themeToggleButton.ForeColor = textColor;
        themeToggleButton.Invalidate();
        infoIcon.BackColor = Color.Transparent;
        infoIcon.ForeColor = textColor;
        infoIcon.Invalidate();

        Invalidate();
    }

    private void ThemeToggleButton_Click(object? sender, EventArgs e)
    {
        isDarkTheme = !isDarkTheme;
        ApplyTheme();
        SaveTheme();
    }

    private void AccountList_MouseMove(object? sender, MouseEventArgs e)
    {
        int index = accountList.IndexFromPoint(e.Location);
        if (index != hoverIndex)
        {
            hoverIndex = index;
            accountList.Invalidate();
        }
    }

    private void AccountList_MouseLeave(object? sender, EventArgs e)
    {
        hoverIndex = -1;
        accountList.Invalidate();
    }

    private async void AccountList_DoubleClick(object? sender, EventArgs e)
    {
        if (isSwitching || accountList.SelectedItem == null)
            return;

        isSwitching = true;
        accountList.Enabled = false;
        accountList.Visible = false;
        hintLabel.Visible = false;
        logBox.Visible = true;
        logBox.Clear();

        string accountKey = accountList.SelectedItem.ToString()!;

        await Task.Run(() =>
        {
            manager.TrySwitchAccount(accountKey, AppendLog, out string resultMessage);
            AppendLog(resultMessage);
        });

        AppendLog("✅ Операция завершена.");
        await Task.Delay(1200);

        logBox.Visible = false;
        accountList.Visible = true;
        accountList.Enabled = true;
        hintLabel.Visible = true;
        isSwitching = false;
    }

    private void AccountList_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
            return;

        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        bool hover = e.Index == hoverIndex;
        string text = accountList.Items[e.Index].ToString()!;

        var backColor = isDarkTheme ? Color.FromArgb(45, 45, 48) : Color.FromArgb(245, 245, 245);
        var hoverColor = Color.FromArgb(40, 0, 120, 215);
        var selectedColor = Color.FromArgb(0, 120, 215);
        var textColor = isDarkTheme ? Color.WhiteSmoke : Color.FromArgb(25, 25, 25);
        var selectedTextColor = Color.White;

        e.Graphics.FillRectangle(new SolidBrush(backColor), e.Bounds);

        var textRect = new Rectangle(e.Bounds.Left + 14, e.Bounds.Top + 8, e.Bounds.Width - 28, e.Bounds.Height - 16);
        if (selected || hover)
        {
            using var highlightBrush = new SolidBrush(selected ? selectedColor : hoverColor);
            e.Graphics.FillRectangle(highlightBrush, textRect);
        }

        TextRenderer.DrawText(e.Graphics, text, accountList.Font, textRect, selected ? selectedTextColor : textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        using var lineBrush = new SolidBrush(isDarkTheme ? Color.FromArgb(70, 70, 70) : Color.FromArgb(220, 220, 220));
        e.Graphics.FillRectangle(lineBrush, e.Bounds.Left + 12, e.Bounds.Bottom - 1, e.Bounds.Width - 24, 1);
    }

    private void AppendLog(string text)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(AppendLog), text);
            return;
        }

        if (!string.IsNullOrEmpty(logBox.Text))
            logBox.AppendText("\r\n");

        logBox.AppendText(text);
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        ReleaseCapture();
        SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, IntPtr.Zero);
    }

    private void CloseButton_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Button btn)
            return;

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var backgroundBrush = new SolidBrush(btn.BackColor);
        e.Graphics.FillRectangle(backgroundBrush, btn.ClientRectangle);
        using var pen = new Pen(btn.ForeColor, 2.5F)
        {
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
            StartCap = System.Drawing.Drawing2D.LineCap.Round
        };

        int offset = 10;
        int max = btn.Width - offset;
        e.Graphics.DrawLine(pen, offset, offset, max, max);
        e.Graphics.DrawLine(pen, max, offset, offset, max);
    }

    private void InfoIcon_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var backgroundColor = isDarkTheme ? Color.FromArgb(45, 45, 48) : Color.FromArgb(245, 245, 245);
        using var backgroundBrush = new SolidBrush(backgroundColor);
        e.Graphics.FillEllipse(backgroundBrush, 0, 0, infoIcon.Width - 1, infoIcon.Height - 1);
        using var borderPen = new Pen(isDarkTheme ? Color.FromArgb(120, 120, 120) : Color.FromArgb(200, 200, 200), 1.5F);
        e.Graphics.DrawEllipse(borderPen, 0, 0, infoIcon.Width - 1, infoIcon.Height - 1);
        using var textBrush = new SolidBrush(isDarkTheme ? Color.WhiteSmoke : Color.FromArgb(40, 40, 40));
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        e.Graphics.DrawString("i", infoIcon.Font, textBrush, new RectangleF(0, 0, infoIcon.Width, infoIcon.Height), sf);
    }

    private void ThemeToggleButton_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var iconBackground = themeHover ? (isDarkTheme ? Color.FromArgb(60, 60, 60) : Color.FromArgb(235, 235, 235)) : Color.Transparent;
        if (iconBackground.A > 0)
        {
            using var backgroundBrush = new SolidBrush(iconBackground);
            e.Graphics.FillEllipse(backgroundBrush, 0, 0, themeToggleButton.Width - 1, themeToggleButton.Height - 1);
        }

        using var borderPen = new Pen(isDarkTheme ? Color.FromArgb(120, 120, 120) : Color.FromArgb(200, 200, 200), 1.5F);
        e.Graphics.DrawEllipse(borderPen, 0, 0, themeToggleButton.Width - 1, themeToggleButton.Height - 1);

        var center = new PointF(themeToggleButton.Width / 2F, themeToggleButton.Height / 2F);
        var iconColor = themeToggleButton.ForeColor;

        if (isDarkTheme)
        {
            // Light mode icon: sun
            float sunRadius = 5.6f;
            using var sunBrush = new SolidBrush(iconColor);
            e.Graphics.FillEllipse(sunBrush, center.X - sunRadius, center.Y - sunRadius, sunRadius * 2, sunRadius * 2);
            using var rayPen = new Pen(iconColor, 1.2F) { EndCap = System.Drawing.Drawing2D.LineCap.Round };
            var rayLength = 3.5f;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * (float)(Math.PI / 180.0);
                float x1 = center.X + (sunRadius + 0.8f) * (float)Math.Cos(angle);
                float y1 = center.Y + (sunRadius + 0.8f) * (float)Math.Sin(angle);
                float x2 = center.X + (sunRadius + rayLength) * (float)Math.Cos(angle);
                float y2 = center.Y + (sunRadius + rayLength) * (float)Math.Sin(angle);
                e.Graphics.DrawLine(rayPen, x1, y1, x2, y2);
            }
        }
        else
        {
            // Dark mode icon: crescent moon
            using var moonBrush = new SolidBrush(iconColor);
            var moonRect = new RectangleF(center.X - 7.3f, center.Y - 6.8f, 14.6f, 13.6f);
            e.Graphics.FillEllipse(moonBrush, moonRect);
            using var backgroundBrush = new SolidBrush(isDarkTheme ? Color.FromArgb(45, 45, 48) : Color.FromArgb(245, 245, 245));
            var cutoutRect = new RectangleF(center.X - 2.2f, center.Y - 7.3f, 13.6f, 13.6f);
            e.Graphics.FillEllipse(backgroundBrush, cutoutRect);
        }
    }
}
