﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BuildVersionIncrement.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsEnabled {
            get {
                return ((bool)(this["IsEnabled"]));
            }
            set {
                this["IsEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool GlobalAutoUpdateAssemblyVersion {
            get {
                return ((bool)(this["GlobalAutoUpdateAssemblyVersion"]));
            }
            set {
                this["GlobalAutoUpdateAssemblyVersion"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2000-01-01")]
        public global::System.DateTime GlobalStartDate {
            get {
                return ((global::System.DateTime)(this["GlobalStartDate"]));
            }
            set {
                this["GlobalStartDate"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Both")]
        public string GlobalBuildAction {
            get {
                return ((string)(this["GlobalBuildAction"]));
            }
            set {
                this["GlobalBuildAction"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool GlobalAutoUpdateFileVersion {
            get {
                return ((bool)(this["GlobalAutoUpdateFileVersion"]));
            }
            set {
                this["GlobalAutoUpdateFileVersion"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("None")]
        public string GlobalMajor {
            get {
                return ((string)(this["GlobalMajor"]));
            }
            set {
                this["GlobalMajor"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("None")]
        public string GlobalMinor {
            get {
                return ((string)(this["GlobalMinor"]));
            }
            set {
                this["GlobalMinor"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("None")]
        public string GlobalBuild {
            get {
                return ((string)(this["GlobalBuild"]));
            }
            set {
                this["GlobalBuild"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("None")]
        public string GlobalRevision {
            get {
                return ((string)(this["GlobalRevision"]));
            }
            set {
                this["GlobalRevision"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool GlobalReplaceNonNumeric {
            get {
                return ((bool)(this["GlobalReplaceNonNumeric"]));
            }
            set {
                this["GlobalReplaceNonNumeric"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("OnlyWhenChosen")]
        public string GlobalApply {
            get {
                return ((string)(this["GlobalApply"]));
            }
            set {
                this["GlobalApply"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsVerboseLogEnabled {
            get {
                return ((bool)(this["IsVerboseLogEnabled"]));
            }
            set {
                this["IsVerboseLogEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool DetectChanges {
            get {
                return ((bool)(this["DetectChanges"]));
            }
            set {
                this["DetectChanges"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool GlobalIncrementBeforeBuild {
            get {
                return ((bool)(this["GlobalIncrementBeforeBuild"]));
            }
            set {
                this["GlobalIncrementBeforeBuild"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool GlobalUseUniversalClock {
            get {
                return ((bool)(this["GlobalUseUniversalClock"]));
            }
            set {
                this["GlobalUseUniversalClock"] = value;
            }
        }
    }
}
