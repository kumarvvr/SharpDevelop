﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Core;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.Search;
using ICSharpCode.SharpDevelop.Parser;
using ICSharpCode.SharpDevelop.Project;
using ICSharpCode.SharpDevelop.Refactoring;

namespace CSharpBinding.Parser
{
	public class TParser : IParser
	{
		public IReadOnlyList<string> TaskListTokens { get; set; }
		
		public bool CanParse(string fileName)
		{
			return Path.GetExtension(fileName).Equals(".CS", StringComparison.OrdinalIgnoreCase);
		}
		
		/*
		void RetrieveRegions(ICompilationUnit cu, ICSharpCode.NRefactory.Parser.SpecialTracker tracker)
		{
			for (int i = 0; i < tracker.CurrentSpecials.Count; ++i) {
				ICSharpCode.NRefactory.PreprocessingDirective directive = tracker.CurrentSpecials[i] as ICSharpCode.NRefactory.PreprocessingDirective;
				if (directive != null) {
					if (directive.Cmd == "#region") {
						int deep = 1;
						for (int j = i + 1; j < tracker.CurrentSpecials.Count; ++j) {
							ICSharpCode.NRefactory.PreprocessingDirective nextDirective = tracker.CurrentSpecials[j] as ICSharpCode.NRefactory.PreprocessingDirective;
							if (nextDirective != null) {
								switch (nextDirective.Cmd) {
									case "#region":
										++deep;
										break;
									case "#endregion":
										--deep;
										if (deep == 0) {
											cu.FoldingRegions.Add(new FoldingRegion(directive.Arg.Trim(), DomRegion.FromLocation(directive.StartPosition, nextDirective.EndPosition)));
											goto end;
										}
										break;
								}
							}
						}
						end: ;
					}
				}
			}
		}
		 */
		
		public ParseInformation Parse(FileName fileName, ITextSource fileContent, bool fullParseInformationRequested,
		                              IProject parentProject, CancellationToken cancellationToken)
		{
			var csharpProject = parentProject as CSharpProject;
			
			CSharpParser parser = new CSharpParser(csharpProject != null ? csharpProject.CompilerSettings : null);
			parser.GenerateTypeSystemMode = !fullParseInformationRequested;
			
			CompilationUnit cu = parser.Parse(fileContent, fileName);
			cu.Freeze();
			
			CSharpParsedFile file = cu.ToTypeSystem();
			ParseInformation parseInfo;
			
			if (fullParseInformationRequested)
				parseInfo = new CSharpFullParseInformation(file, fileContent.Version, cu);
			else
				parseInfo = new ParseInformation(file, fullParseInformationRequested);
			
			AddCommentTags(cu, parseInfo.TagComments, fileContent);
			
			return parseInfo;
		}
		
		void AddCommentTags(CompilationUnit cu, IList<TagComment> tagComments, ITextSource fileContent)
		{
			ReadOnlyDocument document = null;
			foreach (var comment in cu.Descendants.OfType<Comment>().Where(c => c.CommentType != CommentType.InactiveCode)) {
				int matchLength;
				int index = comment.Content.IndexOfAny(TaskListTokens, 0, out matchLength);
				if (index > -1) {
					if (document == null)
						document = new ReadOnlyDocument(fileContent);
					int commentSignLength = comment.CommentType == CommentType.Documentation || comment.CommentType == CommentType.MultiLineDocumentation ? 3 : 2;
					int commentEndSignLength = comment.CommentType == CommentType.MultiLine || comment.CommentType == CommentType.MultiLineDocumentation ? 2 : 0;
					int commentStartOffset = document.GetOffset(comment.StartLocation) + commentSignLength;
					int commentEndOffset = document.GetOffset(comment.EndLocation) - commentEndSignLength;
					do {
						int absoluteOffset = commentStartOffset + index;
						var startLocation = document.GetLocation(absoluteOffset);
						int endOffset = Math.Min(document.GetLineByNumber(startLocation.Line).EndOffset, commentEndOffset);
						string content = document.GetText(absoluteOffset, endOffset - absoluteOffset);
						if (content.Length < matchLength) {
							// HACK: workaround parser bug with multi-line documentation comments
							break;
						}
						tagComments.Add(new TagComment(content.Substring(0, matchLength), new DomRegion(cu.FileName, startLocation.Line, startLocation.Column), content.Substring(matchLength)));
						index = comment.Content.IndexOfAny(TaskListTokens, endOffset - commentStartOffset, out matchLength);
					} while (index > -1);
				}
			}
		}
		
		public ResolveResult Resolve(ParseInformation parseInfo, TextLocation location, ICompilation compilation, CancellationToken cancellationToken)
		{
			var csParseInfo = parseInfo as CSharpFullParseInformation;
			if (csParseInfo == null)
				throw new ArgumentException("Parse info does not have CompilationUnit");
			
			return ResolveAtLocation.Resolve(compilation, csParseInfo.ParsedFile, csParseInfo.CompilationUnit, location, cancellationToken);
		}
		
		public void FindLocalReferences(ParseInformation parseInfo, ITextSource fileContent, IVariable variable, ICompilation compilation, Action<Reference> callback, CancellationToken cancellationToken)
		{
			var csParseInfo = parseInfo as CSharpFullParseInformation;
			if (csParseInfo == null)
				throw new ArgumentException("Parse info does not have CompilationUnit");
			
			ReadOnlyDocument document = null;
			ISyntaxHighlighter highlighter = null;
			
			new FindReferences().FindLocalReferences(
				variable, csParseInfo.ParsedFile, csParseInfo.CompilationUnit, compilation,
				delegate (AstNode node, ResolveResult result) {
					if (document == null) {
						document = new ReadOnlyDocument(fileContent);
						highlighter = SD.EditorControlService.CreateHighlighter(document, csParseInfo.FileName);
					}
					var region = new DomRegion(parseInfo.FileName, node.StartLocation, node.EndLocation);
					int offset = document.GetOffset(node.StartLocation);
					int length = document.GetOffset(node.EndLocation) - offset;
					var builder = SearchResultsPad.CreateInlineBuilder(node.StartLocation, node.EndLocation, document, highlighter);
					callback(new Reference(region, result, offset, length, builder, highlighter.DefaultTextColor));
				}, cancellationToken);
		}
		
		static readonly Lazy<IAssemblyReference[]> defaultReferences = new Lazy<IAssemblyReference[]>(
			delegate {
				Assembly[] assemblies = {
					typeof(object).Assembly,
					typeof(Uri).Assembly,
					typeof(Enumerable).Assembly
				};
				return assemblies.Select(asm => new CecilLoader().LoadAssemblyFile(asm.Location)).ToArray();
			});
		
		public ICompilation CreateCompilationForSingleFile(FileName fileName, IParsedFile parsedFile)
		{
			return new CSharpProjectContent()
				.AddAssemblyReferences(defaultReferences.Value)
				.UpdateProjectContent(null, parsedFile)
				.CreateCompilation();
		}
		
		public ResolveResult ResolveSnippet(ParseInformation parseInfo, TextLocation location, string codeSnippet, ICompilation compilation, CancellationToken cancellationToken)
		{
			var csParseInfo = parseInfo as CSharpFullParseInformation;
			if (csParseInfo == null)
				throw new ArgumentException("Parse info does not have CompilationUnit");
			CSharpAstResolver contextResolver = new CSharpAstResolver(compilation, csParseInfo.CompilationUnit, csParseInfo.ParsedFile);
			var node = csParseInfo.CompilationUnit.GetNodeAt(location);
			CSharpResolver context;
			if (node != null)
				context = contextResolver.GetResolverStateAfter(node, cancellationToken);
			else
				context = new CSharpResolver(compilation);
			CSharpParser parser = new CSharpParser();
			var expr = parser.ParseExpression(new StringReader(codeSnippet));
			if (parser.HasErrors)
				return new ErrorResolveResult(SpecialType.UnknownType, PrintErrorsAsString(parser.Errors), TextLocation.Empty);
			CSharpAstResolver snippetResolver = new CSharpAstResolver(context, expr);
			return snippetResolver.Resolve(expr, cancellationToken);
		}
		
		string PrintErrorsAsString(IEnumerable<Error> errors)
		{
			StringBuilder builder = new StringBuilder();
			
			foreach (var error in errors)
				builder.AppendLine(error.Message);
			
			return builder.ToString();
		}
	}
}
