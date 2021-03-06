﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AppHelpers;
using Codist.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using WpfBrushes = System.Windows.Media.Brushes;

namespace Codist.SmartBars
{
	//todo Make commands async and cancellable
	/// <summary>
	/// An extended <see cref="SmartBar"/> for C# content type.
	/// </summary>
	sealed class CSharpSmartBar : SmartBar {
		static readonly Classifiers.HighlightClassifications __HighlightClassifications = new Classifiers.HighlightClassifications(ServicesHelper.Instance.ClassificationTypeRegistry);
		readonly SemanticContext _Context;
		ISymbol _Symbol;

		public CSharpSmartBar(IWpfTextView view, Microsoft.VisualStudio.Text.Operations.ITextSearchService2 textSearchService) : base(view, textSearchService) {
			_Context = view.Properties.GetOrCreateSingletonProperty(() => new SemanticContext(view));
		}

		ToolBar MyToolBar => ToolBar2;

		protected override void AddCommands(CancellationToken cancellationToken) {
			if (UpdateSemanticModel() && _Context.NodeIncludeTrivia != null) {
				AddContextualCommands(cancellationToken);
			}
			//MyToolBar.Items.Add(new Separator());
			base.AddCommands(cancellationToken);
		}

		static CommandItem CreateCommandMenu(string title, int imageId, ISymbol symbol, string emptyMenuTitle, Action<CommandContext, MenuItem, ISymbol> itemPopulator) {
			return new CommandItem(imageId, title, ctrl => (ctrl as MenuItem).StaysOpenOnClick = true, ctx => {
				var menuItem = ctx.Sender as ThemedMenuItem;
				if (menuItem.Items.Count > 0 || menuItem.SubMenuHeader != null) {
					return;
				}
				ctx.KeepToolBarOnClick = true;
				itemPopulator(ctx, menuItem, symbol);
				if (menuItem.Items.Count == 0) {
					menuItem.Items.Add(new ThemedMenuItem { Header = emptyMenuTitle, IsEnabled = false });
				}
				else {
					CreateItemsFilter(menuItem);
				}
				menuItem.IsSubmenuOpen = true;
			});
		}

		static void CreateItemsFilter(ThemedMenuItem menuItem) {
			menuItem.SubMenuHeader = new StackPanel {
				Margin = WpfHelper.TopItemMargin,
				Children = {
					new MemberFilterBox(menuItem.Items),
					new Separator()
				}
			};
		}

		void AddContextualCommands(CancellationToken cancellationToken) {
			// anti-pattern for a small margin of performance
			bool isDesignMode = CodistPackage.DebuggerStatus == DebuggerStatus.Design;
			var isReadOnly = _Context.View.IsCaretInReadOnlyRegion();
			var node = _Context.NodeIncludeTrivia;
			if (isDesignMode && isReadOnly == false && node is XmlTextSyntax) {
				AddXmlDocCommands();
				return;
			}
			var trivia = _Context.GetNodeTrivia();
			if (trivia.RawKind == 0) {
				var token = _Context.Token;
				if (token.Span.Contains(View.Selection, true)
					&& token.Kind() == SyntaxKind.IdentifierToken
					&& (node.IsDeclaration() || node is TypeSyntax || node is ParameterSyntax || node.IsKind(SyntaxKind.VariableDeclarator))) {
					// selection is within a symbol
					_Symbol = ThreadHelper.JoinableTaskFactory.Run(() => _Context.GetSymbolAsync(cancellationToken));
					if (_Symbol != null) {
						if (node is IdentifierNameSyntax) {
							AddEditorCommand(MyToolBar, KnownImageIds.GoToDefinition, "Edit.GoToDefinition", "Go to definition\nRight click: Peek definition", "Edit.PeekDefinition");
						}
						AddCommands(MyToolBar, KnownImageIds.ReferencedDimension, "Analyze symbol...", GetReferenceCommandsAsync);
						if (Classifiers.SymbolMarkManager.CanBookmark(_Symbol)) {
							AddCommands(MyToolBar, KnownImageIds.FlagGroup, "Mark symbol...", null, GetMarkerCommands);
						}

						if (isDesignMode && isReadOnly == false) {
							AddRefactorCommands(node);
						}
					}
				}
				else if (token.RawKind >= (int)SyntaxKind.NumericLiteralToken && token.RawKind <= (int)SyntaxKind.StringLiteralToken) {
					AddEditorCommand(MyToolBar, KnownImageIds.ReferencedDimension, "Edit.FindAllReferences", "Find all references");
				}
				else if (isReadOnly == false && (token.IsKind(SyntaxKind.TrueKeyword) || token.IsKind(SyntaxKind.FalseKeyword))) {
					AddCommand(MyToolBar, KnownImageIds.ToggleButton, "Toggle value", ctx => {
						Replace(ctx, v => v == "true" ? "false" : "true", true);
					});
				}
				else if (node.IsRegionalDirective()) {
					AddDirectiveCommands();
				}
				if (isDesignMode && isReadOnly == false) {
					if (node.IsKind(SyntaxKind.VariableDeclarator)) {
						if (node?.Parent?.Parent is MemberDeclarationSyntax) {
							AddCommand(MyToolBar, KnownImageIds.AddComment, "Insert comment", ctx => {
								TextEditorHelper.ExecuteEditorCommand("Edit.InsertComment");
								ctx.View.Selection.Clear();
							});
						}
					}
					else if (node.IsDeclaration()) {
						if (node is TypeDeclarationSyntax || node is MemberDeclarationSyntax || node is ParameterListSyntax) {
							AddCommand(MyToolBar, KnownImageIds.AddComment, "Insert comment", ctx => {
								TextEditorHelper.ExecuteEditorCommand("Edit.InsertComment");
								ctx.View.Selection.Clear();
							});
						}
					}
					else if (IsInvertableOperation(node.Kind())) {
						AddCommand(MyToolBar, KnownImageIds.Operator, "Invert operator", InvertOperator);
					}
					else {
						AddEditorCommand(MyToolBar, KnownImageIds.ExtractMethod, "Refactor.ExtractMethod", "Extract Method");
					}
				}
			}
			if (CodistPackage.DebuggerStatus != DebuggerStatus.Running && isReadOnly == false) {
				AddCommentCommands();
			}
			if (isDesignMode == false) {
				AddCommands(MyToolBar, KnownImageIds.BreakpointEnabled, "Debugger...\nLeft click: Toggle breakpoint\nRight click: Debugger menu...", ctx => TextEditorHelper.ExecuteEditorCommand("Debug.ToggleBreakpoint"), ctx => DebugCommands);
			}
			AddCommands(MyToolBar, KnownImageIds.SelectFrame, "Expand selection...\nRight click: Duplicate...\nCtrl click item: Copy\nShift click item: Exclude whitespaces and comments", null, GetExpandSelectionCommands);
		}

		void AddDirectiveCommands() {
			AddCommand(MyToolBar, KnownImageIds.BlockSelection, "Select directive region", ctx => {
				var a = _Context.NodeIncludeTrivia as DirectiveTriviaSyntax;
				if (a == null) {
					return;
				}
				DirectiveTriviaSyntax b;
				if (a.IsKind(SyntaxKind.EndRegionDirectiveTrivia) || a.IsKind(SyntaxKind.EndIfDirectiveTrivia)) {
					b = a;
					a = b.GetPreviousDirective();
					if (a == null) {
						return;
					}
				}
				else {
					b = a.GetNextDirective();
				}
				ctx.View.SelectSpan(new SnapshotSpan(ctx.View.TextSnapshot, Span.FromBounds(a.FullSpan.Start, b.FullSpan.End)));
			});
		}

		void AddCommentCommands() {
			var token = _Context.Token;
			var triviaList = token.HasLeadingTrivia ? token.LeadingTrivia : token.HasTrailingTrivia ? token.TrailingTrivia : default;
			var lineComment = new SyntaxTrivia();
			if (triviaList.Equals(SyntaxTriviaList.Empty) == false && triviaList.FullSpan.Contains(View.Selection.Start.Position)) {
				lineComment = triviaList.FirstOrDefault(i => i.IsLineComment());
			}
			if (lineComment.RawKind != 0) {
				AddEditorCommand(MyToolBar, KnownImageIds.UncommentCode, "Edit.UncommentSelection", "Uncomment selection");
			}
			else {
				AddCommand(MyToolBar, KnownImageIds.CommentCode, "Comment selection\nRight click: Comment line", ctx => {
					if (ctx.RightClick) {
						ctx.View.ExpandSelectionToLine();
					}
					TextEditorHelper.ExecuteEditorCommand("Edit.CommentSelection");
				});
			}
		}

		void AddRefactorCommands(SyntaxNode node) {
			if (_Symbol.ContainingAssembly.GetSourceType() != AssemblySource.Metadata) {
				AddCommand(MyToolBar, KnownImageIds.Rename, "Rename symbol", ctx => {
					ctx.KeepToolBar(false);
					TextEditorHelper.ExecuteEditorCommand("Refactor.Rename");
				});
			}
			if (node is ParameterSyntax && node.Parent is ParameterListSyntax) {
				AddEditorCommand(MyToolBar, KnownImageIds.ReorderParameters, "Refactor.ReorderParameters", "Reorder parameters");
			}
			if (node.IsKind(SyntaxKind.ClassDeclaration) || node.IsKind(SyntaxKind.StructDeclaration)) {
				AddEditorCommand(MyToolBar, KnownImageIds.ExtractInterface, "Refactor.ExtractInterface", "Extract interface");
			}
		}

		void AddXmlDocCommands() {
			AddCommand(MyToolBar, KnownImageIds.MarkupTag, "Tag XML Doc with <c>", ctx => {
				SurroundWith(ctx, "<c>", "</c>", true);
			});
			AddCommand(MyToolBar, KnownImageIds.GoToNext, "Tag XML Doc with <see> or <paramref>", ctx => {
				// updates the semantic model before executing the command,
				// for it could be modified by external editor commands or duplicated document windows
				if (UpdateSemanticModel() == false) {
					return;
				}
				ctx.View.Edit((view, edit) => {
					foreach (var item in view.Selection.SelectedSpans) {
						var t = item.GetText();
						var d = _Context.GetNode(item.Start, false, false).GetAncestorOrSelfDeclaration();
						if (d != null) {
							var mp = (d as BaseMethodDeclarationSyntax).FindParameter(t);
							if (mp != null) {
								edit.Replace(item, "<paramref name=\"" + t + "\"/>");
								continue;
							}
							var tp = d.FindTypeParameter(t);
							if (tp != null) {
								edit.Replace(item, "<typeparamref name=\"" + t + "\"/>");
								continue;
							}
						}
						edit.Replace(item, (SyntaxFacts.GetKeywordKind(t) != SyntaxKind.None ? "<see langword=\"" : "<see cref=\"") + t + "\"/>");
					}
				});
			});
			AddCommand(MyToolBar, KnownImageIds.ParagraphHardReturn, "Tag XML Doc with <para>", ctx => {
				SurroundWith(ctx, "<para>", "</para>", false);
			});
			AddCommand(MyToolBar, KnownImageIds.Bold, "Tag XML Doc with HTML <b>", ctx => {
				SurroundWith(ctx, "<b>", "</b>", true);
			});
			AddCommand(MyToolBar, KnownImageIds.Italic, "Tag XML Doc with HTML <i>", ctx => {
				SurroundWith(ctx, "<i>", "</i>", true);
			});
			AddCommand(MyToolBar, KnownImageIds.Underline, "Tag XML Doc with HTML <u>", ctx => {
				SurroundWith(ctx, "<u>", "</u>", true);
			});
			AddCommand(MyToolBar, KnownImageIds.CommentCode, "Comment selection\nRight click: Comment line", ctx => {
				if (ctx.RightClick) {
					ctx.View.ExpandSelectionToLine();
				}
				TextEditorHelper.ExecuteEditorCommand("Edit.CommentSelection");
			});
		}

		List<CommandItem> GetMarkerCommands(CommandContext arg) {
			var r = new List<CommandItem>(3);
			var symbol = _Symbol;
			if (symbol.Kind == SymbolKind.Method) {
				var ctor = symbol as IMethodSymbol;
				if (ctor != null && ctor.MethodKind == MethodKind.Constructor) {
					symbol = ctor.ContainingType;
				}
			}
			r.Add(new CommandItem(KnownImageIds.Flag, "Mark " + symbol.Name, AddHighlightMenuItems, null));
			if (Classifiers.SymbolMarkManager.Contains(symbol)) {
				r.Add(new CommandItem(KnownImageIds.FlagOutline, "Unmark " + symbol.Name, ctx => {
					UpdateSemanticModel();
					if (_Symbol != null && Classifiers.SymbolMarkManager.Remove(_Symbol)) {
						Config.Instance.FireConfigChangedEvent(Features.SyntaxHighlight);
						return;
					}
				}));
			}
			else if (Classifiers.SymbolMarkManager.HasBookmark) {
				r.Add(CreateCommandMenu("Unmark symbol...", KnownImageIds.FlagOutline, symbol, "No symbol marked", (ctx, m, s) => {
					foreach (var item in Classifiers.SymbolMarkManager.MarkedSymbols) {
						m.Items.Add(new CommandMenuItem(this, new CommandItem(item.ImageId, item.DisplayString, _ => {
							Classifiers.SymbolMarkManager.Remove(item);
							Config.Instance.FireConfigChangedEvent(Features.SyntaxHighlight);
						})));
					}
				}));
			}
			return r;
		}

		void AddHighlightMenuItems(MenuItem menuItem) {
			menuItem.Items.Add(new CommandMenuItem(this, new CommandItem(KnownImageIds.Flag, "Highlight 1", item => item.Tag = __HighlightClassifications.Highlight1, SetSymbolMark)));
			menuItem.Items.Add(new CommandMenuItem(this, new CommandItem(KnownImageIds.Flag, "Highlight 2", item => item.Tag = __HighlightClassifications.Highlight2, SetSymbolMark)));
			menuItem.Items.Add(new CommandMenuItem(this, new CommandItem(KnownImageIds.Flag, "Highlight 3", item => item.Tag = __HighlightClassifications.Highlight3, SetSymbolMark)));
			menuItem.Items.Add(new CommandMenuItem(this, new CommandItem(KnownImageIds.Flag, "Highlight 4", item => item.Tag = __HighlightClassifications.Highlight4, SetSymbolMark)));
			menuItem.Items.Add(new CommandMenuItem(this, new CommandItem(KnownImageIds.Flag, "Highlight 5", item => item.Tag = __HighlightClassifications.Highlight5, SetSymbolMark)));
			menuItem.Items.Add(new CommandMenuItem(this, new CommandItem(KnownImageIds.Flag, "Highlight 6", item => item.Tag = __HighlightClassifications.Highlight6, SetSymbolMark)));
			menuItem.Items.Add(new CommandMenuItem(this, new CommandItem(KnownImageIds.Flag, "Highlight 7", item => item.Tag = __HighlightClassifications.Highlight7, SetSymbolMark)));
			menuItem.Items.Add(new CommandMenuItem(this, new CommandItem(KnownImageIds.Flag, "Highlight 8", item => item.Tag = __HighlightClassifications.Highlight8, SetSymbolMark)));
			menuItem.Items.Add(new CommandMenuItem(this, new CommandItem(KnownImageIds.Flag, "Highlight 9", item => item.Tag = __HighlightClassifications.Highlight9, SetSymbolMark)));
		}

		void SetSymbolMark(CommandContext context) {
			if (_Symbol == null) {
				return;
			}
			if (_Symbol.Kind == SymbolKind.Method) {
				var ctor = _Symbol as IMethodSymbol;
				if (ctor != null && ctor.MethodKind == MethodKind.Constructor) {
					_Symbol = ctor.ContainingType;
				}
			}
			Classifiers.SymbolMarkManager.Update(_Symbol, context.Sender.Tag as Microsoft.VisualStudio.Text.Classification.IClassificationType);
			Config.Instance.FireConfigChangedEvent(Features.SyntaxHighlight);
		}

		void FindCallers(CommandContext context, MenuItem menuItem, ISymbol source) {
			var doc = _Context.Document;
			var docs = System.Collections.Immutable.ImmutableHashSet.CreateRange(doc.Project.GetRelatedProjectDocuments());
			SymbolCallerInfo[] callers;
			switch (source.Kind) {
				case SymbolKind.Method:
				case SymbolKind.Property:
				case SymbolKind.Event:
					callers = ThreadHelper.JoinableTaskFactory.Run(() => SymbolFinder.FindCallersAsync(source, doc.Project.Solution, docs, context.CancellationToken)).ToArray();
					break;
				case SymbolKind.NamedType:
					var tempResults = new HashSet<SymbolCallerInfo>(SymbolCallerInfoComparer.Instance);
					ThreadHelper.JoinableTaskFactory.Run(async () => {
						foreach (var item in (source as INamedTypeSymbol).InstanceConstructors) {
							foreach (var c in await SymbolFinder.FindCallersAsync(item, doc.Project.Solution, docs, context.CancellationToken)) {
								tempResults.Add(c);
							}
						}
					});
					tempResults.CopyTo(callers = new SymbolCallerInfo[tempResults.Count]);
					break;
				default: return;
			}
			Array.Sort(callers, (a, b) => {
				return CompareSymbol(a.CallingSymbol, b.CallingSymbol);
			});
			if (callers.Length < 10) {
				foreach (var caller in callers) {
					var s = caller.CallingSymbol;
					menuItem.Items.Add(new SymbolMenuItem(this, s, caller.Locations) {
						Header = new TextBlock().Append(s.ContainingType.Name + ".", WpfBrushes.Gray).Append(s.Name)
					});
				}
			}
			else {
				SymbolMenuItem subMenu = null;
				INamedTypeSymbol typeSymbol = null;
				foreach (var caller in callers) {
					var s = caller.CallingSymbol;
					if (typeSymbol == null || typeSymbol != s.ContainingType) {
						typeSymbol = s.ContainingType;
						subMenu = new SymbolMenuItem(this, typeSymbol, null);
						menuItem.Items.Add(subMenu);
					}
					subMenu.Items.Add(new SymbolMenuItem(this, s, caller.Locations));
				}
			}
		}

		void FindDerivedClasses(CommandContext context, MenuItem menuItem, ISymbol symbol) {
			var classes = ThreadHelper.JoinableTaskFactory.Run(() => SymbolFinder.FindDerivedClassesAsync(symbol as INamedTypeSymbol, _Context.Document.Project.Solution, null, context.CancellationToken)).ToArray();
			Array.Sort(classes, (a, b) => a.Name.CompareTo(b.Name));
			foreach (var derived in classes) {
				var item = new SymbolMenuItem(this, derived, derived.Locations);
				if (derived.GetSourceLocations().Length == 0) {
					(item.Header as TextBlock).Foreground = WpfBrushes.Gray;
				}
				menuItem.Items.Add(item);
			}
		}

		void FindImplementations(CommandContext context, MenuItem menuItem, ISymbol symbol) {
			var implementations = new List<ISymbol>(ThreadHelper.JoinableTaskFactory.Run(() => SymbolFinder.FindImplementationsAsync(symbol, _Context.Document.Project.Solution, null, context.CancellationToken)));
			implementations.Sort((a, b) => a.Name.CompareTo(b.Name));
			if (symbol.Kind == SymbolKind.NamedType) {
				foreach (var impl in implementations) {
					menuItem.Items.Add(new SymbolMenuItem(this, impl, impl.Locations));
				}
			}
			else {
				foreach (var impl in implementations) {
					menuItem.Items.Add(new SymbolMenuItem(this, impl.ContainingSymbol, impl.Locations));
				}
			}
		}

		void FindInstanceAsParameter(CommandContext context, MenuItem menuItem, ISymbol source) {
			ThreadHelper.JoinableTaskFactory.Run(async () => {
				var members = await (source as ITypeSymbol).FindInstanceAsParameterAsync(_Context.Document.Project, context.CancellationToken);
				SortAndGroupSymbolByClass(menuItem, members);
			});
		}

		void FindInstanceProducer(CommandContext context, MenuItem menuItem, ISymbol source) {
			ThreadHelper.JoinableTaskFactory.Run(async () => {
				var members = await (source as ITypeSymbol).FindSymbolInstanceProducerAsync(_Context.Document.Project, context.CancellationToken);
				SortAndGroupSymbolByClass(menuItem, members);
			});
		}

		void FindExtensionMethods(CommandContext context, MenuItem menuItem, ISymbol source) {
			ThreadHelper.JoinableTaskFactory.Run(async () => {
				var members = await (source as ITypeSymbol).FindExtensionMethodsAsync(_Context.Document.Project, context.CancellationToken);
				SortAndGroupSymbolByClass(menuItem, members);
			});
		}

		void FindMembers(CommandContext context, MenuItem menuItem, ISymbol symbol) {
			var type = symbol as INamedTypeSymbol;
			var ct = context.CancellationToken;
			if (type != null) {
				if (type.TypeKind == TypeKind.Class) {
					while ((type = type.BaseType) != null && type.IsCommonClass() == false) {
						if (ct.IsCancellationRequested) {
							return;
						}
						var baseTypeItem = new SymbolMenuItem(this, type, type.ToDisplayString(WpfHelper.MemberNameFormat) + " (base class)", null);
						menuItem.Items.Add(baseTypeItem);
						AddSymbolMembers(this, baseTypeItem, type, ct);
					}
				}
				else if (type.TypeKind == TypeKind.Interface) {
					foreach (var item in type.AllInterfaces) {
						if (ct.IsCancellationRequested) {
							return;
						}
						var baseInterface = new SymbolMenuItem(this, item, item.ToDisplayString(WpfHelper.MemberNameFormat) + " (base interface)", null);
						menuItem.Items.Add(baseInterface);
						AddSymbolMembers(this, baseInterface, item, ct);
					}
				}
			}
			AddSymbolMembers(this, menuItem, symbol, ct);
			void AddSymbolMembers(SmartBar bar, MenuItem menu, ISymbol source, CancellationToken token) {
				var nsOrType = source as INamespaceOrTypeSymbol;
				var members = nsOrType.GetMembers().RemoveAll(m => m.CanBeReferencedByName == false);
				if (source.Kind == SymbolKind.NamedType && (source as INamedTypeSymbol).TypeKind == TypeKind.Enum) {
					// sort enum members by value
					members = members.Sort(CodeAnalysisHelper.CompareByFieldIntegerConst);
				}
				else {
					members = members.Sort(CodeAnalysisHelper.CompareByAccessibilityKindName);
				}
				foreach (var item in members) {
					if (token.IsCancellationRequested) {
						break;
					}
					menu.Items.Add(new SymbolMenuItem(bar, item, item.Locations));
				}
			}
		}

		void FindOverrides(CommandContext context, MenuItem menuItem, ISymbol symbol) {
			foreach (var ov in ThreadHelper.JoinableTaskFactory.Run(() => SymbolFinder.FindOverridesAsync(symbol, _Context.Document.Project.Solution, null, context.CancellationToken))) {
				menuItem.Items.Add(new SymbolMenuItem(this, ov, ov.ContainingType.Name, ov.Locations));
			}
		}

		void FindReferences(CommandContext context, MenuItem menuItem, ISymbol source) {
			var refs = ThreadHelper.JoinableTaskFactory.Run(() => SymbolFinder.FindReferencesAsync(source, _Context.Document.Project.Solution, System.Collections.Immutable.ImmutableHashSet.CreateRange(_Context.Document.Project.GetRelatedProjectDocuments()), context.CancellationToken)).ToArray();
			Array.Sort(refs, (a, b) => {
				int s;
				return 0 != (s = a.Definition.ContainingType.Name.CompareTo(b.Definition.ContainingType.Name)) ? s :
					0 != (s = b.Definition.DeclaredAccessibility - a.Definition.DeclaredAccessibility) ? s
					: a.Definition.Name.CompareTo(b.Definition.Name);
			});
			if (refs.Length < 10) {
				foreach (var item in refs) {
					menuItem.Items.Add(new SymbolMenuItem(this, item.Definition, item.Definition.ContainingType?.Name + "." + item.Definition.Name, null));
				}
			}
			else {
				SymbolMenuItem subMenu = null;
				INamedTypeSymbol typeSymbol = null;
				foreach (var item in refs) {
					if (typeSymbol == null || typeSymbol != item.Definition.ContainingType) {
						typeSymbol = item.Definition.ContainingType;
						subMenu = new SymbolMenuItem(this, typeSymbol, null);
						menuItem.Items.Add(subMenu);
					}
					subMenu.Items.Add(new SymbolMenuItem(this, item.Definition, null));
				}
			}
		}

		void FindSymbolWithName(CommandContext ctx, MenuItem menuItem, ISymbol source) {
			var result = _Context.SemanticModel.Compilation.FindDeclarationMatchName(source.Name, Keyboard.Modifiers == ModifierKeys.Control, true, ctx.CancellationToken);
			SortAndGroupSymbolByClass(menuItem, new List<ISymbol>(result));
		}

		List<CommandItem> GetExpandSelectionCommands(CommandContext ctx) {
			var r = new List<CommandItem>();
			var duplicate = ctx.RightClick;
			var node = _Context.NodeIncludeTrivia;
			while (node != null) {
				if (node.FullSpan.Contains(ctx.View.Selection, false)
					&& (node.IsSyntaxBlock() || node.IsDeclaration() || node.IsKind(SyntaxKind.VariableDeclarator))
					&& node.IsKind(SyntaxKind.VariableDeclaration) == false) {
					var n = node;
					r.Add(new CommandItem(CodeAnalysisHelper.GetImageId(n), (duplicate ? "Duplicate " : "Select ") + n.GetSyntaxBrief() + " " + n.GetDeclarationSignature(), ctx2 => {
						ctx2.View.SelectNode(n, Keyboard.Modifiers == ModifierKeys.Shift ^ Config.Instance.SmartBarOptions.MatchFlags(SmartBarOptions.ExpansionIncludeTrivia) || n.Span.Contains(ctx2.View.Selection, false) == false);
						if (Keyboard.Modifiers == ModifierKeys.Control) {
							TextEditorHelper.ExecuteEditorCommand("Edit.Copy");
						}
						if (duplicate) {
							TextEditorHelper.ExecuteEditorCommand("Edit.Duplicate");
						}
					}));
				}
				node = node.Parent;
			}
			r.Add(new CommandItem(KnownImageIds.SelectAll, "Select All", ctrl => ctrl.ToolTip = "Select all text", ctx2 => TextEditorHelper.ExecuteEditorCommand("Edit.SelectAll")));
			return r;
		}

		async Task<IEnumerable<CommandItem>> GetReferenceCommandsAsync(CommandContext ctx) {
			var r = new List<CommandItem>();
			var symbol = await SymbolFinder.FindSymbolAtPositionAsync(_Context.Document, View.GetCaretPosition(), ctx.CancellationToken);
			if (symbol == null) {
				return r;
			}
			symbol = symbol.GetAliasTarget();
			switch (symbol.Kind) {
				case SymbolKind.Method:
				case SymbolKind.Property:
				case SymbolKind.Event:
					r.Add(CreateCommandMenu("Find Callers...", KnownImageIds.ShowCallerGraph, symbol, "No caller was found", FindCallers));
					if (symbol.MayHaveOverride()) {
						r.Add(CreateCommandMenu("Find Overrides...", KnownImageIds.OverloadBehavior, symbol, "No override was found", FindOverrides));
					}
					var st = symbol.ContainingType as INamedTypeSymbol;
					if (st != null && st.TypeKind == TypeKind.Interface) {
						r.Add(CreateCommandMenu("Find Implementations...", KnownImageIds.ImplementInterface, symbol, "No implementation was found", FindImplementations));
					}
					if (symbol.Kind != SymbolKind.Event) {
						CreateCommandsForReturnTypeCommand(symbol, r);
					}
					if (symbol.Kind == SymbolKind.Method && (symbol as IMethodSymbol).MethodKind == MethodKind.Constructor) {
						goto case SymbolKind.NamedType;
					}
					//r.Add(CreateCommandMenu("Find similar...", KnownImageIds.DropShadow, symbol, "No similar symbol was found", FindSimilarSymbols));
					break;
				case SymbolKind.Field:
				case SymbolKind.Local:
				case SymbolKind.Parameter:
					CreateCommandsForReturnTypeCommand(symbol, r);
					break;
				case SymbolKind.NamedType:
					var t = symbol as INamedTypeSymbol;
					if (symbol.Kind == SymbolKind.Method) { // from case SymbolKind.Method
						t = symbol.ContainingType as INamedTypeSymbol;
					}
					else {
						if (t.TypeKind == TypeKind.Class || t.TypeKind == TypeKind.Struct) {
							var ctor = _Context.NodeIncludeTrivia.GetObjectCreationNode();
							if (ctor != null) {
								var s = _Context.SemanticModel.GetSymbolOrFirstCandidate(ctor);
								if (s != null) {
									r.Add(CreateCommandMenu("Find Callers...", KnownImageIds.ShowCallerGraph, s, "No caller was found", FindCallers));
								}
							}
							else if (t.InstanceConstructors.Length > 0) {
								r.Add(CreateCommandMenu("Find Constructor Callers...", KnownImageIds.ShowCallerGraph, t, "No caller was found", FindCallers));
							}
						}
						r.Add(CreateCommandMenu("Find Members...", KnownImageIds.ListMembers, t, "No member was found", FindMembers));
						if (t.IsStatic == false) {
							r.Add(CreateCommandMenu("Find Extensions...", KnownImageIds.ListMembers, t, "No extension method was found", FindExtensionMethods));
						}
					}
					if (t.IsStatic || t.SpecialType != SpecialType.None) {
						break;
					}
					r.Add(CreateCommandMenu("Find Instance Producer...", KnownImageIds.NewItem, t, "No instance creator was found", FindInstanceProducer));
					r.Add(CreateCommandMenu("Find Instance as Parameter...", KnownImageIds.Parameter, t, "No instance as parameter was found", FindInstanceAsParameter));
					if (t.IsSealed == false) {
						if (t.TypeKind == TypeKind.Class) {
							r.Add(CreateCommandMenu("Find Derived Classes...", KnownImageIds.NewClass, t, "No derived class was found", FindDerivedClasses));
						}
						else if (t.TypeKind == TypeKind.Interface) {
							r.Add(CreateCommandMenu("Find Implementations...", KnownImageIds.ImplementInterface, symbol, "No implementation was found", FindImplementations));
						}
					}
					break;
				case SymbolKind.Namespace:
					r.Add(CreateCommandMenu("Find Members...", KnownImageIds.ListMembers, symbol, "No member was found", FindMembers));
					break;
			}
			//r.Add(CreateCommandMenu("Find references...", KnownImageIds.ReferencedDimension, symbol, "No reference found", FindReferences));
			r.Add(CreateCommandMenu("Find Symbol Named " + symbol.Name + "...", KnownImageIds.FindSymbol, symbol, "No symbol was found", FindSymbolWithName));
			r.Add(new CommandItem(KnownImageIds.ReferencedDimension, "Find All References", _ => TextEditorHelper.ExecuteEditorCommand("Edit.FindAllReferences")));
			r.Add(new CommandItem(KnownImageIds.ListMembers, "Go to Member", _ => TextEditorHelper.ExecuteEditorCommand("Edit.GoToMember")));
			r.Add(new CommandItem(KnownImageIds.Type, "Go to Type", _ => TextEditorHelper.ExecuteEditorCommand("Edit.GoToType")));
			r.Add(new CommandItem(KnownImageIds.FindSymbol, "Go to Symbol", _ => TextEditorHelper.ExecuteEditorCommand("Edit.GoToSymbol")));
			return r;
		}

		void CreateCommandsForReturnTypeCommand(ISymbol symbol, List<CommandItem> list) {
			var type = symbol.GetReturnType();
			if (type != null && type.SpecialType == SpecialType.None) {
				list.Add(CreateCommandMenu("Find Members of " + type.Name + type.GetParameterString() + "...", KnownImageIds.ListMembers, type, "No member was found", FindMembers));
				if (type.IsStatic == false) {
					list.Add(CreateCommandMenu("Find Extensions for " + type.Name + type.GetParameterString() + "...", KnownImageIds.ExtensionMethod, type, "No extension method was found", FindExtensionMethods));
				}
				if (type.ContainingAssembly.GetSourceType() != AssemblySource.Metadata) {
					list.Add(new CommandItem(KnownImageIds.GoToDeclaration, "Go to " + type.Name + type.GetParameterString(), _ => type.GoToSource()));
				}
			}
		}

		static bool IsInvertableOperation(SyntaxKind kind) {
			switch (kind) {
				case SyntaxKind.BitwiseAndExpression:
				case SyntaxKind.BitwiseOrExpression:
				case SyntaxKind.LogicalOrExpression:
				case SyntaxKind.LogicalAndExpression:
				case SyntaxKind.EqualsExpression:
				case SyntaxKind.NotEqualsExpression:
				case SyntaxKind.GreaterThanExpression:
				case SyntaxKind.GreaterThanOrEqualExpression:
				case SyntaxKind.LessThanExpression:
				case SyntaxKind.LessThanOrEqualExpression:
				case SyntaxKind.PostDecrementExpression:
				case SyntaxKind.PostIncrementExpression:
				case SyntaxKind.PreIncrementExpression:
				case SyntaxKind.PreDecrementExpression:
				case SyntaxKind.AddExpression:
				case SyntaxKind.SubtractExpression:
				case SyntaxKind.MultiplyExpression:
				case SyntaxKind.DivideExpression:
				case SyntaxKind.UnaryPlusExpression:
				case SyntaxKind.UnaryMinusExpression:
				case SyntaxKind.LeftShiftExpression:
				case SyntaxKind.RightShiftExpression:
				case SyntaxKind.AddAssignmentExpression:
				case SyntaxKind.SubtractAssignmentExpression:
				case SyntaxKind.MultiplyAssignmentExpression:
				case SyntaxKind.DivideAssignmentExpression:
				case SyntaxKind.AndAssignmentExpression:
				case SyntaxKind.OrAssignmentExpression:
				case SyntaxKind.LeftShiftAssignmentExpression:
				case SyntaxKind.RightShiftAssignmentExpression:
					return true;
			}
			return false;
		}

		void InvertOperator(CommandContext ctx) {
			Replace(ctx, input => {
				switch (input) {
					case "==": return "!=";
					case "!=": return "==";
					case "&&": return "||";
					case "||": return "&&";
					case "--": return "++";
					case "++": return "--";
					case "<": return ">=";
					case ">": return "<=";
					case "<=": return ">";
					case ">=": return "<";
					case "+": return "-";
					case "-": return "+";
					case "*": return "/";
					case "/": return "*";
					case "&": return "|";
					case "|": return "&";
					case "<<": return ">>";
					case ">>": return "<<";
					case "+=": return "-=";
					case "-=": return "+=";
					case "*=": return "/=";
					case "/=": return "*=";
					case "<<=": return ">>=";
					case ">>=": return "<<=";
					case "&=": return "|=";
					case "|=": return "&=";
				}
				return null;
			}, true);
		}

		void SortAndGroupSymbolByClass(MenuItem menuItem, List<ISymbol> members) {
			members.Sort(CompareSymbol);
			if (members.Count < 10) {
				foreach (var member in members) {
					menuItem.Items.Add(new SymbolMenuItem(this, member, member.Locations) {
						Header = (member.ContainingType != null
							? new TextBlock().Append(member.ContainingType.Name + ".", WpfBrushes.Gray)
							: new TextBlock())
						.Append(member.Name)
					});
				}
			}
			else {
				SymbolMenuItem subMenu = null;
				INamedTypeSymbol typeSymbol = null;
				foreach (var member in members) {
					if (typeSymbol == null || typeSymbol != member.ContainingType) {
						typeSymbol = member.ContainingType ?? member as INamedTypeSymbol;
						if (typeSymbol != null) {
							subMenu = new SymbolMenuItem(this, typeSymbol, typeSymbol.Locations);
							menuItem.Items.Add(subMenu);
							if (typeSymbol == member) {
								continue;
							}
						}
						else {
							continue;
						}
					}
					subMenu.Items.Add(new SymbolMenuItem(this, member, member.Locations));
				}
			}
		}

		static int CompareSymbol(ISymbol a, ISymbol b) {
			var s = b.ContainingAssembly.GetSourceType().CompareTo(a.ContainingAssembly.GetSourceType());
			if (s != 0) {
				return s;
			}
			INamedTypeSymbol ta = a.ContainingType, tb = b.ContainingType;
			var ct = ta != null && tb != null;
			return ct && (s = tb.DeclaredAccessibility.CompareTo(ta.DeclaredAccessibility)) != 0 ? s
				: (s = b.DeclaredAccessibility.CompareTo(a.DeclaredAccessibility)) != 0 ? s
				: ct && (s = ta.Name.CompareTo(tb.Name)) != 0 ? s
				: ct && (s = ta.GetHashCode().CompareTo(tb.GetHashCode())) != 0 ? s
				: (s = a.Name.CompareTo(b.Name)) != 0 ? s
				: 0;
		}

		bool UpdateSemanticModel() {
			return ThreadHelper.JoinableTaskFactory.Run(() => _Context.UpdateAsync(View.Selection.Start.Position, default));
		}
		sealed class SymbolMenuItem : CommandMenuItem, IMemberFilterable
		{
			public SymbolMenuItem(SmartBar bar, ISymbol symbol, IEnumerable<Location> locations) : this(bar, symbol, symbol.ToDisplayString(WpfHelper.MemberNameFormat), locations) {
			}
			public SymbolMenuItem(SmartBar bar, ISymbol symbol, string alias, IEnumerable<Location> locations) : base(bar, new CommandItem(symbol, alias)) {
				Locations = locations;
				Symbol = symbol;
				if (symbol.Kind == SymbolKind.Field) {
					var f = symbol as IFieldSymbol;
					if (f.HasConstantValue) {
						InputGestureText = f.ConstantValue?.ToString();
					}
					//else {
					//	InputGestureText = f.Type.Name;
					//}
				}
				//else {
				//	InputGestureText = symbol.GetReturnType()?.Name;
				//}
				SetColorPreviewIcon(symbol);
				//todo deal with symbols with multiple locations
				if (locations != null && locations.Any(l => l.SourceTree?.FilePath != null)) {
					Click += GotoLocation;
				}
				if (Symbol != null) {
					ToolTip = String.Empty;
					ToolTipOpening += ShowToolTip;
				}
			}

			public IEnumerable<Location> Locations { get; }
			public ISymbol Symbol { get; }

			bool IMemberFilterable.Filter(MemberFilterTypes filterTypes) {
				return MemberFilterBox.FilterByImageId(filterTypes, CommandItem.ImageId);
			}

			void GotoLocation(object sender, RoutedEventArgs args) {
				var loc = Locations.FirstOrDefault();
				if (loc != null) {
					var p = loc.GetLineSpan();
					CodistPackage.DTE.OpenFile(loc.SourceTree.FilePath, p.StartLinePosition.Line + 1, p.StartLinePosition.Character + 1);
					args.Handled = true;
				}
			}

			void SetColorPreviewIcon(ISymbol symbol) {
				var b = symbol.Kind == SymbolKind.Property || symbol.Kind == SymbolKind.Field ? QuickInfo.ColorQuickInfo.GetBrush(symbol) : null;
				if (b != null) {
					var h = Header as TextBlock;
					h.Inlines.InsertBefore(
						h.Inlines.FirstInline,
						new System.Windows.Documents.InlineUIContainer(new System.Windows.Shapes.Rectangle {
							Height = ThemeHelper.DefaultIconSize,
							Width = ThemeHelper.DefaultIconSize,
							Fill = b,
							Margin = WpfHelper.GlyphMargin
						}) {
							BaselineAlignment = BaselineAlignment.TextTop
						});
				}
			}

			void ShowToolTip(object sender, ToolTipEventArgs args) {
				ToolTip = ToolTipFactory.CreateToolTip(Symbol, (SmartBar as CSharpSmartBar)._Context.SemanticModel.Compilation);
				this.SetTipOptions();
				ToolTipService.SetPlacement(this, System.Windows.Controls.Primitives.PlacementMode.Left);
				ToolTipOpening -= ShowToolTip;
			}

		}

		sealed class SymbolCallerInfoComparer : IEqualityComparer<SymbolCallerInfo>
		{
			internal static readonly SymbolCallerInfoComparer Instance = new SymbolCallerInfoComparer();

			public bool Equals(SymbolCallerInfo x, SymbolCallerInfo y) {
				return x.CallingSymbol == y.CallingSymbol;
			}

			public int GetHashCode(SymbolCallerInfo obj) {
				return obj.CallingSymbol.GetHashCode();
			}
		}
	}
}
