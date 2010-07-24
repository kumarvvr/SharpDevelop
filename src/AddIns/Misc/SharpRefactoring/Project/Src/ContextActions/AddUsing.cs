﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Martin Konicek" email="martin.konicek@gmail.com"/>
//     <version>$Revision: $</version>
// </file>
using System;
using System.Collections.Generic;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.AvalonEdit;
using ICSharpCode.SharpDevelop.Refactoring;

namespace SharpRefactoring.ContextActions
{
	/// <summary>
	/// Description of AddUsing.
	/// </summary>
	public class AddUsingProvider : IContextActionsProvider
	{
		public IEnumerable<IContextAction> GetAvailableActions(EditorASTProvider editorAST)
		{
			var currentLineAST = editorAST.CurrentLineAST;
			if (currentLineAST == null)
				yield break;
			var symbol = editorAST.SymbolUnderCaret;
			foreach (var contextAction in GetAddUsingContextActions(symbol, editorAST.Editor)) {
				yield return contextAction;
			}
		}
		
		IEnumerable<IContextAction> GetAddUsingContextActions(ResolveResult symbol, ITextEditor editor)
		{
			IEnumerable<RefactoringService.AddUsingAction> addUsingActions = null;
			if (symbol is UnknownIdentifierResolveResult) {
				addUsingActions = RefactoringService.GetAddUsingActions((UnknownIdentifierResolveResult)symbol, editor);
			} else if (symbol is UnknownConstructorCallResolveResult) {
				addUsingActions = RefactoringService.GetAddUsingActions((UnknownConstructorCallResolveResult)symbol, editor);
			}
			if (addUsingActions == null)
				yield break;
			foreach (var addUsingAction in addUsingActions) {
				var addUsingActionCopy = addUsingAction;
				yield return new DelegateAction {
					Title = "using " + addUsingActionCopy.NewNamespace,
					ExecuteAction = addUsingActionCopy.Execute
				};
			}
		}
	}
}
