﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using AppHelpers;
using Microsoft.CodeAnalysis;

namespace Codist
{
	sealed class XmlDoc
	{
		readonly ISymbol _Symbol;
		readonly Compilation _Compilation;
		readonly bool _HasDoc;
		XElement _Summary, _Remarks, _Returns, _Example;
		bool _Preliminary;
		List<XElement> _Parameters, _Exceptions, _TypeParameters, _SeeAlso;
		List<XmlDoc> _InheritedXmlDocs;
		XmlDoc _ExplicitInheritDoc;

		public XmlDoc(ISymbol symbol, Compilation compilation) {
			if (symbol == null) {
				return;
			}
			_Symbol = symbol.GetAliasTarget();
			_Compilation = compilation;
			switch (_Symbol.Kind) {
				case SymbolKind.Event:
				case SymbolKind.Field:
				case SymbolKind.Method:
				case SymbolKind.NamedType:
				case SymbolKind.Property:
					_HasDoc = Parse(_Symbol);
					break;
				case SymbolKind.Parameter:
				case SymbolKind.TypeParameter:
					_HasDoc = Parse(_Symbol.ContainingSymbol);
					break;
			}
		}
		public bool HasDoc => _HasDoc;
		public ISymbol Symbol => _Symbol;
		public XElement Summary => _Summary;
		public XElement Remarks => _Remarks;
		public XElement Returns => _Returns;
		public XElement Example => _Example;
		public IEnumerable<XElement> Exceptions => _Exceptions;
		public IEnumerable<XElement> SeeAlso => _SeeAlso;
		public XmlDoc ExplicitInheritDoc => _ExplicitInheritDoc;
		public IEnumerable<XmlDoc> InheritedXmlDocs {
			get {
				if (_InheritedXmlDocs == null) {
					_InheritedXmlDocs = new List<XmlDoc>();
					InheritDocumentation(_Symbol, _Symbol);
				}
				return _InheritedXmlDocs;
			}
		}
		public bool IsPreliminary => _Preliminary;

		public XElement GetDescription(ISymbol symbol) {
			switch (symbol.Kind) {
				case SymbolKind.Parameter: return GetParameter(symbol.Name);
				case SymbolKind.TypeParameter: return GetTypeParameter(symbol.Name);
				default: return Summary;
			}
		}
		public XElement GetParameter(string name) {
			return GetNamedItem(_Parameters, name) ?? _ExplicitInheritDoc?.GetParameter(name);
		}
		public XElement GetTypeParameter(string name) {
			return GetNamedItem(_TypeParameters, name) ?? _ExplicitInheritDoc?.GetTypeParameter(name);
		}

		static XElement GetNamedItem(List<XElement> elements, string name) {
			if (elements == null) {
				return null;
			}
			foreach (var item in elements) {
				if (item.Attribute("name")?.Value == name) {
					return item;
				}
			}
			return null;
		}

		bool Parse(ISymbol symbol) {
			if (symbol == null) {
				return false;
			}
			string c = symbol.GetDocumentationCommentXml(null, true);
			if (String.IsNullOrEmpty(c)) {
				return false;
			}
			XElement d;
			try {
				d = XElement.Parse(c, LoadOptions.None);
			}
			catch (XmlException) {
				// ignore
				return false;
			}
			if (d.FirstNode == null || d.HasElements == false) {
				return false;
			}
			bool r = false;
			foreach (var item in d.Elements()) {
				if (ParseDocSection(item)) {
					r = true;
				}
			}
			// use the member element if it begins or ends with a text node
			// support: text only XML Doc
			if (r == false && (d.FirstNode.NodeType == XmlNodeType.Text || d.LastNode.NodeType == XmlNodeType.Text)) {
				_Summary = d;
				r = true;
			}
			return r;
		}

		bool ParseDocSection(XElement item) {
			switch (item.Name.ToString()) {
				case "summary":
					_Summary = item; break;
				case "remarks":
					_Remarks = item; break;
				case "returns":
					_Returns = item; break;
				case "param":
					(_Parameters ?? (_Parameters = new List<XElement>())).Add(item); break;
				case "typeparam":
					(_TypeParameters ?? (_TypeParameters = new List<XElement>())).Add(item); break;
				case "exception":
					(_Exceptions ?? (_Exceptions = new List<XElement>())).Add(item); break;
				case "example":
					_Example = item; break;
				case "seealso":
					(_SeeAlso ?? (_SeeAlso = new List<XElement>())).Add(item); break;
				case "preliminary":
					_Preliminary = true; break;
				case "inheritdoc":
					if (Config.Instance.QuickInfoOptions.MatchFlags(QuickInfoOptions.DocumentationFromInheritDoc)) {
						var cref = item.Attribute("cref");
						if (cref != null && String.IsNullOrEmpty(cref.Value) == false) {
							var s = DocumentationCommentId.GetFirstSymbolForDeclarationId(cref.Value, _Compilation);
							if (s != null) {
								_ExplicitInheritDoc = new XmlDoc(s, _Compilation);
							}
						}
					}
					break;
				default:
					return false;
			}
			return true;
		}

		void InheritDocumentation(ISymbol symbol, ISymbol querySymbol) {
			var t = symbol.Kind == SymbolKind.NamedType ? symbol as INamedTypeSymbol : symbol.ContainingType;
			if (t == null) {
				return;
			}
			if (t.TypeKind == TypeKind.Class && t.BaseType != null) {
				InheritDocumentation(t.BaseType, querySymbol);
			}
			// inherit from base type
			if (symbol != querySymbol) {
				var kind = querySymbol.Kind;
				var returnType = querySymbol.GetReturnType();
				var parameters = querySymbol.GetParameters();
				var member = t.GetMembers(querySymbol.Name)
					.FirstOrDefault(i => i.MatchSignature(kind, returnType, parameters));
				if (member != null && AddInheritedDocFromSymbol(member)) {
					return;
				}
			}
			// inherit from implemented interfaces
			if (symbol.Kind != SymbolKind.NamedType
				&& (t = symbol.ContainingType) != null) {
				switch (symbol.Kind) {
					case SymbolKind.Method:
						foreach (var item in (symbol as IMethodSymbol).ExplicitInterfaceImplementations) {
							if (AddInheritedDocFromSymbol(item)) {
								return;
							}
						}
						break;
					case SymbolKind.Property:
						foreach (var item in (symbol as IPropertySymbol).ExplicitInterfaceImplementations) {
							if (AddInheritedDocFromSymbol(item)) {
								return;
							}
						}
						break;
					case SymbolKind.Event:
						foreach (var item in (symbol as IEventSymbol).ExplicitInterfaceImplementations) {
							if (AddInheritedDocFromSymbol(item)) {
								return;
							}
						}
						break;
				}
				foreach (var item in t.Interfaces) {
					InheritDocumentation(item, querySymbol);
				}
			}
			return;
		}

		bool AddInheritedDocFromSymbol(ISymbol symbol) {
			var doc = new XmlDoc(symbol, _Compilation);
			if (doc.HasDoc) {
				_InheritedXmlDocs.Add(doc);
				return true;
			}
			return false;
		}
	}
}
