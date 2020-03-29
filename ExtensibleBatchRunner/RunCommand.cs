using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace ExtensibleBatchRunner
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class RunCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("7905e230-cfef-4b9c-9f32-1e972ea4bb7e");

        private static Guid OutputWindow = new Guid("AB088A53-E491-4925-9FA2-B1B02CF89057");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly ExtensibleBatchRunnerPackage package;

        private readonly VsServiceInfo _serviceInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private RunCommand(ExtensibleBatchRunnerPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            _serviceInfo = new VsServiceInfo();
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static RunCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(ExtensibleBatchRunnerPackage package)
        {
            // Switch to the main thread - the call to AddCommand in RunCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new RunCommand(package, commandService);

            IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            string customTitle = "Extensible batch engine";
            outWindow.CreatePane(ref OutputWindow, customTitle, 1, 1);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var selectedItemPaths = _serviceInfo.SolutionSelectionService.GetSelectedItemsPaths();

            var allPaths = _serviceInfo.InnerPathFinder.GetAllInnerPaths(selectedItemPaths);

            if (!allPaths.Any())
            {
                return;
            }

            var solutionFile = new FileInfo(package.GetSolution().FullName);
            var projectFile = ProjectHelper.GetProjectFilePath(allPaths[0]);
            var tempScript = CreateTempBatchScript(Path.GetFullPath(allPaths[0]), solutionFile, projectFile);


            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await Task.Run(() =>
                {
                    try
                    {
                        var p = new Process();

                        p.StartInfo.FileName = tempScript;
                        p.StartInfo.WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(allPaths[0]));
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.RedirectStandardError = true;

                        p.OutputDataReceived += async (s, args) => await Output(args.Data);
                        p.ErrorDataReceived += async (s, args) => await Output(args.Data);

                        p.Start();
                        p.BeginOutputReadLine();
                        p.BeginErrorReadLine();

                        p.WaitForExit();
                        Output($"Script exited with exit code: {p.ExitCode}.");
                    }
                    finally
                    {
                        File.Delete(tempScript);
                    }
                });
            });
        }

        private async Task Output(string st)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            IVsOutputWindowPane customPane;
            outWindow.GetPane(ref OutputWindow, out customPane);

            customPane.OutputString(st + Environment.NewLine);
            customPane.Activate(); // Brings this pane into view
        }

        private string CreateTempBatchScript(string batscript, FileInfo solutionFile, FileInfo projectFile)
        {
            string fileName = Path.GetTempPath() + Guid.NewGuid().ToString() + ".bat";

            string script = File.ReadAllText(batscript)
                .ResolvePathVariables("solutionFile", solutionFile)
                .ResolvePathVariables("projectFile", projectFile)
                .ResolvePathVariables("file", new FileInfo(batscript));

            File.WriteAllText(fileName, script);

            return fileName;
        }
    }
}
