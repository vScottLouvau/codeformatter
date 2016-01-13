// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Microsoft.DotNet.CodeFormatting.Rules
{
    /// <summary>
    /// This will ensure that using directives are sorted alphabetically with System usings first
    /// </summary>
    [SyntaxRule(UsingOrderRule.Name, UsingOrderRule.Description, SyntaxRuleOrder.UsingOrderFormattingRule, DefaultRule = false)]
    internal sealed class UsingOrderRule : CSharpOnlyFormattingRule, ISyntaxFormattingRule
    {
        internal const string Name = "UsingOrder";
        internal const string Description = "Sort usings alphabetically, System usings first, newline between distinct root namespaces";

        public SyntaxNode Process(SyntaxNode syntaxNode, string languageName)
        {
            var root = syntaxNode as CompilationUnitSyntax;
            if (root == null)
                return syntaxNode;

            // Extract usings outside any namespace
            var rootUsings = root.Usings;

            // Sort them alphabetically but with System usings first
            var sortedUsings = root.Usings.OrderBy((u) => u.Name.ToFullString(), new UsingSorter());

            // Update whitespace to add a line between each top level namespace
            var whitespaceCorrectedUsings = SyntaxFactory.List<UsingDirectiveSyntax>();

            // TODO: Want to keep leading trivia unless it contains the copyright comment. Seems overly complex.
            string lastRootNamespace = String.Empty;
            foreach(UsingDirectiveSyntax u in sortedUsings)
            {
                string rootNamespace = u.Name.GetFirstToken().ToString();

                if(rootNamespace != lastRootNamespace && !String.IsNullOrEmpty(lastRootNamespace))
                {
                    //whitespaceCorrectedUsings = whitespaceCorrectedUsings.Add(WithLeadingNewlineCount(u, 1));
                    whitespaceCorrectedUsings = whitespaceCorrectedUsings.Add(u.WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed));
                }
                else
                {
                    //whitespaceCorrectedUsings = whitespaceCorrectedUsings.Add(WithLeadingNewlineCount(u, 0));
                    whitespaceCorrectedUsings = whitespaceCorrectedUsings.Add(u.WithoutLeadingTrivia());
                }

                lastRootNamespace = rootNamespace;
            }

            var newRoot = root.WithUsings(whitespaceCorrectedUsings);

            return newRoot;
        }

        private UsingDirectiveSyntax WithLeadingNewlineCount(UsingDirectiveSyntax node, int newlineCount)
        {
            // Count current leading newlines
            int currentNewlineCount = 0;
            foreach(SyntaxTrivia trivia in node.GetLeadingTrivia())
            {
                if (trivia.Kind() == SyntaxKind.EndOfLineTrivia) currentNewlineCount++;
                break;
            }

            if(currentNewlineCount == newlineCount)
            {
                // If count is already correct, leave alone
                return node;
            }
            else if(currentNewlineCount > newlineCount)
            {
                // If too many, exclude the excess ones
                return node.WithLeadingTrivia(node.GetLeadingTrivia().Skip(currentNewlineCount - newlineCount));
            }
            else
            {
                // If too few, insert new leading ones
                SyntaxTriviaList newLeadingTrivia = node.GetLeadingTrivia();
                for(int i = currentNewlineCount; i < newlineCount; ++i)
                {
                    newLeadingTrivia = newLeadingTrivia.Insert(0, SyntaxFactory.CarriageReturnLineFeed);
                }

                return node.WithLeadingTrivia(newLeadingTrivia);
            }
        }

        private class UsingSorter : IComparer<string>
        {
            public int Compare(string leftNs, string rightNs)
            {
                // If either namespace starts with 'System', remove the prefix to make it first ('.' before all letters)
                if (leftNs.StartsWith("System")) leftNs = leftNs.Substring("System".Length);
                if (rightNs.StartsWith("System")) rightNs = rightNs.Substring("System".Length);

                return leftNs.CompareTo(rightNs);
            }
        }
    }
}
