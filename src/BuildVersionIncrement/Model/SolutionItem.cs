// ----------------------------------------------------------------------
// Project:     BuildVersionIncrement
// Module Name: SolutionItem.cs
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
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows.Media;

    using Annotations;

    using EnvDTE;

    using Extensions;

    using Logging;

    using Microsoft.VisualStudio.Shell;

    internal class SolutionItem : INotifyPropertyChanged
    {
        private readonly object _item;
        private ImageSource _icon;
        private bool _isExpanded;
        private bool _isSelected;

        public SolutionItem(Package package, Solution solution) : this(package, solution, true) { }

        public SolutionItem(Package package, Solution solution, bool recursive)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IncrementSettings = new SolutionItemIncrementSettings(this);

            Package = package ?? throw new ArgumentNullException(nameof(package));
            _item = solution ?? throw new ArgumentNullException(nameof(solution));
            ItemType = SolutionItemType.Solution;
            Icon = IconReader.AddFileIcon(solution.FileName).ToImageSource();
            Name = Path.GetFileNameWithoutExtension(solution.FileName);
            Filename = solution.FileName;
            UniqueName = Name;

            string fileExt = (!string.IsNullOrWhiteSpace(Filename) && !string.IsNullOrWhiteSpace(Path.GetExtension(Filename))) ? Path.GetExtension(Filename) : string.Empty;// Path.GetExtension(project.UniqueName);
            //Logger.Write($"Project Name={Name}, Ext={fileExt}\r\n", LogLevel.Info);
            if (!string.IsNullOrWhiteSpace(fileExt) && fileExt.Equals(".shproj", StringComparison.OrdinalIgnoreCase))
                Logger.Write($"Project extension matched a shared project type, skipping... Name={Name}, Ext={fileExt}\r\n", LogLevel.Info);
            else
                GetGlobalVariables();

            if (recursive)
                FillSolutionTree(package, this, solution.Projects);
        }

        //private SolutionItem(Package package, Project project) : this(package, project, true) {}

        private SolutionItem(Package package, Project project, bool recursive)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IncrementSettings = new SolutionItemIncrementSettings(this);

            Package = package ?? throw new ArgumentNullException(nameof(package));
            _item = project ?? throw new ArgumentNullException(nameof(project));
            Name = project.Name;

            Filename = project.FileName;
            UniqueName = project.UniqueName;

            if (!string.IsNullOrEmpty(Filename) && string.IsNullOrEmpty(Path.GetExtension(Filename)))
                Filename += Path.GetExtension(project.UniqueName);
            string fileExt = (!string.IsNullOrWhiteSpace(Filename) && !string.IsNullOrWhiteSpace(Path.GetExtension(Filename))) ? Path.GetExtension(Filename) : Path.GetExtension(project.UniqueName);
            //Logger.Write($"Project Name={Name}, Ext={fileExt}\r\n", LogLevel.Info);

            if (string.IsNullOrEmpty(project.FullName))
            {
                ItemType = SolutionItemType.Folder;
                Icon = IconReader.AddFolderIcon(FolderType.Closed).ToImageSource();
                if (recursive)
                {
                    FillSolutionTree(package, this, project.ProjectItems);
                }
            }
            else
            {
                ItemType = SolutionItemType.Project;
                Icon = IconReader.AddFileIcon(Filename).ToImageSource();

                if (!string.IsNullOrWhiteSpace(fileExt) && fileExt.Equals(".shproj", StringComparison.OrdinalIgnoreCase))
                    Logger.Write($"Project extension matched a shared project type, skipping... Name={Name}, Ext={fileExt}\r\n", LogLevel.Info);
                else
                    GetGlobalVariables();
            }
        }

        [Browsable(false)]
        public BuildDependency BuildDependency
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return DTE.Solution.SolutionBuild.BuildDependencies.Item(UniqueName);
            }
        }

        [Browsable(false)]
        public DTE DTE
        {
            get
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                switch (ItemType)
                {
                    case SolutionItemType.Project:
                        return Project.DTE;

                    case SolutionItemType.Solution:
                        return Solution.DTE;
                    default:
                        return null;
                }
            }
        }

        public string Filename { get; }

        [Browsable(false)]
        public Globals Globals
        {
            get
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                switch (ItemType)
                {
                    case SolutionItemType.Solution:
                        return Solution.Globals;

                    case SolutionItemType.Project:
                        return Project.Globals;

                    default:
                        return null;
                }
            }
        }

#if DEBUG

        public string Guid
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return ItemType != SolutionItemType.Solution ? Project.Kind : string.Empty;
            }
        }
#endif

        public ImageSource Icon
        {
            get { return _icon; }
            set
            {
                if (Equals(value, _icon))
                {
                    return;
                }
                _icon = value;
                OnPropertyChanged();
            }
        }

        [Browsable(false)]
        public SolutionItemIncrementSettings IncrementSettings { get; }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value == _isExpanded)
                {
                    return;
                }
                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value == _isSelected)
                {
                    return;
                }
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly",
            MessageId = "SubItems")]
        [Browsable(false)]
        public ObservableCollection<SolutionItem> Items
            =>
                new ObservableCollection<SolutionItem>(
                    SubItems.Where(
                        i =>
                        i.ItemType == SolutionItemType.Solution || i.ItemType == SolutionItemType.Project
                        || (i.ItemType == SolutionItemType.Folder && i.SubItems.Any())).OrderBy(i => i.Name));

        [Browsable(false)]
        public SolutionItemType ItemType { get; set; }

        public string Name { get; set; }

        [Browsable(false)]
        public Package Package { get; }

        [Browsable(false)]
        public Project Project
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return (Project)_item;
            }
        }

        public LanguageType ProjectType { get; set; } = LanguageType.None;

        [Browsable(false)]
        public Solution Solution
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return (Solution)_item;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly",
            MessageId = "SubItems")]
        [Browsable(false)]
        public List<SolutionItem> SubItems { get; } = new List<SolutionItem>();

        [Browsable(false)]
        public string UniqueName { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public static SolutionItem ConstructSolutionItem(Package connect, Project project)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            return ConstructSolutionItem(connect, project, true);
        }

        public static SolutionItem ConstructSolutionItem(Package connect, Project project, bool recursive)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            SolutionItem ret = null;

            if (IsValidSolutionItem(project))
            {
                ret = new SolutionItem(connect, project, recursive);
            }

            return ret;
        }

        public void ApplyGlobalSettings()
        {
            var globalSett = new GlobalIncrementSettings();

            try
            {
                globalSett.Load();
            }
            catch (Exception ex)
            {
                throw (new ApplicationException(
                    "Exception occured while applying global settings to the solution item (" + UniqueName + ").",
                    ex));
            }

            IncrementSettings.CopyFrom(globalSett);
        }

        public ProjectItem FindProjectItem(string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ProjectItem ret = null;

            if (ItemType == SolutionItemType.Project)
            {
                ret = FindProjectItem(Project.ProjectItems, name);
            }

            return ret;
        }

        public void SetGlobalVariable(string varName, string value)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            GlobalVariables.SetGlobalVariable(Globals, varName, value);
        }

        //private string GetGlobalVariable(string varName, string defaultValue)
        //{
        //	return GlobalVariables.GetGlobalVariable(Globals, varName, defaultValue);
        //}

        public void SetGlobalVariables()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            IncrementSettings.Save();

            foreach (var child in SubItems)
            {
                child.SetGlobalVariables();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static void FillSolutionTree(Package connect, SolutionItem solutionItem, Projects projects)
        {
            if (projects == null)
            {
                return;
            }

            foreach (var item in
                projects.Cast<Project>()
                        .Select(p =>
                        {
                            ThreadHelper.ThrowIfNotOnUIThread();
                            return ConstructSolutionItem(connect, p);
                        })
                        .Where(item => item != null))
            {
                solutionItem.SubItems.Add(item);
            }
        }

        private static void FillSolutionTree(Package connect,
                                             SolutionItem solutionItem,
                                             ProjectItems projectItems)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            if (projectItems == null)
            {
                return;
            }

            foreach (var item in
                projectItems.Cast<ProjectItem>()
                            .Select(p => { Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread(); return ConstructSolutionItem(connect, p.SubProject); })
                            .Where(item => item != null))
            {
                solutionItem.SubItems.Add(item);
            }
        }

        private static ProjectItem FindProjectItem(IEnumerable projectItems, string name)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            if (projectItems == null)
            {
                return null;
            }

            foreach (ProjectItem item in projectItems)
            {
                if (string.Compare(item.Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return item;
                }

                if (item.ProjectItems == null || item.ProjectItems.Count <= 0)
                {
                    continue;
                }
                var subItem = FindProjectItem(item.ProjectItems, name);

                if (subItem != null)
                {
                    return subItem;
                }
            }

            return null;
        }

        private static bool IsValidSolutionItem(Project p)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                if (p?.Object != null && !string.IsNullOrEmpty(p.Kind)
                    && (p.Kind == "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"
                        || p.Kind == "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"
                        || p.Kind == "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}"
                        || p.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}"))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"Exception occured while checking project type \"{p?.UniqueName}\".\n{ex}",
                             LogLevel.Error);
            }

            return false;
        }

        private void GetGlobalVariables()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            IncrementSettings.Load();
        }
    }
}