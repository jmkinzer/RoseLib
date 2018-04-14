﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System;
using System.Collections.Generic;
using RoseLibApp.RoseLib.Validation_Attributes;
using System.IO;

namespace RoseLibApp.RoseLib.Selectors
{
    public class ClassSelector : BaseSelector
    {
        #region Constructors

        public ClassSelector() : base()
        {
        }

        public ClassSelector(StreamReader reader) : base(reader)
        {
        }

        public ClassSelector(SyntaxNode node) : base(node)
        {
        }

        public ClassSelector(List<SyntaxNode> nodes) : base(nodes)
        {
        }

        #endregion

        #region Finding field declarations

        /// <summary>
        /// Finds a field declaration of a given name, if such exists.
        /// </summary>
        /// <param name="fieldName">Name of the variable being declared</param>
        public void FindFieldDeclaration([NotBlank] string fieldName)
        {
            var fieldDeclarations = CurrentNode?.DescendantNodes().OfType<FieldDeclarationSyntax>().ToList();
            foreach (var fieldDeclaration in fieldDeclarations)
            {
                var declaratorExists = fieldDeclaration.DescendantNodes().OfType<VariableDeclaratorSyntax>().
                    Where(d => d.Identifier.ValueText == fieldName).Any();

                if (declaratorExists)
                {
                    CurrentNode = fieldDeclaration;
                }
            }

            CurrentNode = null;
        }

        /// <summary>
        /// Finds the last field declaration, if such exists.
        /// </summary>
        public void FindLastFieldDeclaration()
        {
            CurrentNode = CurrentNode?.DescendantNodes().OfType<FieldDeclarationSyntax>().LastOrDefault();
        }

        #endregion

        #region Finding property declarations

        /// <summary>
        /// Finds a property declaration of a given name.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        public void FindPropertyDeclaration([NotBlank] string propertyName)
        {
            CurrentNode = CurrentNode?.DescendantNodes().OfType<PropertyDeclarationSyntax>().
                Where(p => p.Identifier.ValueText == propertyName).FirstOrDefault();
        }

        /// <summary>
        /// Finds the last property declaration.
        /// </summary>
        public void FindLastPropertyDeclaration()
        {
            CurrentNode = CurrentNode?.DescendantNodes().OfType<PropertyDeclarationSyntax>().LastOrDefault();
        }

        #endregion

        #region Finding method declarations

        /// <summary>
        /// Finds and returns occurances of (possibly overloaded) methods with a specified name, if such exist.
        /// </summary>
        /// <param name="methodName">Method's name</param>
        public void FindOverloadedMethodDeclarations([NotBlank] string methodName)
        {
            var allMethods = CurrentNode?.DescendantNodes().OfType<MethodDeclarationSyntax>();

            CurrentNodesList = allMethods?.Where(p => p.Identifier.ValueText == methodName).Cast<SyntaxNode>().ToList();
        }

        /// <summary>
        /// Finds and returns a method with a specified name if such exists. If there is method overloading, returns the first one.
        /// </summary>
        /// <param name="methodName">Method's name</param>
        public void FindMethodDeclaration([NotBlank] string methodName)
        {
            FindOverloadedMethodDeclarations(methodName);

            CurrentNode = CurrentNodesList?.FirstOrDefault();
        }

        /// <summary>
        /// Finds and returns a method with a specified name and parameter types, if such exists.
        /// </summary>
        /// <param name="methodName">Method's name</param>
        /// <param name="parameterTypes">Array of strings representing parameters' types.</param>
        public void FindMethodDeclaration([NotBlank] string methodName, params string[] parameterTypes)
        {
            FindOverloadedMethodDeclarations(methodName);

            if (CurrentNodesList?.Count == 0)
            {
                CurrentNode = null;
            }

            foreach (var methodDeclaration in CurrentNodesList)
            {
                bool areSame = CompareParameterTypes(methodDeclaration as MethodDeclarationSyntax, parameterTypes);

                if (areSame)
                {
                    CurrentNode = methodDeclaration;
                }
            }

            CurrentNode = null;
        }

        /// <summary>
        /// Finds the last method declaration.
        /// </summary>
        public void FindLastMethodDeclaration()
        {
            CurrentNode = CurrentNode?.DescendantNodes().OfType<FieldDeclarationSyntax>().LastOrDefault();
        }

        #endregion

        #region Finding constructor declarations

        /// <summary>
        /// Finds all constructors of a class
        /// </summary>
        public void FindOverloadedConstructorDeclarations()
        {
            CurrentNodesList = CurrentNode?.DescendantNodes().OfType<ConstructorDeclarationSyntax>().Cast<SyntaxNode>().ToList();
        }

        /// <summary>
		/// Finds a parameterless constructor of a class. 
		/// </summary>
		public void FindParameterlessConstructorDeclaration()
        {
            FindOverloadedConstructorDeclarations();

            var constructors = CurrentNodesList
                ?.Where(n => 
                {
                    var c = n as ConstructorDeclarationSyntax;
                    return c.ParameterList.DescendantNodes().Count() == 0;
                })
                .ToList();

            CurrentNode = constructors?.FirstOrDefault();
        }

        /// <summary>
        /// Finds and returns a constructor with parameters' types, if such exists.
        /// </summary>
        public void FindConstructorDeclaration(params string[] parameterTypes)
        {
            FindOverloadedConstructorDeclarations();

            if (CurrentNodesList?.Count == 0)
            {
                CurrentNode = null;
            }

            foreach (var constructorDeclaration in CurrentNodesList)
            {
                bool areSame = CompareParameterTypes(constructorDeclaration as ConstructorDeclarationSyntax, parameterTypes);

                if (areSame)
                {
                    CurrentNode = constructorDeclaration;
                    return;
                }
            }

            CurrentNode = null;
        }

        /// <summary>
        /// Find last declared constructor
        /// </summary>
        public void FindLastConstructorDeclaration()
        {
            FindOverloadedConstructorDeclarations();
            CurrentNode = CurrentNodesList?.LastOrDefault();
        }
        #endregion

        #region Finding destructor declaration

        /// <summary>
        /// A method that find a destructor.
        /// </summary>
        public void FindDestructor()
        {
            CurrentNode = CurrentNode?.DescendantNodes().OfType<DestructorDeclarationSyntax>().FirstOrDefault();
        }

        #endregion

        #region Common functionalities

        /// <summary>
        /// A method that compares types of a parameters of a given method, with expected values.
        /// </summary>
        /// <param name="node">A base method node. (It is in inheritance tree of constructors and ordinary methods)</param>
        /// <param name="parameterTypes">Expected parameters.</param>
        /// <returns></returns>
        private bool CompareParameterTypes([NotNull] BaseMethodDeclarationSyntax node, params string[] parameterTypes)
        {
            var foundParameters = node.ParameterList.Parameters;

            if (foundParameters.Count() != parameterTypes.Count())
            {
                return false;   
            }

            for (int i = 0; i < foundParameters.Count(); i++)
            {
                string foundType = foundParameters[i].Type.ToString();
                string expectedType = parameterTypes[i];
                if (foundType != expectedType)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
