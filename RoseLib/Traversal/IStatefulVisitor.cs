﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoseLib.Exceptions;
using RoseLib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RoseLib.Traversal
{
    public interface IStatefulVisitor
    {
        const string ANNOTATION = "RoseLibAnnotation";
        public FileInfo? SourceFile { get; set; }
        public Stack<SelectedObject> State { get; protected set; }
        public SyntaxNode? CurrentNode => State.Peek()?.CurrentNode;
        public List<SyntaxNode>? CurrentNodesList => State.Peek()?.CurrentNodesList;
        public CompilationUnitSyntax? TrackedRoot { get; protected set; }

        internal void NextStep(SyntaxNode? node)
        {
            if (node == null)
            {
                throw new InvalidOperationException($"{GetType()}: Selection failed!");
            }

            State.Push(new SelectedObject(node));
        }

        internal void NextStep(List<SyntaxNode>? nodes)
        {
            if (nodes == null)
            {
                throw new InvalidOperationException($"{GetType()}: Selection failed!");
            }

            State.Push(new SelectedObject(nodes));
        }

        internal void SetHead(SyntaxNode node)
        {
            State.Clear();
            State.Push(new SelectedObject(node));
        }

        internal void PopUntil(Type T)
        {
            var typeInState = State
                .Where(so => so.CurrentNode != null)
                .Select(so => so.CurrentNode)
                .Where(node => node?.GetType() == T)
                .Any();

            if (!typeInState)
            {
                throw new InvalidOperationException($"Can't pop to node of type {T}, not in state");
            }
            
            while(State.Peek().CurrentNode?.GetType() != T)
            {
                State.Pop();
            }
        }


        internal CompilationUnitSyntax? GetRoot()
        {
            return State.ToList()
                .Select(so => so.CurrentNode)
                .Where(node => node is CompilationUnitSyntax)
                .Cast<CompilationUnitSyntax>()
                .FirstOrDefault();
        }

        internal List<SyntaxNode> GetAllSelectedSyntaxNodes()
        {
            List<SyntaxNode> selectedNodes = new List<SyntaxNode>();

            var allSelectedObjects = State.ToList();
            foreach(var @object in allSelectedObjects)
            {
                if(@object.CurrentNode != null)
                {
                    selectedNodes.Add(@object.CurrentNode);
                }
                else if(@object.CurrentNodesList != null)
                {
                    selectedNodes.AddRange(@object.CurrentNodesList);
                }
            }

            return selectedNodes;
        }

        internal List<SelectedObject> GetAllSelectedObjects()
        {
            var allOldSelectedObjects = State.ToList();
            allOldSelectedObjects.Reverse();
            return allOldSelectedObjects;
        }

        // TODO: Prosto, ova metoda bi uvek da ide uz AfterUpdateStateAdjustment
        // Dve operacije koje moraju jedna uz drugu inače se sve raspada.
        // Tako da bi možda trebalo napraviti funkciju koja prima operaciju što radi update.
        // A usput se lepo ta operacija zaokruži parom :) I onda ne bi zaboravio nikad...
        internal void PrepareForTreeUpdate()
        {
            var root = GetRoot();

            if (root == null)
            {
                throw new InvalidStateException("State not valid, compilation unit not present");
            }

            var nodesToTrack = GetAllSelectedSyntaxNodes();
            TrackedRoot = root.TrackNodes(nodesToTrack);

        }
       
        internal void ReplaceAndAdjustState(SyntaxNode oldNode, SyntaxNode newNode)
        {
            if (State.Peek().CurrentNode != oldNode)
            {
                throw new InvalidStateException("Updates only possible to the currently selected node");

            }

            if (TrackedRoot == null)
            {
                throw new InvalidStateException("Adjustment can only be done after you've called 'PrepareForTreeUpdate'");
            }

            if (oldNode.GetType() != newNode.GetType())
            {
                throw new Exception("Old and new node must be of the same type");
            }


            string? customId;
            SyntaxAnnotation? annotation;

            if (!newNode.HasAnnotations(ANNOTATION))
            {
                customId = Guid.NewGuid().ToString();
                annotation = new SyntaxAnnotation(ANNOTATION, customId)!;
                newNode = newNode.WithAdditionalAnnotations(annotation);
            }
            else
            {
                annotation = newNode.GetAnnotations(ANNOTATION).First()!;
            }


            var trackedOldNode = TrackedRoot.GetCurrentNode(oldNode)!;

            var newRoot = TrackedRoot.ReplaceNode(trackedOldNode, newNode);

            var newState = new Stack<SelectedObject>();

            List<SelectedObject> allOldSelectedObjects = GetAllSelectedObjects();

            foreach (var oldSelectedObject in allOldSelectedObjects)
            {
                if (oldSelectedObject.CurrentNode != null)
                {
                    if (oldSelectedObject.CurrentNode == oldNode)
                    {
                        var insertedNewNode = newRoot.GetAnnotatedNodes(annotation).First();
                        newState.Push(new SelectedObject(insertedNewNode));
                    }
                    else
                    {
                        var freshNode = newRoot.GetCurrentNode(oldSelectedObject.CurrentNode);
                        if (freshNode != null)
                        {
                            newState.Push(new SelectedObject(freshNode));
                        }
                    }

                }
                else if (oldSelectedObject.CurrentNodesList != null)
                {
                    var freshNodes = newRoot.GetCurrentNodes(oldSelectedObject.CurrentNodesList);
                    newState.Push(new SelectedObject(freshNodes.ToList()));
                }
            }

            State = newState;
        }
    }
}
