// ----------------------------------------------------------------------
// Project:     BuildVersionIncrement
// Module Name: BuildVersionIncrementor.cs
// ----------------------------------------------------------------------
// Created and maintained by Paul J. Melia.
// Copyright © 2020 Paul J. Melia.
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
    using EnvDTE;
    using Incrementors;
    using Logging;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Model;
    using Properties;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Task = System.Threading.Tasks.Task;

    internal class BuildVersionIncrementor
    {
        private static readonly Dictionary<string, DateTime> _fileDateCache =
            new Dictionary<string, DateTime>();

        private static readonly Dictionary<string, bool> _solutionItemCache =
            new Dictionary<string, bool>();

        private readonly BuildVersionIncrementPackage _package;

        private readonly Dictionary<string, SolutionItem> _updatedItems =
            new Dictionary<string, SolutionItem>();

        private DateTime _buildStartDate = DateTime.MinValue;
        private vsBuildAction _currentBuildAction = vsBuildAction.vsBuildActionClean;
        private vsBuildScope _currentBuildScope = vsBuildScope.vsBuildScopeBatch;

        private vsBuildState _currentBuildState = vsBuildState.vsBuildStateInProgress;

        public BuildVersionIncrementor(BuildVersionIncrementPackage package)
        {
            _package = package;
            Instance = this;
        }

        public static BuildVersionIncrementor Instance { get; private set; }

        public IncrementorCollection Incrementors { get; } = new IncrementorCollection();

        private AsyncPackage ServiceProvider => _package;

        public void InitializeIncrementors()
        {
            try
            {
                Incrementors.AddFrom(Assembly.GetExecutingAssembly());

                // ReSharper disable once AssignNullToNotNullAttribute
                var files = Directory.GetFiles(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "*.Incrementor.dll");

                foreach (var file in files)
                {
                    Logger.Write($"Loading incrementors from \"{file}\".", LogLevel.Debug);

                    var asm = Assembly.LoadFrom(file);

                    Incrementors.AddFrom(asm);
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"Exception occurred while initializing incrementors.\n{ex}",
                             LogLevel.Error);
            }
        }

        public async Task OnBuildBeginAsync(vsBuildScope scope, vsBuildAction action)
        {
            Logger.Write($"BuildEvents_OnBuildBegin scope: {scope} action {action}",
                         LogLevel.Debug);

            _currentBuildState = vsBuildState.vsBuildStateInProgress;
            _currentBuildAction = action;
            _currentBuildScope = scope;
            _buildStartDate = DateTime.Now;

            await ExecuteIncrementAsync();
            _updatedItems.Clear();
        }

        public async Task OnBuildDoneAsync(vsBuildScope scope, vsBuildAction action)
        {
            Logger.Write($"BuildEvents_OnBuildDone scope: {scope} action {action}", LogLevel.Debug);
            _currentBuildState = vsBuildState.vsBuildStateDone;

            await ExecuteIncrementAsync();
            _updatedItems.Clear();
            ClearSolutionItemAndFileDateCache();
        }

        private static bool ActiveConfigurationMatch(SolutionItem solutionItem)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (solutionItem.ItemType == SolutionItemType.Folder)
                {
                    return false;
                }

                var activeConfigName = solutionItem.ItemType == SolutionItemType.Solution
                                           ? solutionItem.Solution.SolutionBuild.ActiveConfiguration
                                                         .Name
                                           : solutionItem.Project.ConfigurationManager
                                                         .ActiveConfiguration.ConfigurationName;

                if (solutionItem.IncrementSettings.ConfigurationName == "Any"
                    || solutionItem.IncrementSettings.ConfigurationName == activeConfigName)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (!solutionItem.UniqueName.EndsWith("contentproj"))
                {
                    Logger.Write(
                        $"Couldn't get the active configuration name for \"{solutionItem.UniqueName}\": \"{ex.Message}\"\nSkipping ...",
                        LogLevel.Warn);
                }
            }

            return false;
        }

        private static bool CheckFilesystemItem(string localPath, string itemName)
        {
            var attributes = File.GetAttributes(localPath);
            if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                if (Directory.Exists(localPath))
                {
                    return false;
                }

                Logger.Write(
                    $" Directory '{itemName}' was not found - assuming a clean build was made",
                    LogLevel.Debug);
                return true;
            }

            if (File.Exists(localPath))
            {
                return false;
            }

            Logger.Write($" File '{itemName}' was not found - assuming a clean build was made",
                         LogLevel.Debug);
            return true;
        }

        private static bool CheckProjectItem(ProjectItem item, DateTime outputFileDate)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var itemFileDate = DateTime.MinValue;
            if (PropertyExists(item.Properties, Constants.PROPERTY_LOCAL_PATH))
            {
                var localPathProp = item.Properties.Item(Constants.PROPERTY_LOCAL_PATH);
                var localPath = localPathProp.Value.ToString();
                if (CheckFilesystemItem(localPath, item.Name))
                {
                    return true;
                }

                if (!PropertyExists(item.Properties, Constants.PROPERTY_DATE_MODIFIED))
                {
                    return itemFileDate > outputFileDate;
                }

                var dateModifiedProp = item.Properties.Item(Constants.PROPERTY_DATE_MODIFIED);
                var itemDateString = dateModifiedProp.Value.ToString();

                try
                {
                    itemFileDate = DateTime.Parse(itemDateString);
                }
                catch
                {
                    try
                    {
                        itemFileDate = DateTime.Parse(itemDateString, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        Logger.Write($"Cannot parse current item's date '{itemFileDate}'",
                                     LogLevel.Warn);
                    }
                }
            }
            else if (PropertyExists(item.Properties, Constants.PROPERTY_FULL_PATH))
            {
                var localPathProp = item.Properties.Item(Constants.PROPERTY_FULL_PATH);
                var localPath = localPathProp.Value.ToString();
                if (CheckFilesystemItem(localPath, item.Name))
                {
                    return true;
                }

                itemFileDate = File.GetLastWriteTime(localPath);
            }

            return itemFileDate > outputFileDate;
        }

        private static void ClearSolutionItemAndFileDateCache()
        {
            Logger.Write("Clearing date and solution cache", LogLevel.Debug);
            _fileDateCache.Clear();
            _solutionItemCache.Clear();
        }

        private static DateTime GetCachedFileDate(string outputFileName, string fullPath)
        {
            var path = Path.Combine(fullPath, outputFileName);
            DateTime fileDate;

            if (_fileDateCache.ContainsKey(path))
            {
                fileDate = _fileDateCache[path];
            }
            else
            {
                fileDate = File.GetLastWriteTime(path);
                _fileDateCache.Add(path, fileDate);
            }

            Logger.Write($"Last Build:{path} ({fileDate})", LogLevel.Debug);

            return fileDate;
        }

        private static void PrepareSolutionItem(SolutionItem solutionItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (solutionItem.ProjectType != LanguageType.None)
            {
                return;
            }

            var extension = Path.GetExtension(solutionItem.Filename);
            switch (extension)
            {
                case ".vbproj":
                    solutionItem.ProjectType = LanguageType.VisualBasic;
                    break;
                case ".vcproj":
                case ".vcxproj":
                    solutionItem.ProjectType = LanguageType.CppManaged;
                    break;
                case ".csproj":
                    solutionItem.ProjectType = LanguageType.CSharp;
                    break;
            }

            var assemblyInfo = solutionItem.FindProjectItem("AssemblyInfo.cpp");
            if (assemblyInfo != null)
            {
                return;
            }

            if (extension == ".vcproj" || extension == ".vcxproj")
            {
                solutionItem.ProjectType = LanguageType.CppUnmanaged;
            }
        }

        private static bool PropertyExists(IEnumerable properties, string propertyName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return properties.Cast<Property>().Any(item =>
                                                   {
                                                       ThreadHelper.ThrowIfNotOnUIThread();
                                                       return item.Name == propertyName;
                                                   });
        }

        private async Task ExecuteIncrementAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!Settings.Default.IsEnabled)
            {
                Logger.Write("BuildVersionIncrement disabled.");
                return;
            }

            try
            {
                var dte = await GetDteAsync();
                if (_currentBuildAction != vsBuildAction.vsBuildActionBuild
                    && _currentBuildAction != vsBuildAction.vsBuildActionRebuildAll)
                {
                    return;
                }

                if (_currentBuildScope == vsBuildScope.vsBuildScopeSolution)
                {
                    var solution = dte.Solution;
                    var solutionItem = new SolutionItem(_package, solution, true);
                    await UpdateRecursiveAsync(solutionItem);
                }
                else
                {
                    if (dte.ActiveSolutionProjects is Array projects)
                    {
                        foreach (var solutionItem in from Project p in projects
                                                     select SolutionItem.ConstructSolutionItem(
                                                         _package,
                                                         p,
                                                         false)
                                                     into solutionItem
                                                     where solutionItem != null
                                                     where IsSolutionItemModified(solutionItem)
                                                     select solutionItem)
                        {
                            await UpdateProjectAsync(solutionItem);
                        }
                    }
                }

                Logger.Write(
                    $"{(_currentBuildState == vsBuildState.vsBuildStateInProgress ? "Pre" : "Post")}-build process : Completed");
            }
            catch (Exception ex)
            {
                Logger.Write("Error occurred while executing build version increment.\n" + ex,
                             LogLevel.Error);
            }
        }

        private string GetAssemblyInfoFilename(SolutionItem solutionItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var filename = "AssemblyInfo";
            var ext = Path.GetExtension(solutionItem.Filename);

            solutionItem.ProjectType = LanguageType.None;

            switch (ext)
            {
                case ".vbproj":
                    filename += ".vb";
                    solutionItem.ProjectType = LanguageType.VisualBasic;
                    break;

                case ".vcproj":
                case ".vcxproj":
                    filename += ".cpp";
                    solutionItem.ProjectType = LanguageType.CppManaged;
                    break;

                case ".csproj":
                    filename += ".cs";
                    solutionItem.ProjectType = LanguageType.CSharp;
                    break;

                case ".sln":
                    if (string.IsNullOrEmpty(solutionItem.IncrementSettings.AssemblyInfoFilename))
                    {
                        Logger.Write(
                            "Can't update build version for a solution without specifying an assembly info file.",
                            LogLevel.Error);
                        return null;
                    }

                    solutionItem.ProjectType =
                        GetLanguageType(solutionItem.IncrementSettings.AssemblyInfoFilename);
                    if (solutionItem.ProjectType == LanguageType.None)
                    {
                        Logger.Write(
                            "Can't infer solution's assembly info file language. Please add extension to filename.",
                            LogLevel.Error);
                    }

                    break;

                default:
                    Logger.Write("Unknown project file type: \"" + ext + "\"", LogLevel.Error);
                    return null;
            }

            if (!string.IsNullOrEmpty(solutionItem.IncrementSettings.AssemblyInfoFilename))
            {
                var basePath = Path.GetDirectoryName(solutionItem.Filename);
                return Common.MakeAbsolutePath(basePath,
                                               solutionItem.IncrementSettings.AssemblyInfoFilename);
            }

            var assemblyInfo = solutionItem.FindProjectItem(filename);

            if (assemblyInfo == null)
            {
                if (ext == ".vcproj" || ext == ".vcxproj")
                {
                    filename = solutionItem.Name + ".rc";
                    assemblyInfo = solutionItem.FindProjectItem(filename);
                    solutionItem.ProjectType = LanguageType.CppUnmanaged;
                }

                if (assemblyInfo == null)
                {
                    Logger.Write($"Could not locate \"{filename}\" in project.", LogLevel.Warn);
                    return null;
                }
            }

            var ret = assemblyInfo.FileNames[0];

            if (string.IsNullOrEmpty(ret))
            {
                Logger.Write($"Located \"{filename}\" project item but failed to get filename.",
                             LogLevel.Error);
                return null;
            }

            Logger.Write($"Found \"{ret}\"", LogLevel.Debug);

            return ret;
        }

        private async Task<DTE> GetDteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var obj = await ServiceProvider.GetServiceAsync(typeof(DTE));
            return (DTE)obj;
        }

        private LanguageType GetLanguageType(string fileName)
        {
            switch (Path.GetExtension(fileName))
            {
                case ".cs":
                    return LanguageType.CSharp;
                case ".vb":
                    return LanguageType.VisualBasic;
                case ".cpp":
                    return LanguageType.CppManaged;
                default:
                    return LanguageType.None;
            }
        }

        private bool IsProjectModified(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                Logger.Write($"Checking project '{project.Name}'...", LogLevel.Debug);
                string outputFileName, fullPath;
                try
                {
                    var activeConfiguration = project.ConfigurationManager.ActiveConfiguration;
                    outputFileName = project.Properties.Item(Constants.PROPERTY_OUTPUT_FILE_NAME)
                                            .Value.ToString();
                    fullPath = project.Properties.Item(Constants.PROPERTY_FULL_PATH).Value
                                      .ToString();
                    fullPath = Path.Combine(fullPath,
                                            activeConfiguration.Properties
                                                               .Item(Constants.PROPERTY_OUTPUT_PATH)
                                                               .Value.ToString());
                }
                catch
                {
                    try
                    {
                        var prj = project.Properties.Item(Constants.PROPERTY_PROJECT).Object;
                        var configurations = prj.GetType().InvokeMember(
                            Constants.MEMBER_CONFIGURATIONS,
                            BindingFlags.GetProperty,
                            null,
                            prj,
                            null);
                        var cfg = configurations.GetType().InvokeMember(
                            Constants.MEMBER_ITEM,
                            BindingFlags.InvokeMethod,
                            null,
                            configurations,
                            new object[] { 1 });
                        var fullPathToOutputFile = string.Empty;
                        if (cfg != null)
                        {
                            fullPathToOutputFile = (string)cfg.GetType()
                                                              .InvokeMember(
                                                                  Constants.MEMBER_PRIMARY_OUTPUT,
                                                                  BindingFlags.GetProperty,
                                                                  null,
                                                                  cfg,
                                                                  null);
                        }

                        outputFileName = Path.GetFileName(fullPathToOutputFile);
                        fullPath = Path.GetDirectoryName(fullPathToOutputFile);
                        if (fullPath != null
                            && !fullPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                        {
                            fullPath += Path.DirectorySeparatorChar;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Write(
                            $"Could not get project output file date: {ex.Message}. Assuming file is modified.",
                            LogLevel.Warn);
                        return true;
                    }
                }

                var outputFileDate = GetCachedFileDate(outputFileName, fullPath);
                foreach (ProjectItem item in project.ProjectItems)
                {
                    var kind = Guid.Parse(item.Kind);
                    if (kind == VSConstants.GUID_ItemType_PhysicalFolder
                        || kind == VSConstants.GUID_ItemType_VirtualFolder)
                    {
                        if (!item.ProjectItems.Cast<ProjectItem>()
                                 .Any(innerItem => CheckProjectItem(innerItem, outputFileDate)))
                        {
                            continue;
                        }

                        Logger.Write(
                            $"Project's ('{project.Name}') item '{item.Name}' is modified. Version will be updated.",
                            LogLevel.Debug);
                        return true;
                    }

                    if (!CheckProjectItem(item, outputFileDate))
                    {
                        continue;
                    }

                    Logger.Write(
                        $"Project's ('{project.Name}') item '{item.Name}' is modified. Version will be updated.",
                        LogLevel.Debug);
                    return true;
                }

                Logger.Write($"Project '{project.Name}' is not modified", LogLevel.Debug);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Write(
                    $"Could not check if project were modified because: {ex.Message}. Assuming file is modified.",
                    LogLevel.Warn);
                Logger.Write(ex.ToString(), LogLevel.Debug);
                return true;
            }
        }

        private bool IsSolutionItemModified(SolutionItem solutionItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var key = $"{solutionItem.ItemType}:{solutionItem.Name}";
            if (_solutionItemCache.ContainsKey(key))
            {
                var result = _solutionItemCache[key];
                return result;
            }

            if (!solutionItem.IncrementSettings.DetectChanges)
            {
                Logger.Write(
                    $"Detect changes disabled. Mark item '{solutionItem.Name}' as modified.",
                    LogLevel.Debug);
                _solutionItemCache.Add(key, true);
                return true;
            }

            PrepareSolutionItem(solutionItem);
            switch (solutionItem.ItemType)
            {
                case SolutionItemType.Project:
                    {
                        var result = IsProjectModified(solutionItem.Project);
                        _solutionItemCache.Add($"{solutionItem.ItemType}:{solutionItem.Name}", result);
                        return result;
                    }
                case SolutionItemType.Folder:
                case SolutionItemType.Solution:
                    {
                        var result = false;
                        foreach (var subItem in solutionItem.SubItems)
                        {
                            // ReSharper disable once SwitchStatementMissingSomeCases
                            switch (subItem.ItemType)
                            {
                                case SolutionItemType.Project:
                                    result = IsProjectModified(subItem.Project);
                                    _solutionItemCache.Add($"{subItem.ItemType}:{subItem.Name}",
                                                           result);
                                    break;
                                case SolutionItemType.Folder:
                                    result = IsSolutionItemModified(subItem);
                                    break;
                            }

                            if (result)
                            {
                                break;
                            }
                        }

                        Logger.Write($"Solution/Folder '{solutionItem.Name}' is not modified",
                                     LogLevel.Debug);
                        _solutionItemCache.Add(key, result);
                        return result;
                    }
                case SolutionItemType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Logger.Write(
                $"Solution item '{solutionItem.ItemType}' is not supported. Run standard behavior (is modified).",
                LogLevel.Warn);
            _solutionItemCache.Add(key, true);
            return true;
        }

        private async Task UpdateAsync(SolutionItem solutionItem, string attribute)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (solutionItem.IncrementSettings.BuildAction == BuildActionType.Both
                || (solutionItem.IncrementSettings.BuildAction == BuildActionType.Build
                    && _currentBuildAction == vsBuildAction.vsBuildActionBuild)
                || (solutionItem.IncrementSettings.BuildAction == BuildActionType.ReBuild
                    && _currentBuildAction == vsBuildAction.vsBuildActionRebuildAll))
            {
                if ((solutionItem.IncrementSettings.IncrementBeforeBuild)
                    != (_currentBuildState == vsBuildState.vsBuildStateInProgress))
                {
                    return;
                }

                Logger.Write($"Updating attribute {attribute} of project {solutionItem.Name}",
                             LogLevel.Debug);

                var filename = GetAssemblyInfoFilename(solutionItem);

                if (filename == null || !File.Exists(filename))
                {
                    return;
                }

                switch (solutionItem.ProjectType)
                {
                    case LanguageType.CSharp:
                    case LanguageType.VisualBasic:
                    case LanguageType.CppManaged:

                        await UpdateVersionAsync(solutionItem,
                                                 $@"^[\[<]assembly:\s*{attribute}(Attribute)?\s*\(\s*""(?<FullVersion>\S+\.\S+(\.(?<Version>[^""]+))?)""\s*\)[\]>]",
                                                 filename,
                                                 attribute);
                        break;
                    case LanguageType.CppUnmanaged:
                        if (attribute == Constants.ATTRIBUTE_ASSEMBLY_VERSION)
                        {
                            attribute = Constants.ATTRIBUTE_PRODUCT_VERSION;
                        }

                        if (attribute == Constants.ATTRIBUTE_ASSEMBLY_FILE_VERSION)
                        {
                            attribute = Constants.ATTRIBUTE_FILE_VERSION;
                        }

                        await UpdateVersionAsync(solutionItem,
                                                 $@"^[\s]*VALUE\ ""{attribute}"",\ ""(?<FullVersion>\S+[.,\s]+\S+[.,\s]+\S+[.,\s]+[^\s""]+)""",
                                                 filename,
                                                 attribute);
                        await UpdateVersionAsync(solutionItem,
                                                 $@"^[\s]*{attribute.ToUpper()}\ (?<FullVersion>\S+[.,]+\S+[.,]+\S+[.,]+\S+)",
                                                 filename,
                                                 attribute.ToUpper());
                        break;
                    case LanguageType.None:

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private async Task UpdateProjectAsync(SolutionItem solutionItem)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (GlobalIncrementSettings.ApplySettings
                == GlobalIncrementSettings.ApplyGlobalSettings.Always
                || solutionItem.IncrementSettings.UseGlobalSettings)
            {
                solutionItem.ApplyGlobalSettings();
            }

            if (_updatedItems.ContainsKey(solutionItem.UniqueName))
            {
                return;
            }

            if (ActiveConfigurationMatch(solutionItem))
            {
                if (solutionItem.IncrementSettings.AutoUpdateAssemblyVersion)
                {
                    await UpdateAsync(solutionItem, Constants.ATTRIBUTE_ASSEMBLY_VERSION);
                }

                if (solutionItem.IncrementSettings.AutoUpdateFileVersion)
                {
                    await UpdateAsync(solutionItem, Constants.ATTRIBUTE_ASSEMBLY_FILE_VERSION);
                }
            }

            try
            {
                if (solutionItem.BuildDependency != null)
                {
                    var references = (object[])solutionItem.BuildDependency.RequiredProjects;

                    foreach (var dep in references.Select(o =>
                                                          {
                                                              ThreadHelper.ThrowIfNotOnUIThread();
                                                              return SolutionItem
                                                                  .ConstructSolutionItem(
                                                                      _package,
                                                                      (Project)o,
                                                                      false);
                                                          }).Where(dep => dep != null))
                    {
                        try
                        {
                            await UpdateProjectAsync(dep);
                        }
                        catch (Exception ex)
                        {
                            Logger.Write(
                                $"Exception occurred while updating project dependency \"{dep.UniqueName}\" for \"{solutionItem.UniqueName}\".\n{ex.Message}",
                                LogLevel.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(
                    $"Failed updating dependencies for \"{solutionItem.UniqueName}\".\n{ex.Message}",
                    LogLevel.Error);
            }

            _updatedItems.Add(solutionItem.UniqueName, solutionItem);
        }

        private async Task UpdateRecursiveAsync(SolutionItem solutionItem)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                if (!IsSolutionItemModified(solutionItem))
                {
                    return;
                }

                if (solutionItem.IncrementSettings.UseGlobalSettings)
                {
                    solutionItem.ApplyGlobalSettings();
                }

                if (ActiveConfigurationMatch(solutionItem))
                {
                    if (solutionItem.IncrementSettings.AutoUpdateAssemblyVersion)
                    {
                        await UpdateAsync(solutionItem, Constants.ATTRIBUTE_ASSEMBLY_VERSION);
                    }

                    if (solutionItem.IncrementSettings.AutoUpdateFileVersion)
                    {
                        await UpdateAsync(solutionItem, Constants.ATTRIBUTE_ASSEMBLY_FILE_VERSION);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(ex.ToString(), LogLevel.Error);
            }

            foreach (var child in solutionItem.SubItems)
            {
                await UpdateRecursiveAsync(child);
            }
        }

        private async Task UpdateVersionAsync(SolutionItem solutionItem,
                                              string regexPattern,
                                              string assemblyFile,
                                              string debugAttribute)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var fileContent = File.ReadAllText(assemblyFile);

            try
            {
                const RegexOptions options = RegexOptions.Multiline
                                             | RegexOptions.IgnorePatternWhitespace
                                             | RegexOptions.IgnoreCase;

                var m = Regex.Match(fileContent, regexPattern, options);
                if (!m.Success)
                {
                    Logger.Write(
                        $"Failed to locate attribute \"{debugAttribute}\" in file \"{assemblyFile}\".",
                        LogLevel.Error);
                    return;
                }

                var sep = Regex.Match(m.Groups["FullVersion"].Value,
                                      "(?<Separator>[\\s,.]+)",
                                      options);
                if (!sep.Success)
                {
                    Logger.Write(
                        $"Failed to fetch version separator on attribute \"{debugAttribute}\" in file \"{assemblyFile}\".",
                        LogLevel.Error);
                    return;
                }

                StringVersion currentVersion;

                string msg;
                try
                {
                    currentVersion = new StringVersion(
                        Regex.Replace(m.Groups["FullVersion"].Value,
                                      $"[^\\d{sep.Groups["Separator"].Value}]+",
                                      "0").Replace(sep.Groups["Separator"].Value, "."));
                }
                catch (Exception ex)
                {
                    msg =
                        $"Error occurred while parsing value of {debugAttribute} ({m.Groups["FullVersion"].Value}).\n{ex}";

                    throw (new Exception(msg, ex));
                }

                var newVersion = solutionItem.IncrementSettings.VersioningStyle.Increment(
                    currentVersion,
                    solutionItem.IncrementSettings.IsUniversalTime
                        ? _buildStartDate.ToUniversalTime()
                        : _buildStartDate,
                    solutionItem.IncrementSettings.StartDate,
                    solutionItem);

                if (newVersion == currentVersion)
                {
                    return;
                }

                bool success;
                if (_package.IsCommandLine)
                {
                    fileContent =
                        fileContent.Remove(m.Groups["FullVersion"].Index,
                                           m.Groups["FullVersion"].Length);
                    fileContent =
                        fileContent.Insert(m.Groups["FullVersion"].Index, newVersion.ToString());

                    try
                    {
                        File.WriteAllText(assemblyFile, fileContent);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Write(ex.Message, LogLevel.Warn);
                        success = false;
                    }
                }
                else
                {
                    var doCloseWindow =
                        !solutionItem.DTE.ItemOperations.IsFileOpen(assemblyFile, null);

                    string replaceWith;

                    if (!solutionItem.IncrementSettings.ReplaceNonNumerics
                        && Regex.IsMatch(m.Groups["FullVersion"].Value,
                                         $"[^\\d{sep.Groups["Separator"].Value}]+"))
                    {
                        var mergedVersion = m.Groups["FullVersion"].Value
                                             .Replace(sep.Groups["Separator"].Value, ".")
                                             .Split('.');

                        if (Regex.IsMatch(mergedVersion[0], "[\\d]+"))
                        {
                            mergedVersion[0] = newVersion.Major;
                        }

                        if (Regex.IsMatch(mergedVersion[1], "[\\d]+"))
                        {
                            mergedVersion[1] = newVersion.Minor;
                        }

                        if (Regex.IsMatch(mergedVersion[2], "[\\d]+"))
                        {
                            mergedVersion[2] = newVersion.Build;
                        }

                        if (Regex.IsMatch(mergedVersion[3], "[\\d]+"))
                        {
                            mergedVersion[3] = newVersion.Revision;
                        }

                        // ReSharper disable once CoVariantArrayConversion
                        replaceWith = m.Value.Replace(m.Groups["FullVersion"].Value,
                                                      string.Format("{0}.{1}.{2}.{3}",
                                                                mergedVersion)
                                                            .Replace(".",
                                                                sep.Groups["Separator"].Value));
                    }
                    else
                    {
                        replaceWith = m.Value.Replace(m.Groups["FullVersion"].Value,
                                                      newVersion.ToString(4)
                                                                .Replace(".",
                                                                    sep.Groups["Separator"].Value));
                    }

                    var dte = await GetDteAsync();
                    var projectItem = dte.Solution.FindProjectItem(assemblyFile);

                    if (projectItem == null)
                    {
                        throw (new ApplicationException(
                                      $"Failed to find project item \"{assemblyFile}\"."));
                    }

                    //var doc = projectItem.Document;

                    var window = projectItem.Open(EnvDTE.Constants.vsViewKindTextView);

                    if (window == null)
                    {
                        throw (new ApplicationException("Could not open project item."));
                    }

                    var doc = window.Document;

                    if (doc == null)
                    {
                        throw (new ApplicationException("Located project item but no document."));
                    }

                    success = doc.ReplaceText(m.Value, replaceWith);

                    if (doCloseWindow)
                    {
                        window.Close(vsSaveChanges.vsSaveChangesYes);
                    }
                    else
                    {
                        doc.Save(assemblyFile);
                    }
                }

                msg = $"{solutionItem.Name} {debugAttribute}: {newVersion}";
                if (success)
                {
                    msg += " [SUCCESS]";
                }
                else
                {
                    msg += " [FAILED]";
                }

                Logger.Write(msg);
            }
            catch (Exception ex)
            {
                Logger.Write($"Error occurred while updating version.\n{ex}", LogLevel.Error);
            }
        }

#if DEBUG
        public async Task OnBuildProjConfigBeginAsync(string projectName,
                                                      string projectConfig,
                                                      string platform,
                                                      string solutionConfig)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                var dte = await GetDteAsync();
                var p = dte.Solution.Projects.Item(projectName);

                Logger.Write(DumpProperties(p.Properties), LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Write(
                    $"Error occurred while updating build version of project {projectName}\n{ex}",
                    LogLevel.Error);
            }
        }

        private static string DumpProperties(IEnumerable props)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var sb = new StringBuilder();

            foreach (Property prop in props)
            {
                try
                {
                    sb.Append($"Name: \"{prop.Name}\" Value: \"{prop.Value}\"\r\n");
                }
                catch
                {
                    sb.Append($"Name: \"{prop.Name}\" Value: \"(UNKNOWN)\"\r\n");
                }
            }

            return sb.ToString();
        }
#endif
    }
}