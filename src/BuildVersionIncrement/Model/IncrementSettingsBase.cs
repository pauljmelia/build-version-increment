// ----------------------------------------------------------------------
// Project:     BuildVersionIncrement
// Module Name: IncrementSettingsBase.cs
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

	using Logging;

	using UI;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	//using System.ComponentModel.Design;

	internal abstract class IncrementSettingsBase
	{
		[Category("Increment Settings")]
		[Description(
			"Auto update the assembly version. Note that setting this to true on solution level will have no effect on building individual projects."
			)]
		[DisplayName("Update AssemblyVersion")]
		[DefaultValue(false)]
		public bool AutoUpdateAssemblyVersion { get; set; }

		//[Category("Increment Settings")]
		//[Description(
		//	"Auto update the version specified in a vsixmanifest file associated with the project. Note that setting this to true at the solution level or on a project which does not contain a vsixmanifest file will have no effect."
		//)]
		//[DisplayName("Update vsixmanifest Version")]
		//[DefaultValue(false)]
		//public bool AutoUpdateVsixmanifestVersion { get; set; }

		[Category("Increment Settings")]
		[Description(
			"Auto update the file version. Note that setting this to true on solution level will have no effect on building individual projects."
			)]
		[DisplayName("Update AssemblyFileVersion")]
		[DefaultValue(false)]
		public bool AutoUpdateFileVersion { get; set; }

		[Category("Condition")]
		[DefaultValue(BuildActionType.Both)]
		[DisplayName("Build Action")]
		[Description("Set this to the desired build action when the auto update should occur.")]
		public BuildActionType BuildAction { get; set; }

		[Category("Condition")]
		[DefaultValue(true)]
		[DisplayName("Detect changes")]
		[Description(
			"Set this to true if you want to detect item changes in order to make version increment.")]
		public bool DetectChanges { get; set; } = true;

		[Category("Condition")]
		[Description("If the increment should be executed before the build.")]
		[DisplayName("Increment Before Build")]
		[DefaultValue(true)]
		public bool IncrementBeforeBuild { get; set; }

		[Category("Increment Settings")]
		[Description("Indicates wheter to use Coordinated Universal Time (UTC) time stamps.")]
		[DisplayName("Use Coordinated Universal Time")]
		[DefaultValue(false)]
		public bool IsUniversalTime { get; set; }

		[Category("Increment Settings")]
		[Description("If non-numeric values within the version should be replaced with a zero.")]
		[DisplayName("Replace Non-Numerics")]
		[DefaultValue(true)]
		public bool ReplaceNonNumerics { get; set; }

		[Category("Increment Settings")]
		[Description("The start date to use.")]
		[DisplayName("Start Date")]
		[DefaultValue(typeof(DateTime), "2000/01/01")]
		[Editor(typeof(DatePickerEditor), typeof(DatePickerEditor))]
		public DateTime StartDate { get; set; }

		[Browsable(true)]
		[Category("Increment Settings")]
		[DisplayName("Versioning Style")]
		[Description("The version increment style settings.")]
		[ExpandableObject]
		public VersioningStyle VersioningStyle { get; set; } = new VersioningStyle();

		public virtual void CopyFrom(IncrementSettingsBase source)
		{
			try
			{
				VersioningStyle = new VersioningStyle(source.VersioningStyle);
				AutoUpdateAssemblyVersion = source.AutoUpdateAssemblyVersion;
				AutoUpdateFileVersion = source.AutoUpdateFileVersion;
				//AutoUpdateVsixmanifestVersion = source.AutoUpdateVsixmanifestVersion;
				BuildAction = source.BuildAction;
				DetectChanges = source.DetectChanges;
				IncrementBeforeBuild = source.IncrementBeforeBuild;
				IsUniversalTime = source.IsUniversalTime;
				ReplaceNonNumerics = source.ReplaceNonNumerics;
				StartDate = source.StartDate;
			}
			catch(Exception ex)
			{
				Logger.Write($"Exception occured while copying settings:\n{ex}");
			}
		}

		public abstract void Load();
		public abstract void Reset();
		public abstract void Save();

		private bool ShouldSerializeVersioningStyle()
		{
			return VersioningStyle.ToString() != "None.None.None.None";
		}
	}
}