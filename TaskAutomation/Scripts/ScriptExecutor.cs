using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TaskAutomation.Scripts
{
    public sealed class ScriptExecutor : IScriptExecutor
    {
        private readonly ILogger<ScriptExecutor> _logger;

        public ScriptExecutor(ILogger<ScriptExecutor> logger)
        {
            _logger = logger;
        }

        public async Task ExecuteScriptFile(string scriptPath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(scriptPath))
                throw new ArgumentException("Script path is null/empty.", nameof(scriptPath));

            scriptPath = Path.GetFullPath(scriptPath);

            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("Script file not found.", scriptPath);

            var (fileName, arguments, workingDir) = ResolveCommand(scriptPath);

            _logger.LogInformation("Starte Skript {Script} mit Interpreter {Exe} {Args} (WorkingDir={Dir})",
                scriptPath, fileName, arguments, workingDir);

            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true
            };

            proc.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    _logger.LogDebug("[{Script}] STDOUT: {Line}", Path.GetFileName(scriptPath), e.Data);
            };
            proc.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    _logger.LogWarning("[{Script}] STDERR: {Line}", Path.GetFileName(scriptPath), e.Data);
            };

            if (!proc.Start())
                throw new InvalidOperationException($"Failed to start process for script '{scriptPath}'.");

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            using var reg = ct.Register(() =>
            {
                try
                {
                    if (!proc.HasExited)
                    {
                        _logger.LogWarning("Cancellation angefordert – beende Prozess {Pid} ({Script})", proc.Id, scriptPath);
                        KillProcessTree(proc);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Abbrechen des Prozesses {Pid} ({Script})", proc.Id, scriptPath);
                }
            });

            var sw = Stopwatch.StartNew();

            try
            {
                await proc.WaitForExitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Ausführung von {Script} abgebrochen.", scriptPath);
            }

            sw.Stop();

            if (proc.ExitCode == 0)
            {
                _logger.LogInformation("Skript {Script} erfolgreich beendet (Dauer {Duration} ms).",
                    scriptPath, sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError("Skript {Script} beendet mit ExitCode {ExitCode} (Dauer {Duration} ms).",
                    scriptPath, proc.ExitCode, sw.ElapsedMilliseconds);
                throw new InvalidOperationException($"Script '{scriptPath}' exited with code {proc.ExitCode}.");
            }
        }

        // Rest wie gehabt …
        private static (string fileName, string arguments, string workingDir) ResolveCommand(string scriptPath)
        {
            var ext = Path.GetExtension(scriptPath).ToLowerInvariant();
            var dir = Path.GetDirectoryName(scriptPath) ?? Environment.CurrentDirectory;
            string? shebang = TryReadShebang(scriptPath);

            switch (ext)
            {
                case ".ps1":
                    var pwsh = FindOnPath("pwsh.exe");
                    var ps = pwsh ?? FindOnPath("powershell.exe") ?? "powershell.exe";
                    var args = $"-NoLogo -NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"";
                    return (ps, args, dir);

                case ".bat":
                case ".cmd":
                    return ("cmd.exe", $"/c \"{scriptPath}\"", dir);

                case ".sh":
                    var bash = FindOnPath("bash.exe") ?? "bash";
                    return (bash, $"\"{scriptPath}\"", dir);

                case ".py":
                    var python = FindOnPath("python.exe") ?? FindOnPath("py.exe") ?? "python";
                    return (python, $"\"{scriptPath}\"", dir);

                case ".js":
                    var node = FindOnPath("node.exe") ?? "node";
                    return (node, $"\"{scriptPath}\"", dir);

                case ".vbs":
                case ".wsf":
                    var cscript = FindOnPath("cscript.exe") ?? "cscript.exe";
                    return (cscript, $"//nologo \"{scriptPath}\"", dir);

                case ".exe":
                    return (scriptPath, "", dir);

                default:
                    if (!string.IsNullOrEmpty(shebang))
                    {
                        var (exe, shebangArgs) = ParseShebang(shebang);
                        var exePath = FindOnPath(exe) ?? exe;
                        var args2 = string.IsNullOrEmpty(shebangArgs)
                            ? $"\"{scriptPath}\""
                            : $"{shebangArgs} \"{scriptPath}\"";
                        return (exePath, args2, dir);
                    }
                    return (scriptPath, "", dir);
            }
        }

        private static string? TryReadShebang(string scriptPath)
        {
            try
            {
                using var sr = new StreamReader(scriptPath);
                var first = sr.ReadLine();
                if (first != null && first.StartsWith("#!"))
                    return first.Substring(2).Trim();
            }
            catch { }
            return null;
        }

        private static (string exe, string args) ParseShebang(string shebang)
        {
            var parts = shebang.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return (shebang, "");
            var exe = parts[0];
            var args = string.Join(" ", parts.Skip(1));
            return (exe, args);
        }

        private static string? FindOnPath(string fileName)
        {
            try
            {
                if (Path.IsPathRooted(fileName) && File.Exists(fileName))
                    return fileName;

                var envPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                foreach (var p in envPath.Split(Path.PathSeparator))
                {
                    if (string.IsNullOrWhiteSpace(p)) continue;
                    var candidate = Path.Combine(p.Trim(), fileName);
                    if (File.Exists(candidate))
                        return candidate;
                }
            }
            catch { }
            return null;
        }

        private static void KillProcessTree(Process proc)
        {
            try
            {
                if (proc.HasExited) return;
                proc.Kill(entireProcessTree: true);
            }
            catch { }
        }
    }
}
