﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoseLib.Exceptions;
using RoseLib.Model;
using RoseLib.Traversal;
using RoseLib.Traversal.Navigators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RoseLib.Composers
{
    public abstract class BaseComposer
    {
        public IStatefulVisitor Visitor { get; protected set; }
        public int? StatePivotIndex { get; protected set; } = -1;

        protected BaseComposer(IStatefulVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException("Cannot create a composer without a navigator.");
            }

            Visitor = visitor;

            PrepareStateAndSetStatePivotIndex();
        }
        
        protected abstract void PrepareStateAndSetStatePivotIndex();
        
        protected void GenericPrepareStateAndSetStatePivotIndex(Type pivotType, SupportedScope scope)
        {
            var isListSelected = false;
            SyntaxNode testNode;
            if (Visitor.CurrentNode != null)
            {
                testNode = Visitor.CurrentNode;
            }
            else if (Visitor.CurrentNodesList != null && Visitor.CurrentNodesList.Count > 0)
            {
                testNode = Visitor.CurrentNodesList[0];
                isListSelected = true;
            }
            else
            {
                throw new InvalidStateException("No selected nodes in the state!");
            }

            var testNodeType = testNode.GetType();
            if (testNodeType == pivotType && !isListSelected)
            {
                StatePivotIndex = Visitor.State.Count() - 1; // Head
            }
            else if (scope == SupportedScope.IMMEDIATE_OR_PARENT && testNode.Parent != null)
            {
                var parentNodeType = testNode.Parent.GetType();
                if (parentNodeType == pivotType)
                {
                    var indexOfParent = Visitor.GetIndexOfSelectedNode(testNode.Parent);
                    if (indexOfParent == -1)
                    {
                        Visitor.InsertBeforeHead(testNode.Parent);
                        StatePivotIndex = Visitor.State.Count() - 2; // Behind the head
                    }
                }
            }
        }
        
        public static bool CanProcessCurrentSelection(IStatefulVisitor statefulVisitor)
        {
            return false;
        }

        protected static bool GenericCanProcessCurrentSelectionCheck(IStatefulVisitor statefulVisitor, Type supportedSyntaxNodeType, SupportedScope scope)
        {
            SyntaxNode testNode;
            if (statefulVisitor.CurrentNode != null)
            {
                testNode = statefulVisitor.CurrentNode;
            }
            else if (statefulVisitor.CurrentNodesList != null && statefulVisitor.CurrentNodesList.Count > 0)
            {
                testNode = statefulVisitor.CurrentNodesList[0];
            }
            else
            {
                throw new InvalidStateException("No selected nodes in the state!");
            }

            var testNodeType = testNode.GetType();
            if (testNodeType == supportedSyntaxNodeType)
            {
                return true;
            }
            else if (scope == SupportedScope.IMMEDIATE_OR_PARENT && testNode.Parent != null)
            {
                var parentNodeType = testNode.Parent.GetType();
                if(parentNodeType == supportedSyntaxNodeType)
                {
                    return true;
                }
            }

            return false;
        }

        protected enum SupportedScope
        {
            IMMEDIATE,
            IMMEDIATE_OR_PARENT
        }

        protected void DeleteForParentNodeOfType<T>()
        {

            if (Visitor.CurrentNode != null)
            {
                DeleteSingleMember<T>();
            }
            else if (Visitor.CurrentNodesList != null)
            {
                DeleteMultipleMembers<T>();
            }
            else
            {
                throw new InvalidStateException("Nothing in the state, nothing to delete");
            }
        }

        private void DeleteSingleMember<T>()
        {
            SyntaxNode forDeletion = Visitor.CurrentNode!;
            Visitor.State.Pop();

            var stateStepBefore = Visitor.CurrentNode;
            var parent = forDeletion.Parent;
            if (parent == null)
            {
                throw new InvalidActionForStateException("Syntax node to be deleted does not have a parent.");
            }
            if (parent.GetType() != typeof(T))
            {
                throw new InvalidActionForStateException($"Cannot delete a member when parent is not of a {typeof(T)} type.");
            }
            if (stateStepBefore == null)
            {
                throw new InvalidStateException("For some reason, only selected member was in the state");
            }

            // Moving to the parent node, if not already there
            if (stateStepBefore != parent)
            {
                Visitor.NextStep(parent);
            }


            var newParentVersion = parent!.RemoveNode(forDeletion, SyntaxRemoveOptions.KeepNoTrivia);
            Visitor.ReplaceNodeAndAdjustState(parent!, newParentVersion!);
        }
        private void DeleteMultipleMembers<T>()
        {
            List<SyntaxNode> members = Visitor.CurrentNodesList!;
            if (members == null || members.Count == 0)
            {
                throw new InvalidStateException("List of members in the state is empty");
            }

            Visitor.State.Pop();

            var stateStepBefore = Visitor.CurrentNode;


            var parent = members[0].Parent as ClassDeclarationSyntax;

            if (parent == null)
            {
                throw new InvalidActionForStateException("Syntax node to be deleted does not have a parent.");
            }
            if (parent.GetType() != typeof(T))
            {
                throw new InvalidActionForStateException($"Cannot delete a member when parent is not of a {typeof(T)} type.");
            }
            foreach (var member in members)
            {
                if (member.Parent != parent)
                {
                    throw new InvalidStateException("For some reason, not all members have the same parent");
                }
            }
            if (stateStepBefore == null)
            {
                throw new InvalidStateException("For some reason, only selected field was in the state");
            }


            // Moving to the parent node, if not already there
            if (stateStepBefore != parent)
            {
                Visitor.NextStep(parent);
            }


            var newParentVersion = parent!.RemoveNodes(members, SyntaxRemoveOptions.KeepNoTrivia);
            Visitor.ReplaceNodeAndAdjustState(parent!, newParentVersion!);
        }

        /// <summary>
        /// Generates a textual representation of a syntax tree.
        /// Does not alter the state of the composer.
        /// </summary>
        /// <returns>syntax tree as a string</returns>
        public string GetCode()
        {
            var compilationUnit = Visitor.State
               .Where(so => so.CurrentNode is CompilationUnitSyntax)
               .Select(so => so.CurrentNode)
               .FirstOrDefault();

            if (compilationUnit == null)
            {
                throw new InvalidActionForStateException("Cannot generate textual representation if there is no compilation unit");
            }

            return compilationUnit
                .NormalizeWhitespace()
                .ToFullString();
        }
    }
}
