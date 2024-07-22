using System;
using System.Diagnostics;
using System.IO;

namespace GitSyncApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string directory = @"N:\mystream\Steam平台\userdata\1068936900";
            string repositoryUrl = "https://gitee.com/starsea1811230375/steamconfig.git"; // 替换为你的Git存储库URL
            string gitInstallerPath = @"D:\Programs\Git-2.45.2-64-bit.exe"; // Git安装包的路径

            EnsureGitInstalled(gitInstallerPath);
            InitializeGitRepository(directory, repositoryUrl);

            static void ConfigureGit(string directory)
            {
                ExecuteGitCommand(directory, "config core.autocrlf false");
            }


            // 程序开始时进行一次同步操作
            Console.WriteLine("Performing initial sync...");
            bool initialSyncSuccess = SyncWithGit(directory);

            if (initialSyncSuccess)
            {
                Console.WriteLine("Initial sync successful.");
            }
            else
            {
                Console.WriteLine("Initial sync failed, retrying...");
                while (!SyncWithGit(directory))
                {
                    Console.WriteLine("Retrying...");
                }
                Console.WriteLine("Initial sync successful after retry.");
            }

            StartFileWatcher(directory);

            Console.WriteLine("Press 'q' to quit.");
            while (Console.Read() != 'q') ;
        }

        static void EnsureGitInstalled(string gitInstallerPath)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = "/C git --version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                    process.ErrorDataReceived += (sender, e) => Console.WriteLine("ERROR: " + e.Data);

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        InstallGit(gitInstallerPath);
                    }
                    else
                    {
                        Console.WriteLine("Git is already installed.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        static void InstallGit(string gitInstallerPath)
        {
            Console.WriteLine("Installing Git...");

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = gitInstallerPath,
                Arguments = "/SILENT", // 根据Git安装程序的参数进行调整，/SILENT用于静默安装
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                process.ErrorDataReceived += (sender, e) => Console.WriteLine("ERROR: " + e.Data);

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("Git installation completed.");
                }
                else
                {
                    Console.WriteLine("Git installation failed.");
                }
            }
        }

        static void InitializeGitRepository(string directory, string repositoryUrl)
        {
            if (!Directory.Exists(Path.Combine(directory, ".git")))
            {
                Console.WriteLine("Initializing Git repository...");

                ExecuteGitCommand(directory, "init");
                ExecuteGitCommand(directory, "pull https://gitee.com/starsea1811230375/steamconfig.git");
                ExecuteGitCommand(directory, $"remote add origin2 {repositoryUrl}");
                ExecuteGitCommand(directory, "add .");
                ExecuteGitCommand(directory, "commit -m \"Initial commit\"");
                ExecuteGitCommand(directory, "push -u origin2 master");
            }
            else
            {
                Console.WriteLine("Git repository already initialized.");
            }
        }

        static void StartFileWatcher(string directory)
        {
            FileSystemWatcher watcher = new FileSystemWatcher
            {
                Path = directory,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                Filter = "*.*", // 监控所有文件类型
                IncludeSubdirectories = true // 包含子目录
            };

            watcher.Changed += (sender, e) => OnChanged(directory);
            watcher.Created += (sender, e) => OnChanged(directory);
            watcher.Renamed += (sender, e) => OnChanged(directory);
            watcher.Deleted += (sender, e) => OnChanged(directory);

            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Monitoring changes in " + directory);
        }

        static void OnChanged(string directory)
        {
            Console.WriteLine("File changed, attempting to sync with Git...");
            bool success = SyncWithGit(directory);

            if (!success)
            {
                Console.WriteLine("Sync failed, retrying...");
                while (!SyncWithGit(directory))
                {
                    Console.WriteLine("Retrying...");
                }
            }
            else
            {
                Console.WriteLine("Sync successful.");
            }
        }


        static void OnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"File {e.ChangeType}: {e.FullPath}, attempting to sync with Git...");
            bool success = SyncWithGit(e.FullPath);

            if (!success)
            {
                Console.WriteLine("Sync failed, retrying...");
                while (!SyncWithGit(e.FullPath))
                {
                    Console.WriteLine("Retrying...");
                }
            }
            else
            {
                Console.WriteLine("Sync successful.");
            }
        }


        static bool SyncWithGit(string directory)
        {
            return ExecuteGitCommand(directory, "add .")
                && ExecuteGitCommand(directory, $"commit -m \"Auto-sync at {DateTime.Now}\"")
                && ExecuteGitCommand(directory, "pull https://gitee.com/starsea1811230375/steamconfig.git")
                && ExecuteGitCommand(directory, "push -u origin2 master");
        }

        static bool ExecuteGitCommand(string directory, string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                WorkingDirectory = directory,
                FileName = "cmd.exe",
                Arguments = $"/C git {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                process.ErrorDataReceived += (sender, e) => Console.WriteLine("ERROR: " + e.Data);

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                Console.WriteLine($"Git command '{arguments}' completed with exit code: " + process.ExitCode);
                return process.ExitCode == 0;
            }
        }

    }
}
