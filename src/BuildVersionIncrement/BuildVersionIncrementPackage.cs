// ----------------------------------------------------------------------
// Project:     BuildVersionIncrement
// Module Name: BuildVersionIncrementPackage.cs
// ----------------------------------------------------------------------
// Created and maintained by Paul J. Melia.
// Copyright © 2019 Paul J. Melia.
// All rights reserved.
// ----------------------------------------------------------------------
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR 
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// ----------------------------------------------------------------------

namespace BuildVersionIncrement
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.Runtime.InteropServices;
	using System.Threading;

	using Commands;

	using EnvDTE;

	using log4net;

	using Logging;

	using Microsoft.VisualStudio.Shell;
	using Microsoft.VisualStudio.Shell.Interop;

	using Task = System.Threading.Tasks.Task;

	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]

	// Info on this package for Help/About
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[Guid(PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules",
		"SA1650:ElementDocumentationMustBeSpelledCorrectly",
		Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	[ProvideAppCommandLine("IncrementVersion",
		typeof(BuildVersionIncrementPackage),
		Arguments = "0",
		DemandLoad = 1,
		PackageGuid = PackageGuidString)]
	[ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
	[ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
	public sealed class BuildVersionIncrementPackage : AsyncPackage
	{
		public const string PackageGuidString = "d9498ed1-f738-4c84-9cbc-82ab0163d742";
		private BuildEvents _buildEvents;
		private BuildVersionIncrementor _buildVersionIncrementor;

		internal bool IsCommandLine { get; set; }

		private DTE Dte
		{
			get
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				return (DTE)GetService(typeof(DTE));
			}
		}

		protected override async Task InitializeAsync(CancellationToken cancellationToken,
		                                              IProgress<ServiceProgressData> progress)
		{
			await base.InitializeAsync(cancellationToken, progress);

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			GlobalContext.Properties["package"] = this;
			CheckCommandLine();
			Logger.Initialise(IsCommandLine);
			SettingsCommand.Initialize(this);
			VersionCommand.Initialize(this);

			_buildVersionIncrementor = new BuildVersionIncrementor(this);
			_buildEvents = Dte.Events.BuildEvents;
			_buildVersionIncrementor.InitializeIncrementors();

#pragma warning disable VSTHRD101 // Avoid unsupported async delegates
			_buildEvents.OnBuildBegin += async (s, e) =>
				                             await _buildVersionIncrementor.OnBuildBeginAsync(s, e);
			_buildEvents.OnBuildDone += async (s, e) =>
				                            await _buildVersionIncrementor.OnBuildDoneAsync(s, e);
#pragma warning restore VSTHRD101 // Avoid unsupported async delegates
		}

		private void CheckCommandLine()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var commandLine = (IVsAppCommandLine)GetService(typeof(IVsAppCommandLine));

			var isPresent = 0;

			if (commandLine != null)
			{
				commandLine.GetOption("IncrementVersion", out isPresent, out _);
			}

			IsCommandLine = isPresent != 0;
		}
	}
}