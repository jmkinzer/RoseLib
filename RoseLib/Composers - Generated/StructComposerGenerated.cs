// ------------------------------------------------------------------------------
//     This file was generated on 08/03/2023 16:40:34.
//  
//     Changes to this file may cause incorrect behavior
//     and will be lost if the code is regenerated.
// ------------------------------------------------------------------------------
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using RoseLib.Guards;
using RoseLib.Traversal.Navigators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoseLib.Traversal;
using System.Text.RegularExpressions;

namespace RoseLib.Composers
{
    public partial class StructComposer
    {
        public StructComposer AddSimpleMethod(string name)
        {
            CompositionGuard.NodeOrParentIs(Visitor.CurrentNode, typeof(StructDeclarationSyntax));
            var fragment = $"public void {name}() {{​ return;​ }}".Replace('\r', ' ').Replace('\n', ' ').Replace("\u200B", "");
            var member = SyntaxFactory.ParseMemberDeclaration(fragment);
            if (member!.ContainsDiagnostics)
            {
                throw new Exception("Idiom filled with provided parameters not rendered as syntactically valid.");
            }

            var referenceNode = TryGetReferenceAndPopToPivot();
            var newEnclosingNode = AddMemberToCurrentNode(member!, referenceNode);
            Visitor.ReplaceNodeAndAdjustState(Visitor.CurrentNode!, newEnclosingNode);
            var navigator = BaseNavigator.CreateTempNavigator<CSRTypeNavigator>(Visitor);
            navigator.SelectMethodDeclaration(name);
            return this;
        }
    }
}