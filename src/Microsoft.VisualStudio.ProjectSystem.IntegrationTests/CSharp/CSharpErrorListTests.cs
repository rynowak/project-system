﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.IntegrationTests
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    [Trait("Integration", "ErrorList")]
    public class CSharpErrorListTests : AbstractIntegrationTest
    {
        protected override string DefaultLanguageName => LanguageNames.CSharp;

        public CSharpErrorListTests(VisualStudioInstanceFactory instanceFactory)
            : base(nameof(CSharpErrorListTests), WellKnownProjectTemplates.CSharpNetCoreClassLibrary, instanceFactory)
        {
            VisualStudio.SolutionExplorer.OpenFile(Project, "Class1.cs");
        }

        [Fact(Skip = "https://github.com/dotnet/project-system/issues/2281")]
        public void ErrorList()
        {
            VisualStudio.Editor.SetText(@"
class C
{
    void M(P p)
    {
        System.Console.WriteLin();
    }

    static void Main(string[] args)
    {
    }
}
");
            VisualStudio.ErrorList.ShowErrorList();
            var expectedContents = new[] {
                new ErrorListItem(
                    severity: "Error",
                    description: "The type or namespace name 'P' could not be found (are you missing a using directive or an assembly reference?)",
                    project: "TestProj.csproj",
                    fileName: "Class1.cs",
                    line: 4,
                    column: 12),
                new ErrorListItem(
                    severity: "Error",
                    description: "'Console' does not contain a definition for 'WriteLin'",
                    project: "TestProj.csproj",
                    fileName: "Class1.cs",
                    line: 6,
                    column: 24)
            };

            VisualStudio.WaitForApplicationIdle();
            var actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);
            VisualStudio.ErrorList.NavigateToErrorListItem(0);
            VisualStudio.Editor.Verify.CaretPosition(25);
            VisualStudio.SolutionExplorer.BuildSolution(waitForBuildToFinish: true);
            VisualStudio.ErrorList.ShowErrorList();
            actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);
        }

        [Fact(Skip = "https://github.com/dotnet/project-system/issues/3286")]
        public void ErrorLevelWarning()
        {
            VisualStudio.Editor.SetText(@"
class C
{
    static void Main(string[] args)
    {
        int unused = 0;
    }
}
");
            VisualStudio.ErrorList.ShowErrorList();
            var expectedContents = new[] {
                new ErrorListItem(
                    severity: "Warning",
                    description: "The variable 'unused' is assigned but its value is never used",
                    project: "TestProj.csproj",
                    fileName: "Class1.cs",
                    line: 6,
                    column: 13)
            };
            var actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);
        }

        [Fact(Skip = "https://github.com/dotnet/project-system/issues/2281"), Trait("Integration", "ErrorList")]
        public virtual void ErrorsDuringMethodBodyEditing()
        {
            VisualStudio.Editor.SetText(@"
using System;

class Program2
{
    static void Main(string[] args)
    {
        Func<int, int> a = aa => 7;
    }
}
");
            VisualStudio.ErrorList.ShowErrorList();
            var expectedContents = new ErrorListItem[] { };
            var actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);

            VisualStudio.Editor.Activate();
            VisualStudio.Editor.PlaceCaret("a = aa", charsOffset: -1);
            VisualStudio.Editor.SendKeys("a");
            VisualStudio.ErrorList.ShowErrorList();
            expectedContents = new[] {
                new ErrorListItem(
                    severity: "Error",
                    description: "A local or parameter named 'aa' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter",
                    project: "TestProj.csproj",
                    fileName: "Class1.cs",
                    line: 8,
                    column: 29)
            };
            actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);

            VisualStudio.Editor.Activate();
            VisualStudio.Editor.PlaceCaret("aa = aa", charsOffset: -1);
            VisualStudio.Editor.SendKeys(VirtualKey.Delete);
            VisualStudio.ErrorList.ShowErrorList();
            expectedContents = new ErrorListItem[] { };
            actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);
        }
    }
}
