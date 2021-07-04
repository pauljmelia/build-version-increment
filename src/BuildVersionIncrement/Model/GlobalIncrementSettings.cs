// ----------------------------------------------------------------------
// Project:     BuildVersionIncrement
// Module Name: GlobalIncrementSettings.cs
// ----------------------------------------------------------------------
// Created and maintained by Paul J. Melia.
// Copyright © 2016 Paul J. Melia.
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

namespace BuildVersionIncrement.Model
{
	using System;
	using System.ComponentModel;

	using Properties;

	internal class GlobalIncrementSettings : IncrementSettingsBase
	{
		public enum ApplyGlobalSettings
		{
			OnlyWhenChosen,
			AsDefault,
			Always
		}

		public static ApplyGlobalSettings ApplySettings => (ApplyGlobalSettings)Enum.Parse(typeof(ApplyGlobalSettings), Settings.Default.GlobalApply);

		[Browsable(true)]
		[Category("Global")]
		[DisplayName("Apply Global Settings")]
		[Description("The setting when to use the Global Settings")]
		[DefaultValue(typeof(ApplyGlobalSettings), "OnlyWhenChosen")]
		public ApplyGlobalSettings Apply { get; set; }

		public override void Load()
		{
			var vs = new VersioningStyle();
			vs.FromGlobalVariable(
				$"{Settings.Default.GlobalMajor}.{Settings.Default.GlobalMinor}.{Settings.Default.GlobalBuild}.{Settings.Default.GlobalRevision}");
			VersioningStyle = vs;

			BuildAction =
				(BuildActionType)Enum.Parse(typeof(BuildActionType), Settings.Default.GlobalBuildAction);
			AutoUpdateAssemblyVersion = Settings.Default.GlobalAutoUpdateAssemblyVersion;
			AutoUpdateFileVersion = Settings.Default.GlobalAutoUpdateFileVersion;
			//AutoUpdateVsixmanifestVersion = Settings.Default.GlobalAutoUpdateVsixmanifestVersion;
			ReplaceNonNumerics = Settings.Default.GlobalReplaceNonNumeric;
			IsUniversalTime = Settings.Default.GlobalUseUniversalClock;
			IncrementBeforeBuild = Settings.Default.GlobalIncrementBeforeBuild;
			StartDate = Settings.Default.GlobalStartDate;
			DetectChanges = Settings.Default.DetectChanges;
			Apply =
				(ApplyGlobalSettings)Enum.Parse(typeof(ApplyGlobalSettings), Settings.Default.GlobalApply);
		}

		public override void Reset()
		{
			AutoUpdateAssemblyVersion = false;
			AutoUpdateFileVersion = false;
			//AutoUpdateVsixmanifestVersion = false;

			BuildAction = BuildActionType.Both;

			var versioningStyle = VersioningStyle.GetDefaultGlobalVariable();
			VersioningStyle.FromGlobalVariable(versioningStyle);

			IsUniversalTime = false;
			StartDate = new DateTime(2000, 1, 1);
			ReplaceNonNumerics = true;
			IncrementBeforeBuild = true;
			DetectChanges = true;

			Apply = ApplyGlobalSettings.OnlyWhenChosen;
		}

		public override void Save()
		{
			Settings.Default.GlobalMajor = VersioningStyle.Major.Name;
			Settings.Default.GlobalMinor = VersioningStyle.Minor.Name;
			Settings.Default.GlobalBuild = VersioningStyle.Build.Name;
			Settings.Default.GlobalRevision = VersioningStyle.Revision.Name;

			Settings.Default.GlobalBuildAction = BuildAction.ToString();
			Settings.Default.GlobalAutoUpdateAssemblyVersion = AutoUpdateAssemblyVersion;
			Settings.Default.GlobalAutoUpdateFileVersion = AutoUpdateFileVersion;
			//Settings.Default.GlobalAutoUpdateVsixmanifestVersion = AutoUpdateVsixmanifestVersion;
			Settings.Default.GlobalStartDate = StartDate;
			Settings.Default.GlobalReplaceNonNumeric = ReplaceNonNumerics;
			Settings.Default.GlobalUseUniversalClock = IsUniversalTime;
			Settings.Default.GlobalIncrementBeforeBuild = IncrementBeforeBuild;
			Settings.Default.DetectChanges = DetectChanges;

			Settings.Default.GlobalApply = Apply.ToString();

			Settings.Default.Save();
		}
	}
}