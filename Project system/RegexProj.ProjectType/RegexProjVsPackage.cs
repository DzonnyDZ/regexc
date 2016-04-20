/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System.Runtime.InteropServices;

namespace Dzonny.RegexCompiler.RegexProj
{

    /// <summary>This class implements the package exposed by this assembly.</summary>
    /// <remarks>
    /// This package is required if you want to define adds custom commands (ctmenu)
    /// or localized resources for the strings that appear in the New Project and Open Project dialogs.
    /// Creating project extensions or project types does not actually require a VSPackage.
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Description("CIL project system")]
    [Guid(PackageGuid)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    public sealed class RegexProjProjectSystemPackage : ProjectSystemPackage
    {
        /// <summary>CTor - creates a new instance of the <see cref="RegexProjProjectSystemPackage"/> class</summary>
        public RegexProjProjectSystemPackage() : base("RegexProj") { }

        /// <summary>The GUID for this package.</summary>
        public const string PackageGuid = "b584f11e-5e77-40e8-bbfd-7234850504bb";

        /// <summary>The GUID for this project type.  It is unique with the project file extension and appears under the VS registry hive's Projects key.</summary>
        public const string ProjectTypeGuid = "e452ebf3-3bbb-4a96-b835-645487eab85a";

        /// <summary>The file extension of this project type.  No preceding period.</summary>
        public const string ProjectExtension = "regexproj";

        /// <summary>The default namespace this project compiles with, so that manifest resource names can be calculated for embedded resources.</summary>
        internal const string DefaultNamespace = "Dzonny.RegexProj";
    }
}