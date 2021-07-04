// ----------------------------------------------------------------------
// Project:     BuildVersionIncrement
// Module Name: Constants.cs
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

namespace BuildVersionIncrement
{
    public class Constants
    {
        public const int COMMAND_ID_SETTINGS = 0x0100;
        public const int COMMAND_ID_VERSION = 0x0200;
        public const string COMMAND_SET = "1a42bbb0-f5ad-4882-bf32-623425c6d577";

        public const string PROPERTY_LOCAL_PATH = "LocalPath";
        public const string PROPERTY_DATE_MODIFIED = "DateModified";
        public const string PROPERTY_FULL_PATH = "FullPath";
        public const string PROPERTY_OUTPUT_FILE_NAME = "OutputFileName";
        public const string PROPERTY_OUTPUT_PATH = "OutputPath";
        public const string PROPERTY_PROJECT = "project";

        public const string MEMBER_CONFIGURATIONS = "Configurations";
        public const string MEMBER_ITEM = "Item";
        public const string MEMBER_PRIMARY_OUTPUT = "PrimaryOutput";

        public const string ATTRIBUTE_ASSEMBLY_VERSION = "AssemblyVersion";
        public const string ATTRIBUTE_ASSEMBLY_FILE_VERSION = "AssemblyFileVersion";
        public const string ATTRIBUTE_PRODUCT_VERSION = "ProductVersion";
        public const string ATTRIBUTE_FILE_VERSION = "FileVersion";
    }
}