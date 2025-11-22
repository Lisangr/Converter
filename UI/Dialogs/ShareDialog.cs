using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Application.Models;
using Converter.Services;

namespace Converter.UI.Dialogs;

public class ShareDialog : Form
{
    private readonly ShareReport _report;
    private readonly ShareService _shareService;

    private PictureBox _previewImage = null!;
    private TabControl _tabControl = null!;
    private TextBox _twitterText = null!;
    private TextBox _redditText = null!;
    private TextBox _discordText = null!;
    private TextBox _plainText = null!;
    private TextBox _kickstarterText = null!;
    private TextBox _instagramText = null!;

    private Button _btnSaveImage = null!;
    private Button _btnCopyTwitter = null!;
    private Button _btnCopyReddit = null!;
    private Button _btnCopyDiscord = null!;
    private Button _btnCopyPlain = null!;
    private Button _btnCopyKickstarter = null!;
    private Button _btnCopyInstagram = null!;
    private Button _btnOpenTwitter = null!;
    private Button _btnOpenReddit = null!;

    public ShareDialog(ShareReport report)
    {
        _report = report;
        _shareService = new ShareService();
        InitializeComponents();
        _ = LoadPreviewAsync();
    }

    private void InitializeComponents()
    {
        Text = "Поделиться результатами";
        Size = new Size(900, 720);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var previewPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 320,
            Padding = new Padding(10)
        };

        _previewImage = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(30, 30, 30)
        };

        _btnSaveImage = new Button
        {
            Text = "💾 Сохранить изображение",
            Dock = DockStyle.Bottom,
            Height = 36
        };
        _btnSaveImage.Click += OnSaveImage;

        previewPanel.Controls.Add(_previewImage);
        previewPanel.Controls.Add(_btnSaveImage);

        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(10, 10)
        };

        BuildSocialTabs();
        BuildCrowdfundingTab();

        var btnClose = new Button
        {
            Text = "Закрыть",
            Dock = DockStyle.Bottom,
            Height = 40
        };
        btnClose.Click += (s, e) => Close();

        Controls.Add(_tabControl);
        Controls.Add(previewPanel);
        Controls.Add(btnClose);
    }

    private void BuildSocialTabs()
    {
        var twitterTab = new TabPage("Twitter / X");
        _twitterText = CreateTextBox(_report.GetShareText(ShareFormat.Twitter));
        _btnCopyTwitter = CreateCopyButton(280);
        _btnCopyTwitter.Click += (s, e) => CopyToClipboard(_twitterText.Text, "Twitter");

        _btnOpenTwitter = new Button
        {
            Text = "🐦 Открыть Twitter",
            Location = new Point(10, 280),
            Size = new Size(170, 30)
        };
        _btnOpenTwitter.Click += OnOpenTwitter;

        twitterTab.Controls.Add(_twitterText);
        twitterTab.Controls.Add(_btnCopyTwitter);
        twitterTab.Controls.Add(_btnOpenTwitter);

        var redditTab = new TabPage("Reddit");
        _redditText = CreateTextBox(_report.GetShareText(ShareFormat.Reddit));
        _btnCopyReddit = CreateCopyButton(280);
        _btnCopyReddit.Click += (s, e) => CopyToClipboard(_redditText.Text, "Reddit");

        _btnOpenReddit = new Button
        {
            Text = "🔴 Открыть Reddit",
            Location = new Point(10, 280),
            Size = new Size(170, 30)
        };
        _btnOpenReddit.Click += OnOpenReddit;

        redditTab.Controls.Add(_redditText);
        redditTab.Controls.Add(_btnCopyReddit);
        redditTab.Controls.Add(_btnOpenReddit);

        var discordTab = new TabPage("Discord");
        _discordText = CreateTextBox(_report.GetShareText(ShareFormat.Discord));
        _btnCopyDiscord = CreateCopyButton(280);
        _btnCopyDiscord.Click += (s, e) => CopyToClipboard(_discordText.Text, "Discord");
        discordTab.Controls.Add(_discordText);
        discordTab.Controls.Add(_btnCopyDiscord);

        var plainTab = new TabPage("Обычный текст");
        _plainText = CreateTextBox(_report.GetShareText(ShareFormat.Plain));
        _btnCopyPlain = CreateCopyButton(280);
        _btnCopyPlain.Click += (s, e) => CopyToClipboard(_plainText.Text, "текст");
        plainTab.Controls.Add(_plainText);
        plainTab.Controls.Add(_btnCopyPlain);

        _tabControl.TabPages.Add(twitterTab);
        _tabControl.TabPages.Add(redditTab);
        _tabControl.TabPages.Add(discordTab);
        _tabControl.TabPages.Add(plainTab);
    }

    private void BuildCrowdfundingTab()
    {
        var crowdfundingTab = new TabPage("Краудфандинг");

        var kickstarterLabel = new Label
        {
            Text = "Kickstarter Update",
            Location = new Point(10, 10),
            AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        _kickstarterText = CreateTextBox(ShareTemplates.GetKickstarterUpdate(_report), 120);
        _kickstarterText.Location = new Point(10, 40);

        _btnCopyKickstarter = new Button
        {
            Text = "📋 Копировать",
            Location = new Point(720, 170),
            Size = new Size(130, 30)
        };
        _btnCopyKickstarter.Click += (s, e) => CopyToClipboard(_kickstarterText.Text, "Kickstarter");

        var instagramLabel = new Label
        {
            Text = "Instagram Story",
            Location = new Point(10, 210),
            AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        _instagramText = CreateTextBox(ShareTemplates.GetInstagramStory(_report), 120);
        _instagramText.Location = new Point(10, 240);

        _btnCopyInstagram = new Button
        {
            Text = "📋 Копировать",
            Location = new Point(720, 370),
            Size = new Size(130, 30)
        };
        _btnCopyInstagram.Click += (s, e) => CopyToClipboard(_instagramText.Text, "Instagram");

        crowdfundingTab.Controls.Add(kickstarterLabel);
        crowdfundingTab.Controls.Add(_kickstarterText);
        crowdfundingTab.Controls.Add(_btnCopyKickstarter);
        crowdfundingTab.Controls.Add(instagramLabel);
        crowdfundingTab.Controls.Add(_instagramText);
        crowdfundingTab.Controls.Add(_btnCopyInstagram);

        _tabControl.TabPages.Add(crowdfundingTab);
    }

    private TextBox CreateTextBox(string text, int height = 260)
    {
        return new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Location = new Point(10, 10),
            Size = new Size(840, height),
            Text = text,
            Font = new Font("Consolas", 10),
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.White
        };
    }

    private Button CreateCopyButton(int y)
    {
        return new Button
        {
            Text = "📋 Копировать",
            Location = new Point(720, y),
            Size = new Size(130, 30)
        };
    }

    private async Task LoadPreviewAsync()
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"share_preview_{Guid.NewGuid():N}.png");
            var generatedPath = await _shareService.GenerateImageReport(_report, tempPath);

            using (var fs = new FileStream(generatedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var img = Image.FromStream(fs))
            {
                _previewImage.Image = new Bitmap(img);
            }

            _previewImage.Tag = generatedPath;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Ошибка генерации превью: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopyToClipboard(string text, string platform)
    {
        try
        {
            _shareService.CopyToClipboard(text);
            MessageBox.Show(this,
                $"Текст скопирован! Вставьте его в {platform}.",
                "Скопировано",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Ошибка копирования: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnSaveImage(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "PNG Image|*.png",
            DefaultExt = "png",
            FileName = $"conversion_results_{DateTime.Now:yyyy-MM-dd_HHmmss}.png"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                var tempPath = _previewImage.Tag as string;
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    File.Copy(tempPath, dialog.FileName, true);
                    if (MessageBox.Show(this, $"Изображение сохранено:\n{dialog.FileName}\n\nОткрыть папку?",
                            "Сохранено", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"/select,\"{dialog.FileName}\"",
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnOpenTwitter(object? sender, EventArgs e)
    {
        var tweetText = Uri.EscapeDataString(_twitterText.Text);
        var url = $"https://twitter.com/intent/tweet?text={tweetText}";
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось открыть браузер: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnOpenReddit(object? sender, EventArgs e)
    {
        var url = "https://www.reddit.com/submit";
        try
        {
            _shareService.CopyToClipboard(_redditText.Text);
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            MessageBox.Show(this,
                "Reddit открыт в браузере. Текст уже скопирован в буфер — просто вставьте его!",
                "Информация",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось открыть браузер: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        try
        {
            var tempPath = _previewImage.Tag as string;
            _previewImage.Image?.Dispose();
            if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch
        {
            // ignore cleanup errors
        }
    }
}
