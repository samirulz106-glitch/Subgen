using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SubGen;

public class MainForm : Form
{
    sealed record ModelChoice(string Label, string File, bool Foreign)
    {
        public override string ToString() => Label;
    }

    static readonly Regex ProgressRx = new(@"progress\s*=\s*(\d+)\s*%", RegexOptions.Compiled);

    readonly TextBox pathBox = new();
    readonly Button browseBtn = new() { Text = "Choose video..." };
    readonly ComboBox modelBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly CheckBox benchBox = new() { Text = "Benchmark only (first 5 minutes, saves nothing)", AutoSize = true };
    readonly ProgressBar bar = new() { Minimum = 0, Maximum = 100 };
    readonly Label status = new() { AutoSize = false, Text = "Choose a video, or drag one onto this window." };
    readonly Button startBtn = new() { Text = "Generate subtitles" };
    readonly Button cancelBtn = new() { Text = "Cancel", Enabled = false };

    readonly string? initialFile;
    Process? current;
    bool cancelled;

    public MainForm(string? initialFile)
    {
        this.initialFile = initialFile;

        Text = "SubGen - subtitle generator";
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(560, 230);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AllowDrop = true;

        pathBox.SetBounds(12, 14, 430, 25);
        browseBtn.SetBounds(450, 12, 98, 28);
        modelBox.SetBounds(12, 50, 300, 25);
        benchBox.SetBounds(12, 82, 400, 24);
        bar.SetBounds(12, 118, 536, 22);
        status.SetBounds(12, 148, 536, 36);
        startBtn.SetBounds(12, 190, 200, 30);
        cancelBtn.SetBounds(220, 190, 100, 30);

        modelBox.Items.Add(new ModelChoice("Accurate (base.en) - the tested one", "ggml-base.en.bin", false));
        modelBox.Items.Add(new ModelChoice("Fast (tiny.en) - quicker, less accurate", "ggml-tiny.en.bin", false));
        modelBox.Items.Add(new ModelChoice("Foreign speech to English (multilingual) - slower", "ggml-small-q5_1.bin", true));
        modelBox.SelectedIndex = 0;

        Controls.AddRange(new Control[] { pathBox, browseBtn, modelBox, benchBox, bar, status, startBtn, cancelBtn });

        browseBtn.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Choose a video",
                Filter = "Video and audio files|*.mp4;*.mkv;*.avi;*.mov;*.m4v;*.webm;*.ts;*.wmv;*.flv;*.3gp;*.mpg;*.mpeg;*.mp3;*.m4a;*.wav|All files|*.*"
            };
            if (dlg.ShowDialog(this) == DialogResult.OK) pathBox.Text = dlg.FileName;
        };

        startBtn.Click += async (_, _) => await RunAsync();

        cancelBtn.Click += (_, _) =>
        {
            cancelled = true;
            try { current?.Kill(true); } catch { }
        };

        DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy;
        };
        DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                pathBox.Text = files[0];
        };

        FormClosing += (_, _) =>
        {
            cancelled = true;
            try { current?.Kill(true); } catch { }
        };

        Shown += async (_, _) =>
        {
            if (initialFile != null && File.Exists(initialFile))
            {
                pathBox.Text = initialFile;
                await RunAsync();
            }
        };
    }

    // ---------------------------------------------------------------- main job

    async Task RunAsync()
    {
        if (!startBtn.Enabled) return;

        var video = pathBox.Text.Trim().Trim('"');
        if (!File.Exists(video)) { Info("Choose a video file first."); return; }

        var baseDir = AppContext.BaseDirectory;
        var ffmpeg = Path.Combine(baseDir, "engine", "ffmpeg.exe");
        var whisper = Path.Combine(baseDir, "engine", "whisper-cli.exe");
        var choice = (ModelChoice)modelBox.SelectedItem!;
        var model = Path.Combine(baseDir, "models", choice.File);
        foreach (var f in new[] { ffmpeg, whisper, model })
        {
            if (!File.Exists(f))
            {
                Info("A required file is missing:\n" + f + "\n\nTry reinstalling SubGen.");
                return;
            }
        }

        bool bench = benchBox.Checked;
        var finalSrt = Path.ChangeExtension(video, ".srt");

        if (!bench && File.Exists(finalSrt) &&
            MessageBox.Show(this, "A subtitle file already exists:\n" + finalSrt + "\n\nReplace it? (The old one is kept as a .bak copy.)",
                "SubGen", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var id = Guid.NewGuid().ToString("N");
        var wav = Path.Combine(Path.GetTempPath(), "subgen_" + id + ".wav");
        var outBase = Path.Combine(Path.GetTempPath(), "subgen_" + id);
        var producedSrt = outBase + ".srt";

        var tail = new Queue<string>();
        void Remember(string line)
        {
            lock (tail)
            {
                tail.Enqueue(line);
                if (tail.Count > 12) tail.Dequeue();
            }
        }
        string TailText() { lock (tail) return string.Join("\n", tail); }

        cancelled = false;
        SetBusy(true);
        var total = Stopwatch.StartNew();
        try
        {
            // Step 1: pull the audio out of the video.
            SetMarquee("Extracting audio...");
            var ffArgs = new List<string> { "-nostdin", "-y", "-v", "error", "-i", video };
            if (bench) { ffArgs.Add("-t"); ffArgs.Add("300"); }
            ffArgs.AddRange(new[] { "-map", "0:a:0", "-vn", "-ac", "1", "-ar", "16000", "-c:a", "pcm_s16le", wav });

            int code = await RunProc(ffmpeg, ffArgs, Remember);
            if (cancelled) { SetStatus("Cancelled."); return; }
            if (code != 0 || !File.Exists(wav) || new FileInfo(wav).Length < 1000)
            {
                SetStatus("Failed.");
                Info("Could not read audio from this file (no audio track?).\n\n" + TailText());
                return;
            }

            // Step 2: speech to text.
            double audioSeconds = Math.Max(1, (new FileInfo(wav).Length - 44) / 32000.0);
            lock (tail) tail.Clear();
            SetProgress(0);

            var wArgs = new List<string> { "-m", model, "-f", wav, "-l", choice.Foreign ? "auto" : "en" };
            if (choice.Foreign) wArgs.Add("-tr"); // translate foreign speech into English
            if (!bench) { wArgs.Add("-osrt"); wArgs.Add("-of"); wArgs.Add(outBase); }
            wArgs.AddRange(new[] { "-t", Math.Clamp(Environment.ProcessorCount, 1, 4).ToString(), "-pp" });

            var sw = Stopwatch.StartNew();
            code = await RunProc(whisper, wArgs, line =>
            {
                var m = ProgressRx.Match(line);
                if (m.Success) SetProgress(int.Parse(m.Groups[1].Value));
                else Remember(line);
            });
            sw.Stop();

            if (cancelled) { SetStatus("Cancelled."); return; }
            if (code != 0)
            {
                SetStatus("Failed.");
                Info("The speech engine stopped with an error.\n\n" + TailText());
                return;
            }

            if (bench)
            {
                double rtf = sw.Elapsed.TotalSeconds / audioSeconds;
                SetProgress(100);
                var msg = $"Processed {audioSeconds:0} s of audio in {sw.Elapsed.TotalSeconds:0.0} s.\n" +
                          $"Real-time factor: {rtf:0.00} (below 1.00 = faster than real time).\n" +
                          $"For comparison, the tablet scored 0.22 with base.en.\n" +
                          $"A 2-hour film would take roughly {rtf * 120:0} minutes.";
                SetStatus("Benchmark done.");
                Info(msg);
                return;
            }

            if (!File.Exists(producedSrt))
            {
                SetStatus("Failed.");
                Info("No subtitle file was produced. Does this video have any speech?");
                return;
            }

            var text = File.ReadAllText(producedSrt);
            if (!text.Contains(" --> "))
            {
                SetStatus("Failed.");
                Info("The subtitle file came out empty. Does this video have any speech?");
                return;
            }
            if (!text.EndsWith("\n")) text += "\n";

            // Write to a temp file in the video's folder first, then swap it in.
            var tmpSrt = Path.Combine(Path.GetDirectoryName(video)!, ".subgen-" + id + ".tmp");
            try
            {
                File.WriteAllText(tmpSrt, text, new System.Text.UTF8Encoding(false));
                if (File.Exists(finalSrt))
                {
                    var bak = finalSrt + ".bak";
                    for (int n = 2; File.Exists(bak); n++) bak = finalSrt + ".bak" + n;
                    File.Copy(finalSrt, bak);
                }
                File.Move(tmpSrt, finalSrt, true);
            }
            finally
            {
                try { if (File.Exists(tmpSrt)) File.Delete(tmpSrt); } catch { }
            }

            SetProgress(100);
            SetStatus($"Done in {total.Elapsed:hh\\:mm\\:ss}. Saved: {finalSrt}");
            Info("Subtitles saved next to the video:\n" + finalSrt);
        }
        catch (Exception ex)
        {
            SetStatus("Failed.");
            Info("Something went wrong:\n" + ex.Message);
        }
        finally
        {
            try { if (File.Exists(wav)) File.Delete(wav); } catch { }
            try { if (File.Exists(producedSrt)) File.Delete(producedSrt); } catch { }
            current = null;
            SetBusy(false);
        }
    }

    // ---------------------------------------------------------------- helpers

    Task<int> RunProc(string exe, IEnumerable<string> args, Action<string> onLine)
    {
        var tcs = new TaskCompletionSource<int>();
        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            WorkingDirectory = Path.GetDirectoryName(exe)!
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        p.OutputDataReceived += (_, e) => { if (e.Data != null) onLine(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) onLine(e.Data); };
        p.Exited += (_, _) =>
        {
            p.WaitForExit(); // make sure all output lines were delivered
            tcs.TrySetResult(p.ExitCode);
        };

        current = p;
        p.Start();
        p.StandardInput.Close();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        return tcs.Task;
    }

    void UI(Action a)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(a); else a();
    }

    void SetStatus(string text) => UI(() => status.Text = text);

    void SetMarquee(string text) => UI(() =>
    {
        status.Text = text;
        bar.Style = ProgressBarStyle.Marquee;
        bar.MarqueeAnimationSpeed = 30;
    });

    void SetProgress(int percent) => UI(() =>
    {
        bar.Style = ProgressBarStyle.Continuous;
        bar.Value = Math.Clamp(percent, 0, 100);
        status.Text = $"Transcribing... {bar.Value}%";
    });

    void SetBusy(bool busy) => UI(() =>
    {
        startBtn.Enabled = !busy;
        browseBtn.Enabled = !busy;
        modelBox.Enabled = !busy;
        benchBox.Enabled = !busy;
        pathBox.Enabled = !busy;
        cancelBtn.Enabled = busy;
        if (!busy)
        {
            bar.Style = ProgressBarStyle.Continuous;
        }
    });

    void Info(string text) =>
        UI(() => MessageBox.Show(this, text, "SubGen", MessageBoxButtons.OK, MessageBoxIcon.Information));
}
