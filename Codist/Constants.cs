﻿using System;
using System.ComponentModel;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;

namespace Codist
{
	/// <summary>
	/// Code style constants
	/// </summary>
	static partial class Constants
	{
		public const string NameOfMe = nameof(Codist);

		public static class CodeTypes
		{
			public const string CPlusPlus = "C/C++";
			public const string Code = nameof(Code);
			public const string CSharp = nameof(CSharp);
			public const string Text = nameof(Text);
		}

		public static class SyntaxCategory
		{
			public const string Keyword = nameof(Keyword);
			public const string Preprocessor = nameof(Preprocessor);
			public const string General = nameof(General);
			public const string Comment = nameof(Comment);
			public const string CompilerMarked = "Compiler Marked";
			public const string Declaration = nameof(Declaration);
			public const string TypeDefinition = "Type Definition";
			public const string Member = nameof(Member);
			public const string Xml = "XML";
			public const string Highlight = nameof(Highlight);
			public const string Location = nameof(Location);
		}

		public static class EditorProperties
		{
			public const string TextViewBackground = "TextView Background";
			public const string Text = "Text";
			public const string Caret = "Caret";
			public const string OverwriteCaret = "Overwrite Caret";
			public const string SelectedText = "Selected Text";
			public const string InactiveSelectedText = "Inactive Selected Text";
			public const string VisibleWhitespace = "Visible Whitespace";
		}

		public const string CodeKeyword = "Keyword";
		public const string CodeComment = "Comment";

		public const string CodeAbstractionKeyword = "Keyword: Abstraction";
		public const string CodeBranchingKeyword = "Keyword: Branching";
		public const string CodeControlFlowKeyword = "Keyword: Control flow";
		public const string CodeLoopKeyword = "Keyword: Loop";
		public const string CodeSpecialPuctuation = "Special Puctuation";

		public const string CodeClassName = "class name";
		public const string CodeStructName = "struct name";
		public const string CodeEnumName = "enum name";
		public const string CodeInterfaceName = "interface name";
		public const string CodeDelegateName = "delegate name";
		public const string CodeModuleName = "module name";
		public const string CodeTypeParameterName = "type parameter name";
		public const string CodePreprocessorText = "preprocessor text";
		public const string CodePreprocessorKeyword = "Preprocessor Keyword";
		public const string CodeExcluded = "Excluded Code";
		public const string CodeUnnecessary = "unnecessary code";
		public const string CodeIdentifier = "identifier";
		public const string CodeLiteral = "Literal";
		public const string CodeNumber = "Number";
		public const string CodeOperator = "Operator";
		public const string CodePunctuation = "punctuation";
		public const string CodeBraceMatching = "brace matching";
		public const string CodeInlineRenameField = "inline rename field";
		public const string CodeString = "String";
		public const string CodeStringVerbatim = "string - verbatim";
		public const string CodeSymbolDefinition = "symbol definition";
		public const string CodeSymbolReference = "symbol reference";
		public const string CodeUrl = "url";
		public const string CodeFormalLanguage = "formal language";

		public const string XmlDocAttributeName = "xml doc comment - attribute name";
		public const string XmlDocAttributeQuotes = "xml doc comment - attribute quotes";
		public const string XmlDocAttributeValue = "xml doc comment - attribute value";
		public const string XmlDocComment = "xml doc comment - text";
		public const string XmlDocCData = "xml doc comment - cdata section";
		public const string XmlDocDelimiter = "xml doc comment - delimiter";
		public const string XmlDocEntity = "xml doc comment - entity reference";
		public const string XmlDocTag = "xml doc comment - name";

		public const string CSharpLocalVariableName = "C#: Local field";
		public const string CSharpParameterName = "C#: Parameter";
		public const string CSharpNamespaceName = "C#: Namespace";
		public const string CSharpExtensionMethodName = "C#: Extension method";
		public const string CSharpExternMethodName = "C#: Extern method";
		public const string CSharpMethodName = "C#: Method";
		public const string CSharpEventName = "C#: Event";
		public const string CSharpPropertyName = "C#: Property";
		public const string CSharpFieldName = "C#: Field";
		public const string CSharpConstFieldName = "C#: Const field";
		public const string CSharpReadOnlyFieldName = "C#: Read-only field";
		public const string CSharpResourceKeyword = "C#: Resource keyword";
		public const string CSharpAliasNamespaceName = "C#: Alias namespace";
		public const string CSharpConstructorMethodName = "C#: Constructor method";
		public const string CSharpDeclarationName = "C#: Type declaration";
		public const string CSharpNestedDeclarationName = "C#: Nested type declaration";
		public const string CSharpTypeParameterName = "C#: Type parameter";
		public const string CSharpStaticMemberName = "C#: Static member";
		public const string CSharpOverrideMemberName = "C#: Override member";
		public const string CSharpVirtualMemberName = "C#: Virtual member";
		public const string CSharpAbstractMemberName = "C#: Abstract member";
		public const string CSharpSealedClassName = "C#: Sealed class";
		public const string CSharpAttributeName = "C#: Attribute name";
		public const string CSharpAttributeNotation = "C#: Attribute notation";
		public const string CSharpLabel = "C#: Label";
		public const string CSharpDeclarationBrace = "C#: Declaration brace";
		public const string CSharpMethodBody = "C#: Method body";
		public const string CSharpXmlDoc = "C#: XML Doc";
		public const string CSharpUserSymbol = "C#: User symbol";
		public const string CSharpMetadataSymbol = "C#: Metadata symbol";

		public const string CppFunction = "cppFunction";
		public const string CppClassTemplate = "cppClassTemplate";
		public const string CppFunctionTemplate = "cppFunctionTemplate";
		public const string CppEvent = "cppEvent";
		public const string CppGenericType = "cppGenericType";
		public const string CppGlobalVariable = "cppGlobalVariable";
		public const string CppLabel = "cppLabel";
		public const string CppLocalVariable = "cppLocalVariable";
		public const string CppMacro = "cppMacro"; // not mapped
		public const string CppMemberField = "cppMemberField";
		public const string CppMemberFunction = "cppMemberFunction";
		public const string CppMemberOperator = "cppMemberOperator";
		public const string CppNamespace = "cppNamespace";
		public const string CppNewDelete = "cppNewDelete"; // not mapped
		public const string CppParameter = "cppParameter";
		public const string CppOperator = "cppOperator";
		public const string CppProperty = "cppProperty";
		public const string CppRefType = "cppRefType"; // not mapped
		public const string CppStaticMemberField = "cppStaticMemberField"; // not mapped
		public const string CppStaticMemberFunction = "cppStaticMemberFunction"; // not mapped
		public const string CppType = "cppType";
		public const string CppUserDefinedLiteralNumber = "cppUserDefinedLiteralNumber"; // not mapped
		public const string CppUserDefinedLiteralRaw = "cppUserDefinedLiteralRaw"; // not mapped
		public const string CppUserDefinedLiteralString = "cppUserDefinedLiteralString"; // not mapped
		public const string CppValueType = "cppValueType";

		public const string XmlAttributeName = "XML Attribute";
		public const string XmlAttributeQuotes = "XML Attribute Quotes";
		public const string XmlAttributeValue = "XML Attribute Value";
		public const string XmlCData = "XML CData Section";
		public const string XmlComment = "XML Comment";
		public const string XmlDelimiter = "XML Delimiter";
		public const string XmlName = "XML Name";
		public const string XmlProcessingInstruction = "XML Processing Instruction";
		public const string XmlText = "XML Text";

		//public const string EditorIntellisense = "intellisense";
		//public const string EditorSigHelp = "sighelp";
		//public const string EditorSigHelpDoc = "sighelp-doc";

		internal const string CodistPrefix = "Codist: ";
		//! Important
		//# Notice
		public const string EmphasisComment = CodistPrefix + "Emphasis";
		//? Question
		public const string QuestionComment = CodistPrefix + "Question";
		//!? Exclaimation
		public const string ExclaimationComment = CodistPrefix + "Exclaimation";
		//x Removed
		public const string DeletionComment = CodistPrefix + "Deletion";

		//TODO: This does not need work
		public const string TodoComment = CodistPrefix + "Task - ToDo";
		//NOTE: Watch-out!
		public const string NoteComment = CodistPrefix + "Task - Note";
		//Hack: B-) We are in the Matrix now!!!
		public const string HackComment = CodistPrefix + "Task - Hack";
		//Undone: The revolution has not yet succeeded. Comrades still need to strive hard.
		public const string UndoneComment = CodistPrefix + "Task - Undone";

		//+++ heading 1
		public const string Heading1Comment = CodistPrefix + "Heading 1";
		//++ heading 2
		public const string Heading2Comment = CodistPrefix + "Heading 2";
		//+ heading 3
		public const string Heading3Comment = CodistPrefix + "Heading 3";
		//- heading 4
		public const string Heading4Comment = CodistPrefix + "Heading 4";
		//-- heading 5
		public const string Heading5Comment = CodistPrefix + "Heading 5";
		//--- heading 6
		public const string Heading6Comment = CodistPrefix + "Heading 6";

		public const string Task1Comment = CodistPrefix + "Task 1";
		public const string Task2Comment = CodistPrefix + "Task 2";
		public const string Task3Comment = CodistPrefix + "Task 3";
		public const string Task4Comment = CodistPrefix + "Task 4";
		public const string Task5Comment = CodistPrefix + "Task 5";
		public const string Task6Comment = CodistPrefix + "Task 6";
		public const string Task7Comment = CodistPrefix + "Task 7";
		public const string Task8Comment = CodistPrefix + "Task 8";
		public const string Task9Comment = CodistPrefix + "Task 9";

		public const string Highlight1 = CodistPrefix + "Highlight 1";
		public const string Highlight2 = CodistPrefix + "Highlight 2";
		public const string Highlight3 = CodistPrefix + "Highlight 3";
		public const string Highlight4 = CodistPrefix + "Highlight 4";
		public const string Highlight5 = CodistPrefix + "Highlight 5";
		public const string Highlight6 = CodistPrefix + "Highlight 6";
		public const string Highlight7 = CodistPrefix + "Highlight 7";
		public const string Highlight8 = CodistPrefix + "Highlight 8";
		public const string Highlight9 = CodistPrefix + "Highlight 9";

		public static readonly Color CommentColor = Colors.Green;
		public static readonly Color QuestionColor = Colors.MediumPurple;
		public static readonly Color ExclaimationColor = Colors.IndianRed;
		public static readonly Color DeletionColor = Colors.Gray;
		public static readonly Color ToDoColor = Colors.DarkBlue;
		public static readonly Color NoteColor = Colors.Orange;
		public static readonly Color HackColor = Colors.Black;
		public static readonly Color UndoneColor = Color.FromRgb(113, 65, 54);
		public static readonly Color TaskColor = Colors.Red;
		public static readonly Color ControlFlowColor = Colors.MediumBlue;
	}

	enum CommentStyleTypes
	{
		[ClassificationType(ClassificationTypeNames = Constants.CodeComment)]
		Default,
		[ClassificationType(ClassificationTypeNames = Constants.EmphasisComment)]
		Emphasis,
		[ClassificationType(ClassificationTypeNames = Constants.QuestionComment)]
		Question,
		[ClassificationType(ClassificationTypeNames = Constants.ExclaimationComment)]
		Exclaimation,
		[ClassificationType(ClassificationTypeNames = Constants.DeletionComment)]
		Deletion,
		[ClassificationType(ClassificationTypeNames = Constants.TodoComment)]
		ToDo,
		[ClassificationType(ClassificationTypeNames = Constants.NoteComment)]
		Note,
		[ClassificationType(ClassificationTypeNames = Constants.HackComment)]
		Hack,
		[ClassificationType(ClassificationTypeNames = Constants.UndoneComment)]
		Undone,
		[ClassificationType(ClassificationTypeNames = Constants.Heading1Comment)]
		Heading1,
		[ClassificationType(ClassificationTypeNames = Constants.Heading2Comment)]
		Heading2,
		[ClassificationType(ClassificationTypeNames = Constants.Heading3Comment)]
		Heading3,
		[ClassificationType(ClassificationTypeNames = Constants.Heading4Comment)]
		Heading4,
		[ClassificationType(ClassificationTypeNames = Constants.Heading5Comment)]
		Heading5,
		[ClassificationType(ClassificationTypeNames = Constants.Heading6Comment)]
		Heading6,
		[ClassificationType(ClassificationTypeNames = Constants.Task1Comment)]
		Task1,
		[ClassificationType(ClassificationTypeNames = Constants.Task2Comment)]
		Task2,
		[ClassificationType(ClassificationTypeNames = Constants.Task3Comment)]
		Task3,
		[ClassificationType(ClassificationTypeNames = Constants.Task4Comment)]
		Task4,
		[ClassificationType(ClassificationTypeNames = Constants.Task5Comment)]
		Task5,
		[ClassificationType(ClassificationTypeNames = Constants.Task6Comment)]
		Task6,
		[ClassificationType(ClassificationTypeNames = Constants.Task7Comment)]
		Task7,
		[ClassificationType(ClassificationTypeNames = Constants.Task8Comment)]
		Task8,
		[ClassificationType(ClassificationTypeNames = Constants.Task9Comment)]
		Task9,
	}

	enum CodeStyleTypes
	{
		None,
		[Category(Constants.SyntaxCategory.Keyword)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeKeyword)]
		Keyword,
		[Category(Constants.SyntaxCategory.TypeDefinition)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeClassName)]
		ClassName,
		[Category(Constants.SyntaxCategory.TypeDefinition)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeStructName)]
		StructName,
		[Category(Constants.SyntaxCategory.TypeDefinition)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeInterfaceName)]
		InterfaceName,
		[Category(Constants.SyntaxCategory.TypeDefinition)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeEnumName)]
		EnumName,
		[Category(Constants.SyntaxCategory.General)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeIdentifier)]
		[Description("A base style shared by type, type member, local, parameter, etc.")]
		Identifier,
		[Category(Constants.SyntaxCategory.General)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeNumber)]
		Number,
		[Category(Constants.SyntaxCategory.General)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeString)]
		[Description("Literal string")]
		String,
		[Category(Constants.SyntaxCategory.General)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeStringVerbatim)]
		[Description("Multiline literal string")]
		StringVerbatim,
		[Category(Constants.SyntaxCategory.General)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeOperator)]
		Operator,
		[Category(Constants.SyntaxCategory.General)]
		[ClassificationType(ClassificationTypeNames = Constants.CodePunctuation)]
		Punctuation,
		[Category(Constants.SyntaxCategory.General)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeUrl)]
		Url,
		[Category(Constants.SyntaxCategory.Preprocessor)]
		[ClassificationType(ClassificationTypeNames = Constants.CodePreprocessorText)]
		PreprocessorText,
		[Category(Constants.SyntaxCategory.Preprocessor)]
		[ClassificationType(ClassificationTypeNames = Constants.CodePreprocessorKeyword)]
		PreprocessorKeyword,
		[Category(Constants.SyntaxCategory.Comment)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeComment)]
		Comment,
		[Category(Constants.SyntaxCategory.CompilerMarked)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeExcluded)]
		ExcludedCode,
		[Category(Constants.SyntaxCategory.CompilerMarked)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeUnnecessary)]
		UnnecessaryCode,
	}
	enum CppStyleTypes
	{
		None,
		[ClassificationType(ClassificationTypeNames = Constants.CppFunction)]
		Function,
		[ClassificationType(ClassificationTypeNames = Constants.CppClassTemplate)]
		ClassTemplate,
		[ClassificationType(ClassificationTypeNames = Constants.CppFunctionTemplate)]
		FunctionTemplate,
		[ClassificationType(ClassificationTypeNames = Constants.CppEvent)]
		Event,
		[ClassificationType(ClassificationTypeNames = Constants.CppGenericType)]
		GenericType,
		[ClassificationType(ClassificationTypeNames = Constants.CppGlobalVariable)]
		GlobalVariable,
		[ClassificationType(ClassificationTypeNames = Constants.CppLabel)]
		Label,
		[ClassificationType(ClassificationTypeNames = Constants.CppLocalVariable)]
		LocalVariable,
		[ClassificationType(ClassificationTypeNames = Constants.CppMacro)]
		Macro,
		[ClassificationType(ClassificationTypeNames = Constants.CppMemberField)]
		MemberField,
		[ClassificationType(ClassificationTypeNames = Constants.CppMemberFunction)]
		MemberFunction,
		[ClassificationType(ClassificationTypeNames = Constants.CppMemberOperator)]
		MemberOperator,
		[ClassificationType(ClassificationTypeNames = Constants.CppNamespace)]
		Namespace,
		[ClassificationType(ClassificationTypeNames = Constants.CppNewDelete)]
		NewDelete,
		[ClassificationType(ClassificationTypeNames = Constants.CppParameter)]
		Parameter,
		[ClassificationType(ClassificationTypeNames = Constants.CppOperator)]
		Operator,
		[ClassificationType(ClassificationTypeNames = Constants.CppProperty)]
		Property,
		[ClassificationType(ClassificationTypeNames = Constants.CppRefType)]
		RefType,
		[ClassificationType(ClassificationTypeNames = Constants.CppStaticMemberField)]
		StaticMemberField,
		[ClassificationType(ClassificationTypeNames = Constants.CppStaticMemberFunction)]
		StaticMemberFunction,
		[ClassificationType(ClassificationTypeNames = Constants.CppType)]
		Type,
		[ClassificationType(ClassificationTypeNames = Constants.CppUserDefinedLiteralNumber)]
		UserDefinedLiteralNumber,
		[ClassificationType(ClassificationTypeNames = Constants.CppUserDefinedLiteralRaw)]
		UserDefinedLiteralRaw,
		[ClassificationType(ClassificationTypeNames = Constants.CppUserDefinedLiteralString)]
		UserDefinedLiteralString,
		[ClassificationType(ClassificationTypeNames = Constants.CppValueType)]
		ValueType,
	}
	enum CSharpStyleTypes
	{
		None,
		[Category(Constants.SyntaxCategory.Keyword)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeControlFlowKeyword)]
		[Description("Keyword: break, continue, yield, return, inheriting from Keyword")]
		BreakAndReturnKeyword,
		[Category(Constants.SyntaxCategory.Keyword)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeAbstractionKeyword)]
		[Description("Keyword: abstract, override, sealed, virtual, inheriting from Keyword")]
		AbstractionKeyword,
		[Category(Constants.SyntaxCategory.Keyword)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeBranchingKeyword)]
		[Description("Keyword: switch, case, default, if, else, inheriting from Keyword")]
		BranchingKeyword,
		[Category(Constants.SyntaxCategory.Keyword)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeLoopKeyword)]
		[Description("Keyword: for, foreach in, do, while, inheriting from Keyword")]
		LoopKeyword,
		[Category(Constants.SyntaxCategory.Keyword)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpResourceKeyword)]
		[Description("Keyword: using, lock, try catch finally, fixed, unsafe, inheriting from Keyword")]
		ResourceAndExceptionKeyword,
		[Category(Constants.SyntaxCategory.Declaration)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpDeclarationName)]
		[Description("Declaration of non-nested type: class, struct, interface, enum, delegate and event, inheriting from Identifier")]
		Declaration,
		[Category(Constants.SyntaxCategory.Declaration)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpNestedDeclarationName)]
		[Description("Declaration of type memeber: property, method, event, delegate, nested type, etc. (excluding fields), inheriting from Declaration")]
		MemberDeclaration,
		[Category(Constants.SyntaxCategory.Declaration)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpDeclarationBrace)]
		[Description("Braces {} for declaration, inheriting from Punctuation")]
		DeclarationBrace,
		[Category(Constants.SyntaxCategory.Declaration)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpStaticMemberName)]
		[Description("Name of static member, inheriting from Identifier")]
		StaticMemberName,
		[Category(Constants.SyntaxCategory.Declaration)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpOverrideMemberName)]
		[Description("Name of overriding member, inheriting from Identifier")]
		OverrideMemberName,
		[Category(Constants.SyntaxCategory.Declaration)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpAbstractMemberName)]
		[Description("Name of abstract member, inheriting from Identifier")]
		AbstractMemberName,
		[Category(Constants.SyntaxCategory.Declaration)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpVirtualMemberName)]
		[Description("Name of virtual member, inheriting from Identifier")]
		VirtualMemberName,
		[Category(Constants.SyntaxCategory.Declaration)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpLocalVariableName)]
		[Description("Name of local variable, inheriting from Identifier")]
		LocalVariableName,
		[Category(Constants.SyntaxCategory.Declaration)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpLabel)]
		[Description("Name of label, inheriting from Identifier")]
		Label,
		[Category(Constants.SyntaxCategory.Declaration)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpAttributeName)]
		[Description("Name of attribute annotation, inheriting from Class Name")]
		AttributeName,
		[Category(Constants.SyntaxCategory.Declaration)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpAttributeNotation)]
		[Description("Whole region of attribute annotation")]
		AttributeNotation,

		[Category(Constants.SyntaxCategory.TypeDefinition)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpNamespaceName)]
		NamespaceName,
		[Category(Constants.SyntaxCategory.TypeDefinition)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpSealedClassName)]
		[Description("Name of sealed class, inheriting from Class Name")]
		SealedClassName,
		[Category(Constants.SyntaxCategory.TypeDefinition)]
		[ClassificationType(ClassificationTypeNames = Constants.CodeDelegateName)]
		[Description("Name of delegate, inheriting from Identifier")]
		DelegateName,
		[Category(Constants.SyntaxCategory.TypeDefinition)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpEventName)]
		[Description("Name of event, inheriting from Identifier")]
		EventName,
		[Category(Constants.SyntaxCategory.TypeDefinition)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpTypeParameterName)]
		[Description("Name of type parameter, inheriting from Identifier")]
		TypeParameterName,

		//[ClassificationType(ClassificationTypeNames = Constants.CodeModuleName)]
		//ModuleDeclaration,
		[Category(Constants.SyntaxCategory.Member)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpConstructorMethodName)]
		[Description("Name of constructor, inheriting from Method Name")]
		ConstructorMethodName,
		[Category(Constants.SyntaxCategory.Member)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpFieldName)]
		[Description("Name of field, inheriting from Identifier")]
		FieldName,
		[Category(Constants.SyntaxCategory.Member)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpConstFieldName)]
		[Description("Name of constant field, inheriting from Read Only Field Name")]
		ConstFieldName,
		[Category(Constants.SyntaxCategory.Member)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpReadOnlyFieldName)]
		[Description("Name of read-only field, inheriting from Field Name")]
		ReadOnlyFieldName,
		[Category(Constants.SyntaxCategory.Member)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpPropertyName)]
		[Description("Name of property, inheriting from Identifier")]
		PropertyName,
		[Category(Constants.SyntaxCategory.Member)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpMethodName)]
		[Description("Name of method, inheriting from Identifier")]
		MethodName,
		[Category(Constants.SyntaxCategory.Member)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpExtensionMethodName)]
		[Description("Name of extension method, inheriting from Method Name and Static Member Name")]
		ExtensionMethodName,
		[Category(Constants.SyntaxCategory.Member)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpExternMethodName)]
		[Description("Name of extern method, inheriting from Method Name")]
		ExternMethodName,
		[Category(Constants.SyntaxCategory.Member)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpParameterName)]
		[Description("Name of parameter, inheriting from Identifier")]
		ParameterName,

		[Category(Constants.SyntaxCategory.Comment)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpXmlDoc)]
		[Description("Whole region of XML Documentation")]
		XmlDoc,
		[Category(Constants.SyntaxCategory.Comment)]
		[ClassificationType(ClassificationTypeNames = Constants.XmlDocComment)]
		[Description("Comment text of XML Documentation")]
		XmlDocComment,
		[Category(Constants.SyntaxCategory.Comment)]
		[ClassificationType(ClassificationTypeNames = Constants.XmlDocTag)]
		[Description("Tag of XML Documentation")]
		XmlDocTag,
		[Category(Constants.SyntaxCategory.Comment)]
		[ClassificationType(ClassificationTypeNames = Constants.XmlDocAttributeName)]
		[Description("Attribute name of XML Documentation")]
		XmlDocAttributeName,
		[Category(Constants.SyntaxCategory.Comment)]
		[ClassificationType(ClassificationTypeNames = Constants.XmlDocAttributeValue)]
		[Description("Attribute value of XML Documentation")]
		XmlDocAttributeValue,
		[Category(Constants.SyntaxCategory.Comment)]
		[ClassificationType(ClassificationTypeNames = Constants.XmlDocDelimiter)]
		[Description("Tag characters of XML Documentation")]
		XmlDocDelimiter,
		[Category(Constants.SyntaxCategory.Comment)]
		[ClassificationType(ClassificationTypeNames = Constants.XmlDocCData)]
		[Description("CData content of XML Documentation")]
		XmlDocCData,
	}

	enum XmlStyleTypes
	{
		None,
		[Category(Constants.SyntaxCategory.Xml)]
		[ClassificationType(ClassificationTypeNames = Constants.XmlAttributeName)]
		XmlAttributeName,
		[Category(Constants.SyntaxCategory.Xml)]
		[ClassificationType(ClassificationTypeNames = Constants.XmlAttributeQuotes)]
		XmlAttributeQuotes,
		[Category(Constants.SyntaxCategory.Xml)]
		[ClassificationType(ClassificationTypeNames = Constants.XmlAttributeValue)]
		XmlAttributeValue,
		[Category(Constants.SyntaxCategory.Xml)]
		[ClassificationType(ClassificationTypeNames = Constants.XmlCData)]
		XmlCData,
		[Category(Constants.SyntaxCategory.Xml)]
		[ClassificationType(ClassificationTypeNames = Constants.XmlComment)]
		XmlComment,
		[Category(Constants.SyntaxCategory.Xml)]
		[ClassificationType(ClassificationTypeNames = Constants.XmlDelimiter)]
		XmlDelimiter,
		//[Category(Constants.SyntaxCategory.Xml)]
		//[ClassificationType(ClassificationTypeNames = Constants.XmlEntityReference)]
		//XmlEntityReference,
		[Category(Constants.SyntaxCategory.Xml)]
		[ClassificationType(ClassificationTypeNames = Constants.XmlName)]
		XmlName,
		[Category(Constants.SyntaxCategory.Xml)]
		[ClassificationType(ClassificationTypeNames = Constants.XmlProcessingInstruction)]
		XmlProcessingInstruction,
		[Category(Constants.SyntaxCategory.Xml)]
		[ClassificationType(ClassificationTypeNames = Constants.XmlText)]
		XmlText,
	}
	enum SymbolMarkerStyleTypes
	{
		None,
		[Category(Constants.SyntaxCategory.Highlight)]
		[ClassificationType(ClassificationTypeNames = Constants.Highlight1)]
		Highlight1,
		[Category(Constants.SyntaxCategory.Highlight)]
		[ClassificationType(ClassificationTypeNames = Constants.Highlight2)]
		Highlight2,
		[Category(Constants.SyntaxCategory.Highlight)]
		[ClassificationType(ClassificationTypeNames = Constants.Highlight3)]
		Highlight3,
		[Category(Constants.SyntaxCategory.Highlight)]
		[ClassificationType(ClassificationTypeNames = Constants.Highlight4)]
		Highlight4,
		[Category(Constants.SyntaxCategory.Highlight)]
		[ClassificationType(ClassificationTypeNames = Constants.Highlight5)]
		Highlight5,
		[Category(Constants.SyntaxCategory.Highlight)]
		[ClassificationType(ClassificationTypeNames = Constants.Highlight6)]
		Highlight6,
		[Category(Constants.SyntaxCategory.Highlight)]
		[ClassificationType(ClassificationTypeNames = Constants.Highlight7)]
		Highlight7,
		[Category(Constants.SyntaxCategory.Highlight)]
		[ClassificationType(ClassificationTypeNames = Constants.Highlight8)]
		Highlight8,
		[Category(Constants.SyntaxCategory.Highlight)]
		[ClassificationType(ClassificationTypeNames = Constants.Highlight9)]
		Highlight9,

		[Category(Constants.SyntaxCategory.Location)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpUserSymbol)]
		[Description("Type and member defined in source code")]
		MyTypeAndMember,
		[Category(Constants.SyntaxCategory.Location)]
		[ClassificationType(ClassificationTypeNames = Constants.CSharpMetadataSymbol)]
		[Description("Type and member imported via referencing assembly")]
		ReferencedTypeAndMember,
	}

	enum MarkerStyleTypes
	{
		None,
		SymbolReference,
	}
	enum CommentStyleApplication
	{
		Content,
		Tag,
		TagAndContent
	}
	enum DebuggerStatus
	{
		Design,
		Running,
		Break,
		EditAndContinue
	}
}
