﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using System.Reflection.Emit;

namespace Codist
{
	/// <summary>
	/// Denotes where an assembly is imported.
	/// </summary>
	public enum AssemblySource
	{
		/// <summary>
		/// The assembly is an external one.
		/// </summary>
		Metadata,
		/// <summary>
		/// The assembly comes from source code.
		/// </summary>
		SourceCode,
		/// <summary>
		/// The assembly comes from other projects.
		/// </summary>
		Retarget
	}

	static partial class CodeAnalysisHelper
	{
		#region Symbol finder
		/// <summary>
		/// Finds all members defined or referenced in <paramref name="project"/> which may have a parameter that is of or derived from <paramref name="type"/>.
		/// </summary>
		public static async Task<List<ISymbol>> FindInstanceAsParameterAsync(this ITypeSymbol type, Project project, CancellationToken cancellationToken = default) {
			var compilation = await project.GetCompilationAsync(cancellationToken);
			var members = new List<ISymbol>(10);
			ImmutableArray<IParameterSymbol> parameters;
			var assembly = compilation.Assembly;
			foreach (var typeSymbol in compilation.GlobalNamespace.GetAllTypes(cancellationToken)) {
				foreach (var member in typeSymbol.GetMembers()) {
					if (cancellationToken.IsCancellationRequested) {
						return members;
					}
					if (member.Kind != SymbolKind.Field
						&& member.CanBeReferencedByName
						&& (parameters = member.GetParameters()).IsDefaultOrEmpty == false) {
						if (parameters.Any(p => type.CanConvertTo(p.Type) && p.Type.IsCommonClass() == false)
							&& type.CanAccess(member, assembly)) {

							members.Add(member);
						}
					}
				}
			}
			return members;
		}

		/// <summary>
		/// Finds all members defined or referenced in <paramref name="project"/> which may return an instance of <paramref name="type"/>.
		/// </summary>
		public static async Task<List<ISymbol>> FindSymbolInstanceProducerAsync(this ITypeSymbol type, Project project, CancellationToken cancellationToken = default) {
			var compilation = await project.GetCompilationAsync(cancellationToken);
			var assembly = compilation.Assembly;
			//todo cache types
			var members = new List<ISymbol>(10);
			foreach (var typeSymbol in compilation.GlobalNamespace.GetAllTypes(cancellationToken)) {
				foreach (var member in typeSymbol.GetMembers()) {
					if (cancellationToken.IsCancellationRequested) {
						return members;
					}
					ITypeSymbol mt;
					if (member.Kind == SymbolKind.Field) {
						if (member.CanBeReferencedByName
							&& (mt = member.GetReturnType()) != null && (mt.CanConvertTo(type) || (mt as INamedTypeSymbol).ContainsTypeArgument(type))
							&& type.CanAccess(member, assembly)) {
							members.Add(member);
						}
					}
					else if (member.CanBeReferencedByName
						&& ((mt = member.GetReturnType()) != null && (mt.CanConvertTo(type) || (mt as INamedTypeSymbol).ContainsTypeArgument(type))
							|| member.Kind == SymbolKind.Method && member.GetParameters().Any(p => p.Type.CanConvertTo(type) && p.RefKind != RefKind.None))
						&& type.CanAccess(member, assembly)) {
						members.Add(member);
					}
				}
			}
			return members;
		}

		public static async Task<List<ISymbol>> FindExtensionMethodsAsync(this ITypeSymbol type, Project project, CancellationToken cancellationToken = default) {
			var compilation = await project.GetCompilationAsync(cancellationToken);
			var members = new List<ISymbol>(10);
			var isValueType = type.IsValueType;
			foreach (var typeSymbol in compilation.GlobalNamespace.GetAllTypes(cancellationToken)) {
				if (typeSymbol.IsStatic == false || typeSymbol.MightContainExtensionMethods == false) {
					continue;
				}
				foreach (var member in typeSymbol.GetMembers()) {
					if (cancellationToken.IsCancellationRequested) {
						return members;
					}
					if (member.IsStatic == false || member.Kind != SymbolKind.Method) {
						continue;
					}
					var m = member as IMethodSymbol;
					if (m.IsExtensionMethod == false || m.CanBeReferencedByName == false) {
						continue;
					}
					var p = m.Parameters[0];
					if (type.CanConvertTo(p.Type)) {
						members.Add(m);
						continue;
					}
					if (m.IsGenericMethod == false || p.Type.TypeKind != TypeKind.TypeParameter) {
						continue;
					}
					foreach (var item in m.TypeParameters) {
						if (item != p.Type
							|| item.HasValueTypeConstraint && isValueType == false
							|| item.HasReferenceTypeConstraint && isValueType) {
							continue;
						}
						if (item.HasConstructorConstraint) {

						}
						if (item.ConstraintTypes.Length > 0
							&& item.ConstraintTypes.Any(i => i == type || type.CanConvertTo(i)) == false) {
							continue;
						}
						members.Add(m);
					}
				}
			}
			return members;
		}

		/// <summary>
		/// Finds symbol declarations matching <paramref name="symbolName"/> within given <paramref name="project"/>.
		/// </summary>
		public static async Task<IEnumerable<ISymbol>> FindDeclarationsAsync(this Project project, string symbolName, int resultLimit, bool fullMatch, bool matchCase, SymbolFilter filter = SymbolFilter.All, CancellationToken token = default) {
			var symbols = new SortedSet<ISymbol>(CreateSymbolComparer());
			int maxNameLength = 0;
			var predicate = CreateNameFilter(symbolName, fullMatch, matchCase);

			foreach (var symbol in await Microsoft.CodeAnalysis.FindSymbols.SymbolFinder.FindSourceDeclarationsAsync(project, predicate, token)) {
				if (symbols.Count < resultLimit) {
					symbols.Add(symbol);
				}
				else {
					maxNameLength = symbols.Max.Name.Length;
					if (symbol.Name.Length < maxNameLength) {
						symbols.Remove(symbols.Max);
						symbols.Add(symbol);
					}
				}
			}
			return symbols;
		}

		static Comparer<ISymbol> CreateSymbolComparer() {
			return Comparer<ISymbol>.Create((x, y) => {
				var l = x.Name.Length - y.Name.Length;
				return l != 0 ? l : x.GetHashCode() - y.GetHashCode();
			});
		}

		public static IEnumerable<ISymbol> FindDeclarationMatchName(this Compilation compilation, string symbolName, bool fullMatch, bool matchCase, CancellationToken cancellationToken = default) {
			var filter = CreateNameFilter(symbolName, fullMatch, matchCase);
			foreach (var type in compilation.GlobalNamespace.GetAllTypes(cancellationToken)) {
				if (type.IsAccessible(true) == false) {
					continue;
				}
				if (filter(type.Name)) {
					yield return type;
				}
				if (cancellationToken.IsCancellationRequested) {
					break;
				}
				foreach (var member in type.GetMembers()) {
					if (member.Kind != SymbolKind.NamedType
						&& member.CanBeReferencedByName
						&& member.IsAccessible(false)
						&& filter(member.Name)) {
						yield return member;
					}
				}
			}
		}

		static Func<string, bool> CreateNameFilter(string symbolName, bool fullMatch, bool matchCase) {
			if (fullMatch) {
				if (matchCase) {
					return name => name == symbolName;
				}
				else {
					return name => String.Equals(name, symbolName, StringComparison.OrdinalIgnoreCase);
				}
			}
			else {
				if (matchCase) {
					return name => name.IndexOf(symbolName, StringComparison.Ordinal) != -1;
				}
				else {
					return name => name.IndexOf(symbolName, StringComparison.OrdinalIgnoreCase) != -1;
				}
			}
		}
		#endregion

		#region Assembly and namespace
		public static IEnumerable<INamedTypeSymbol> GetAllTypes(this INamespaceSymbol namespaceSymbol, CancellationToken cancellationToken = default) {
			var stack = new Stack<INamespaceOrTypeSymbol>();
			stack.Push(namespaceSymbol);
			while (stack.Count > 0) {
				cancellationToken.ThrowIfCancellationRequested();
				var namespaceOrTypeSymbol = stack.Pop();
				var namespaceSymbol2 = namespaceOrTypeSymbol as INamespaceSymbol;
				if (namespaceSymbol2 != null) {
					foreach (var ns in namespaceSymbol2.GetMembers()) {
						stack.Push(ns);
					}
				}
				else {
					var namedTypeSymbol = (INamedTypeSymbol)namespaceOrTypeSymbol;
					foreach (var item in namedTypeSymbol.GetTypeMembers()) {
						stack.Push(item);
					}
					yield return namedTypeSymbol;
				}
			}
		}

		public static string GetAssemblyModuleName(this ISymbol symbol) {
			return symbol.ContainingAssembly?.Modules?.FirstOrDefault()?.Name
					?? symbol.ContainingAssembly?.Name;
		} 
		#endregion

		#region Symbol information
		public static string GetAbstractionModifier(this ISymbol symbol) {
			if (symbol.IsAbstract) {
				return "abstract ";
			}
			else if (symbol.IsStatic) {
				return "static ";
			}
			else if (symbol.IsVirtual) {
				return "virtual ";
			}
			else if (symbol.IsOverride) {
				return symbol.IsSealed ? "sealed override " : "override ";
			}
			else if (symbol.IsSealed && (symbol.Kind == SymbolKind.NamedType && (symbol as INamedTypeSymbol).TypeKind == TypeKind.Class || symbol.Kind == SymbolKind.Method)) {
				return "sealed ";
			}
			return String.Empty;
		}

		public static string GetAccessibility(this ISymbol symbol) {
			switch (symbol.DeclaredAccessibility) {
				case Accessibility.Public: return "public ";
				case Accessibility.Private: return "private ";
				case Accessibility.ProtectedAndInternal: return "internal protected ";
				case Accessibility.Protected: return "protected ";
				case Accessibility.Internal: return "internal ";
				case Accessibility.ProtectedOrInternal: return "protected internal ";
				default: return String.Empty;
			}
		}

		public static ISymbol GetAliasTarget(this ISymbol symbol) {
			return symbol.Kind == SymbolKind.Alias ? (symbol as IAliasSymbol).Target : symbol;
		}

		public static int GetImageId(this ISymbol symbol) {
			switch (symbol.Kind) {
				case SymbolKind.Assembly: return KnownImageIds.Assembly;
				case SymbolKind.DynamicType: return KnownImageIds.Dynamic;
				case SymbolKind.Event:
					var ev = symbol as IEventSymbol;
					switch (ev.DeclaredAccessibility) {
						case Accessibility.Public: return KnownImageIds.EventPublic;
						case Accessibility.Protected:
						case Accessibility.ProtectedOrInternal:
							return KnownImageIds.EventProtected;
						case Accessibility.Private: return KnownImageIds.EventPrivate;
						case Accessibility.ProtectedAndInternal:
						case Accessibility.Internal: return KnownImageIds.EventInternal;
						default: return KnownImageIds.Event;
					}
				case SymbolKind.Field:
					var f = symbol as IFieldSymbol;
					if (f.IsConst) {
						switch (f.DeclaredAccessibility) {
							case Accessibility.Public: return KnownImageIds.ConstantPublic;
							case Accessibility.Protected:
							case Accessibility.ProtectedOrInternal:
								return KnownImageIds.ConstantProtected;
							case Accessibility.Private: return KnownImageIds.ConstantPrivate;
							case Accessibility.ProtectedAndInternal:
							case Accessibility.Internal: return KnownImageIds.ConstantInternal;
							default: return KnownImageIds.Constant;
						}
					}
					switch (f.DeclaredAccessibility) {
						case Accessibility.Public: return KnownImageIds.FieldPublic;
						case Accessibility.Protected:
						case Accessibility.ProtectedOrInternal:
							return KnownImageIds.FieldProtected;
						case Accessibility.Private: return KnownImageIds.FieldPrivate;
						case Accessibility.ProtectedAndInternal:
						case Accessibility.Internal: return KnownImageIds.FieldInternal;
						default: return KnownImageIds.Field;
					}
				case SymbolKind.Label: return KnownImageIds.Label;
				case SymbolKind.Local: return KnownImageIds.LocalVariable;
				case SymbolKind.Method:
					var m = symbol as IMethodSymbol;
					if (m.IsExtensionMethod) {
						return KnownImageIds.ExtensionMethod;
					}
					if (m.MethodKind == MethodKind.Constructor) {
						switch (m.DeclaredAccessibility) {
							case Accessibility.Public: return KnownImageIds.TypePublic;
							case Accessibility.Protected:
							case Accessibility.ProtectedOrInternal:
								return KnownImageIds.TypeProtected;
							case Accessibility.Private: return KnownImageIds.TypePrivate;
							case Accessibility.ProtectedAndInternal:
							case Accessibility.Internal: return KnownImageIds.TypeInternal;
							default: return KnownImageIds.TypePrivate;
						}
					}
					switch (m.DeclaredAccessibility) {
						case Accessibility.Public: return KnownImageIds.MethodPublic;
						case Accessibility.Protected:
						case Accessibility.ProtectedOrInternal:
							return KnownImageIds.MethodProtected;
						case Accessibility.Private: return KnownImageIds.MethodPrivate;
						case Accessibility.ProtectedAndInternal:
						case Accessibility.Internal: return KnownImageIds.MethodInternal;
						default: return KnownImageIds.Method;
					}
				case SymbolKind.NamedType:
					var t = symbol as INamedTypeSymbol;
					switch (t.TypeKind) {
						case TypeKind.Class:
							switch (t.DeclaredAccessibility) {
								case Accessibility.Public: return KnownImageIds.ClassPublic;
								case Accessibility.Protected:
								case Accessibility.ProtectedOrInternal:
									return KnownImageIds.ClassProtected;
								case Accessibility.Private: return KnownImageIds.ClassPrivate;
								case Accessibility.ProtectedAndInternal:
								case Accessibility.Internal: return KnownImageIds.ClassInternal;
								default: return KnownImageIds.Class;
							}
						case TypeKind.Delegate:
							switch (t.DeclaredAccessibility) {
								case Accessibility.Public: return KnownImageIds.DelegatePublic;
								case Accessibility.Protected:
								case Accessibility.ProtectedOrInternal:
									return KnownImageIds.DelegateProtected;
								case Accessibility.Private: return KnownImageIds.DelegatePrivate;
								case Accessibility.ProtectedAndInternal:
								case Accessibility.Internal: return KnownImageIds.DelegateInternal;
								default: return KnownImageIds.Delegate;
							}
						case TypeKind.Enum:
							switch (t.DeclaredAccessibility) {
								case Accessibility.Public: return KnownImageIds.EnumerationPublic;
								case Accessibility.Protected:
								case Accessibility.ProtectedOrInternal:
									return KnownImageIds.EnumerationProtected;
								case Accessibility.Private: return KnownImageIds.EnumerationPrivate;
								case Accessibility.ProtectedAndInternal:
								case Accessibility.Internal: return KnownImageIds.EnumerationInternal;
								default: return KnownImageIds.Enumeration;
							}
						case TypeKind.Interface:
							switch (t.DeclaredAccessibility) {
								case Accessibility.Public: return KnownImageIds.InterfacePublic;
								case Accessibility.Protected:
								case Accessibility.ProtectedOrInternal:
									return KnownImageIds.InterfaceProtected;
								case Accessibility.Private: return KnownImageIds.InterfacePrivate;
								case Accessibility.ProtectedAndInternal:
								case Accessibility.Internal: return KnownImageIds.InterfaceInternal;
								default: return KnownImageIds.Interface;
							}
						case TypeKind.Struct:
							switch (t.DeclaredAccessibility) {
								case Accessibility.Public: return KnownImageIds.StructurePublic;
								case Accessibility.Protected:
								case Accessibility.ProtectedOrInternal:
									return KnownImageIds.StructureProtected;
								case Accessibility.Private: return KnownImageIds.StructurePrivate;
								case Accessibility.ProtectedAndInternal:
								case Accessibility.Internal: return KnownImageIds.StructureInternal;
								default: return KnownImageIds.Structure;
							}
						case TypeKind.TypeParameter:
						default: return KnownImageIds.Type;
					}
				case SymbolKind.Namespace: return KnownImageIds.Namespace;
				case SymbolKind.Parameter: return KnownImageIds.Parameter;
				case SymbolKind.Property:
					switch ((symbol as IPropertySymbol).DeclaredAccessibility) {
						case Accessibility.Public: return KnownImageIds.PropertyPublic;
						case Accessibility.Protected:
						case Accessibility.ProtectedOrInternal:
							return KnownImageIds.PropertyProtected;
						case Accessibility.Private: return KnownImageIds.PropertyPrivate;
						case Accessibility.ProtectedAndInternal:
						case Accessibility.Internal: return KnownImageIds.PropertyInternal;
						default: return KnownImageIds.Property;
					}
				default: return KnownImageIds.Item;
			}
		}

		public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol symbol) {
			switch (symbol.Kind) {
				case SymbolKind.Method: return (symbol as IMethodSymbol).Parameters;
				case SymbolKind.Event: return (symbol as IEventSymbol).AddMethod.Parameters;
				case SymbolKind.Property: return (symbol as IPropertySymbol).Parameters;
			}
			return default;
		}

		public static ITypeSymbol GetReturnType(this ISymbol symbol) {
			switch (symbol.Kind) {
				case SymbolKind.Field: return (symbol as IFieldSymbol).Type;
				case SymbolKind.Local: return (symbol as ILocalSymbol).Type;
				case SymbolKind.Method:
					var m = symbol as IMethodSymbol;
					return m.MethodKind != MethodKind.Constructor ? m.ReturnType : m.ContainingType;
				case SymbolKind.Parameter: return (symbol as IParameterSymbol).Type;
				case SymbolKind.Property: return (symbol as IPropertySymbol).Type;
				case SymbolKind.Alias: return (symbol as IAliasSymbol).Target as ITypeSymbol;
			}
			return null;
		}

		public static string GetParameterString(this ISymbol symbol) {
			switch (symbol.Kind) {
				case SymbolKind.Property: return GetPropertyAccessors(symbol as IPropertySymbol);
				case SymbolKind.Method: return GetMethodParameters(symbol as IMethodSymbol);
				case SymbolKind.NamedType: return GetTypeParameters(symbol as INamedTypeSymbol);
				default: return String.Empty;
			}
			
			string GetPropertyAccessors(IPropertySymbol p) {
				using (var sbr = ReusableStringBuilder.AcquireDefault(30)) {
					var sb = sbr.Resource;
					sb.Append("{ ");
					var m = p.GetMethod;
					if (m != null) {
						if (m.DeclaredAccessibility != Accessibility.Public) {
							sb.Append(m.GetAccessibility());
						}
						sb.Append("get; ");
					}
					m = p.SetMethod;
					if (m != null) {
						if (m.DeclaredAccessibility != Accessibility.Public) {
							sb.Append(m.GetAccessibility());
						}
						sb.Append("set; ");
					}
					return sb.Append('}').ToString();
				}
			}
			string GetMethodParameters(IMethodSymbol m) {
				using (var sbr = ReusableStringBuilder.AcquireDefault(100)) {
					var sb = sbr.Resource;
					if (m.IsGenericMethod) {
						sb.Append('<');
						var s = false;
						foreach (var item in m.TypeParameters) {
							if (s) {
								sb.Append(", ");
							}
							else {
								s = true;
							}
							sb.Append(item.Name);
						}
						sb.Append('>');
					}
					sb.Append('(');
					var p = false;
					foreach (var item in m.Parameters) {
						if (p) {
							sb.Append(", ");
						}
						else {
							p = true;
						}
						GetTypeName(item.Type, sb);
					}
					sb.Append(')');
					return sb.ToString();
				}
			}
			string GetTypeParameters(INamedTypeSymbol t) {
				if (t.Arity == 0) {
					return String.Empty;
				}
				return "<" + new string(',', t.Arity - 1) + ">";
			}
			void GetTypeName(ITypeSymbol type, StringBuilder output) {
				switch (type.TypeKind) {
					case TypeKind.Array:
						GetTypeName((type as IArrayTypeSymbol).ElementType, output);
						output.Append("[]");
						return;

					case TypeKind.Dynamic:
						output.Append('?'); return;
					case TypeKind.Module:
					case TypeKind.TypeParameter:
					case TypeKind.Enum:
					case TypeKind.Error:
						output.Append(type.Name); return;
					case TypeKind.Pointer:
						GetTypeName((type as IPointerTypeSymbol).PointedAtType, output);
						output.Append('*');
						return;
				}
				output.Append(type.Name);
				var nt = type as INamedTypeSymbol;
				if (nt == null) {
					return;
				}
				if (nt.IsGenericType == false) {
					return;
				}
				var s = false;
				output.Append('<');
				foreach (var item in nt.TypeArguments) {
					if (s) {
						output.Append(", ");
					}
					else {
						s = true;
					}
					GetTypeName(item, output);
				}
				output.Append('>');
			}
		}

		public static string GetSymbolKindName(this ISymbol symbol) {
			switch (symbol.Kind) {
				case SymbolKind.Event: return "event";
				case SymbolKind.Field: return (symbol as IFieldSymbol).IsConst ? "const" : "field";
				case SymbolKind.Label: return "label";
				case SymbolKind.Local: return (symbol as ILocalSymbol).IsConst ? "local const" : "local";
				case SymbolKind.Method: return (symbol as IMethodSymbol).IsExtensionMethod ? "extension" : "method";
				case SymbolKind.NamedType:
					switch ((symbol as INamedTypeSymbol).TypeKind) {
						case TypeKind.Array: return "array";
						case TypeKind.Dynamic: return "dynamic";
						case TypeKind.Class: return "class";
						case TypeKind.Delegate: return "delegate";
						case TypeKind.Enum: return "enum";
						case TypeKind.Interface: return "interface";
						case TypeKind.Struct: return "struct";
						case TypeKind.TypeParameter: return "type parameter";
					}
					return "type";

				case SymbolKind.Namespace: return "namespace";
				case SymbolKind.Parameter: return "parameter";
				case SymbolKind.Property: return "property";
				case SymbolKind.TypeParameter: return "type parameter";
				default: return symbol.Kind.ToString();
			}
		}

		public static ImmutableArray<ITypeParameterSymbol> GetTypeParameters(this ISymbol symbol) {
			switch (symbol.Kind) {
				case SymbolKind.Method: return (symbol as IMethodSymbol).TypeParameters;
				case SymbolKind.NamedType: return (symbol as INamedTypeSymbol).TypeParameters;
				default: return ImmutableArray<ITypeParameterSymbol>.Empty;
			}
		}

		public static bool IsCommonClass(this ISymbol symbol) {
			var type = symbol as ITypeSymbol;
			if (type == null) {
				return false;
			}
			switch (type.SpecialType) {
				case SpecialType.System_Object:
				case SpecialType.System_ValueType:
				case SpecialType.System_Enum:
				case SpecialType.System_MulticastDelegate:
				case SpecialType.System_Delegate:
					return true;
			}
			return false;
		}

		public static bool IsMemberOrType(this ISymbol symbol) {
			switch (symbol.Kind) {
				case SymbolKind.Event:
				case SymbolKind.Field:
				case SymbolKind.Method:
				case SymbolKind.Property:
				case SymbolKind.NamedType:
					return true;
			}
			return false;
		}

		#endregion

		#region Source
		public static Location FirstSourceLocation(this ISymbol symbol) {
			return symbol?.Locations.FirstOrDefault(loc => loc.IsInSource);
		}

		public static AssemblySource GetSourceType(this IAssemblySymbol assembly) {
			return AssemblySourceReflector.GetSourceType(assembly);
		}

		public static ImmutableArray<Location> GetSourceLocations(this ISymbol symbol) {
			return symbol == null || symbol.Locations.Length == 0
				? ImmutableArray<Location>.Empty
				: symbol.Locations.RemoveAll(i => i.IsInSource == false);
		}


		public static void GoToSource(this ISymbol symbol) {
			symbol.FirstSourceLocation().GoToSource();
		}

		public static void GoToSource(this Location loc) {
			if (loc != null) {
				var pos = loc.GetLineSpan().StartLinePosition;
				CodistPackage.DTE.OpenFile(loc.SourceTree.FilePath, pos.Line + 1, pos.Character + 1);
			}
		}

		public static void GoToSource(this SyntaxReference loc) {
			var pos = loc.SyntaxTree.GetLineSpan(loc.Span).StartLinePosition;
			CodistPackage.DTE.OpenFile(loc.SyntaxTree.FilePath, pos.Line + 1, pos.Character + 1);
		}

		public static bool IsAccessible(this ISymbol symbol, bool checkContainingType) {
			return symbol != null
				&& (symbol.DeclaredAccessibility == Accessibility.Public
					|| symbol.DeclaredAccessibility == Accessibility.Protected
					|| symbol.DeclaredAccessibility == Accessibility.ProtectedOrInternal
					|| symbol.ContainingAssembly.GetSourceType() != AssemblySource.Metadata)
				&& (checkContainingType == false || symbol.ContainingType == null || symbol.ContainingType.IsAccessible(true));
		}
		#endregion

		#region Symbol relationship
		/// <summary>
		/// Returns whether a given type <paramref name="from"/> can access symbol <paramref name="target"/>.
		/// </summary>
		public static bool CanAccess(this ITypeSymbol from, ISymbol target, IAssemblySymbol assembly) {
			if (target == null) {
				return false;
			}
			switch (target.DeclaredAccessibility) {
				case Accessibility.Public:
					return true && (target.ContainingType == null || from.CanAccess(target.ContainingType, assembly));
				case Accessibility.Private:
					return target.ContainingType.Equals(from) || target.FirstSourceLocation() != null;
				case Accessibility.Internal:
					return target.ContainingAssembly.GivesAccessTo(assembly) &&
						(target.ContainingType == null || from.CanAccess(target.ContainingType, assembly));
				case Accessibility.ProtectedOrInternal:
					if (target.ContainingAssembly.GivesAccessTo(assembly)) {
						return true;
					}
					goto case Accessibility.Protected;
				case Accessibility.Protected:
					target = target.ContainingType;
					if (target.ContainingType != null && from.CanAccess(target.ContainingType, assembly) == false) {
						return false;
					}
					do {
						if (from.Equals(target)) {
							return true;
						}
					} while ((from = from.BaseType) != null);
					return false;
				case Accessibility.ProtectedAndInternal:
					if (target.ContainingAssembly.GivesAccessTo(assembly)) {
						target = target.ContainingType;
						if (target.ContainingType != null && from.CanAccess(target.ContainingType, null) == false) {
							return false;
						}
						do {
							if (from.Equals(target)) {
								return true;
							}
						} while ((from = from.BaseType) != null);
						return false;
					}
					return false;
			}
			return false;
		}

		public static bool CanConvertTo(this ITypeSymbol symbol, ITypeSymbol target) {
			if (symbol.Equals(target)) {
				return true;
			}
			if (target.TypeKind == TypeKind.TypeParameter) {
				var param = target as ITypeParameterSymbol;
				foreach (var item in param.ConstraintTypes) {
					if (item.CanConvertTo(symbol)) {
						return true;
					}
				}
				return false;
			}
			if (symbol.TypeKind == TypeKind.TypeParameter) {
				var param = symbol as ITypeParameterSymbol;
				foreach (var item in param.ConstraintTypes) {
					if (item.CanConvertTo(target)) {
						return true;
					}
				}
				return false;
			}
			foreach (var item in symbol.Interfaces) {
				if (item.CanConvertTo(target)) {
					return true;
				}
			}
			while ((symbol = symbol.BaseType) != null) {
				if (symbol.CanConvertTo(target)) {
					return true;
				}
			}
			return false;
		}

		public static int CompareByAccessibilityKindName(ISymbol a, ISymbol b) {
			int s;
			if ((s = b.DeclaredAccessibility - a.DeclaredAccessibility) != 0 // sort by visibility first
				|| (s = a.Kind - b.Kind) != 0) { // then by member kind
				return s;
			}
			return a.Name.CompareTo(b.Name);
		}

		public static int CompareByFieldIntegerConst(ISymbol a, ISymbol b) {
			IFieldSymbol fa = a as IFieldSymbol, fb = b as IFieldSymbol;
			return fa == null ? -1 : fb == null ? 1 : Convert.ToInt64(fa.ConstantValue).CompareTo(Convert.ToInt64(fb.ConstantValue));
		}

		public static bool ContainsTypeArgument(this INamedTypeSymbol generic, ITypeSymbol target) {
			if (generic == null || generic.IsGenericType == false || generic.IsUnboundGenericType) {
				return false;
			}
			var types = generic.TypeArguments;
			foreach (var item in types) {
				if (item.CanConvertTo(target)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>Checks whether the given symbol has the given <paramref name="kind"/>, <paramref name="returnType"/> and <paramref name="parameters"/>.</summary>
		/// <param name="symbol">The symbol to be checked.</param>
		/// <param name="kind">The <see cref="SymbolKind"/> the symbol should have.</param>
		/// <param name="returnType">The type that the symbol should return.</param>
		/// <param name="parameters">The parameters the symbol should take.</param>
		public static bool MatchSignature(this ISymbol symbol, SymbolKind kind, ITypeSymbol returnType, ImmutableArray<IParameterSymbol> parameters) {
			if (symbol.Kind != kind) {
				return false;
			}
			if (returnType == null && symbol.GetReturnType() != null
				|| returnType != null && returnType.CanConvertTo(symbol.GetReturnType()) == false) {
				return false;
			}
			var method = kind == SymbolKind.Method ? symbol as IMethodSymbol
				: kind == SymbolKind.Event ? (symbol as IEventSymbol).RaiseMethod
				: null;
			if (method != null && parameters.IsDefault == false) {
				var memberParameters = method.Parameters;
				if (memberParameters.Length != parameters.Length) {
					return false;
				}
				for (var i = parameters.Length - 1; i >= 0; i--) {
					var pi = parameters[i];
					var mi = memberParameters[i];
					if (pi.Type.Equals(mi.Type) == false
						|| pi.RefKind != mi.RefKind) {
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>Returns whether a symbol could have an override.</summary>
		public static bool MayHaveOverride(this ISymbol symbol) {
			return symbol?.ContainingType?.TypeKind == TypeKind.Class &&
				   (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride) &&
				   symbol.IsSealed == false;
		} 
		#endregion

		static class AssemblySourceReflector
		{
			static readonly Func<IAssemblySymbol, byte> __getAssemblyType = CreateAssemblySourceTypeFunc();
			public static AssemblySource GetSourceType(IAssemblySymbol assembly) {
				return (AssemblySource)__getAssemblyType(assembly);
			}

			static Func<IAssemblySymbol, byte> CreateAssemblySourceTypeFunc() {
				var m = new DynamicMethod("GetAssemblySourceType", typeof(byte), new Type[] { typeof(IAssemblySymbol) }, true);
				var il = m.GetILGenerator();
				var isSource = il.DefineLabel();
				var isRetargetSource = il.DefineLabel();
				var a = System.Reflection.Assembly.GetAssembly(typeof(Microsoft.CodeAnalysis.CSharp.CSharpExtensions));
				var ts = a.GetType("Microsoft.CodeAnalysis.CSharp.Symbols.SourceAssemblySymbol");
				var tr = a.GetType("Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting.RetargetingAssemblySymbol");
				if (ts != null) {
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Isinst, ts);
					il.Emit(OpCodes.Brtrue_S, isSource);
				}
				if (tr != null) {
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Isinst, tr);
					il.Emit(OpCodes.Brtrue_S, isRetargetSource);
				}
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Ret);
				if (ts != null) {
					il.MarkLabel(isSource);
					il.Emit(OpCodes.Ldc_I4_1);
					il.Emit(OpCodes.Ret);
				}
				if (tr != null) {
					il.MarkLabel(isRetargetSource);
					il.Emit(OpCodes.Ldc_I4_2);
					il.Emit(OpCodes.Ret);
				}
				return m.CreateDelegate(typeof(Func<IAssemblySymbol, byte>)) as Func<IAssemblySymbol, byte>;
			}
		}
	}
}
