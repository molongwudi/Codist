﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using AppHelpers;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Codist.Classifiers
{
	//todo Extract comment tagger logic for various content types
	[Export(typeof(IViewTaggerProvider))]
	[ContentType(Constants.CodeTypes.Code)]
	[TagType(typeof(ClassificationTag))]
	sealed class CommentTaggerProvider : IViewTaggerProvider
	{
		[Import]
		internal IClassificationTypeRegistryService ClassificationRegistry = null;

		[Import]
		internal IBufferTagAggregatorFactoryService Aggregator = null;

		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag {
			if (Config.Instance.Features.MatchFlags(Features.SyntaxHighlight) == false) {
				return null;
			}
			var codeType = GetCodeType(textView.TextBuffer.ContentType);
			if (codeType == CodeType.None) {
				return null;
			}
			var vp = textView.Properties;
			var tagger = vp.GetOrCreateSingletonProperty(() => Aggregator.CreateTagAggregator<IClassificationTag>(buffer));
			var tags = vp.GetOrCreateSingletonProperty(() => new TaggerResult());
			var codeTagger = vp.GetOrCreateSingletonProperty(() => new CommentTagger(ClassificationRegistry, tagger, tags, codeType));
			textView.Closed += TextViewClosed;
			return codeTagger as ITagger<T>;
		}

		static CodeType GetCodeType(IContentType contentType) {
			return contentType.IsOfType(Constants.CodeTypes.CSharp) ? CodeType.CSharp
				: contentType.IsOfType("html") || contentType.IsOfType("htmlx") || contentType.IsOfType("XAML") || contentType.IsOfType("XML") ? CodeType.Markup
				: CodeType.None;
		}

		void TextViewClosed(object sender, EventArgs args) {
			var textView = sender as ITextView;
			textView.Properties.GetProperty<ITagAggregator<IClassificationTag>>(typeof(ITagAggregator<IClassificationTag>))?.Dispose();
			textView.Properties.GetProperty<CommentTagger>(typeof(CommentTagger))?.Dispose();
			textView.Closed -= TextViewClosed;
		}

		enum CodeType
		{
			None, CSharp, Markup
		}

		sealed class CommentTagger : ITagger<ClassificationTag>, IDisposable
		{
			static ClassificationTag[] __CommentClassifications;
			readonly ITagAggregator<IClassificationTag> _Aggregator;
			readonly TaggerResult _Tags;
			readonly CodeType _CodeType;
#if DEBUG
			readonly HashSet<string> _ClassificationTypes = new HashSet<string>();
#endif
			static readonly string[] __CSharpComments = { "//", "/*" };
			static readonly string[] __Comments = { "//", "/*", "'", "#", "<!--" };

#pragma warning disable 67
			public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
#pragma warning restore 67

			internal CommentTagger(IClassificationTypeRegistryService registry, ITagAggregator<IClassificationTag> aggregator, TaggerResult tags, CodeType codeType) {
				if (__CommentClassifications == null) {
					var t = typeof(CommentStyleTypes);
					var styleNames = Enum.GetNames(t);
					__CommentClassifications = new ClassificationTag[styleNames.Length];
					foreach (var styleName in styleNames) {
						var f = t.GetField(styleName);
						var d = f.GetCustomAttribute<ClassificationTypeAttribute>();
						if (d == null) {
							continue;
						}
						var ct = registry.GetClassificationType(d.ClassificationTypeNames);
						__CommentClassifications[(int)f.GetValue(null)] = new ClassificationTag(ct);
					}
				}

				_Aggregator = aggregator;
				_Tags = tags;
				_CodeType = codeType;
				_Aggregator.BatchedTagsChanged += AggregatorBatchedTagsChanged;
			}

			internal FrameworkElement Margin { get; set; }

			public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
				if (spans.Count == 0
				|| Config.Instance.SpecialHighlightOptions.MatchFlags(SpecialHighlightOptions.SpecialComment) == false) {
					yield break;
				}

				var snapshot = spans[0].Snapshot;
				var contentType = snapshot.TextBuffer.ContentType;
				if (!contentType.IsOfType(Constants.CodeTypes.Code)) {
					yield break;
				}
				IEnumerable<IMappingTagSpan<IClassificationTag>> tagSpans;
				try {
					if (_Tags.LastParsed == 0) {
						// perform a full parse for the first time
						Debug.WriteLine("Full parse");
						tagSpans = _Aggregator.GetTags(new SnapshotSpan(snapshot, 0, snapshot.Length));
						_Tags.LastParsed = snapshot.Length;
					}
					else {
						var start = spans[0].Start;
						var end = spans[spans.Count - 1].End;
						//Debug.WriteLine($"Get tag [{start.Position}..{end.Position})");

						tagSpans = _Aggregator.GetTags(spans);
					}
				}
				catch (ObjectDisposedException ex) {
					// HACK: TagAggregator could be disposed during editing, to be investigated further
					Debug.WriteLine(ex.Message);
					yield break;
				}

				foreach (var tagSpan in tagSpans) {
					var c = tagSpan.Tag.ClassificationType.Classification;
#if DEBUG
					if (_ClassificationTypes.Add(c)) {
						Debug.WriteLine("Classification type: " + c);
					}
#endif
					var ss = tagSpan.Span.GetSpans(snapshot)[0];
					if (_CodeType == CodeType.CSharp) {
						switch (c) {
							case Constants.CodePreprocessorKeyword:
								if (Config.Instance.MarkerOptions.MatchFlags(MarkerOptions.CompilerDirective)) {
									if (Matches(ss, "region") || Matches(ss, "pragma") || Matches(ss, "if") || Matches(ss, "else")) {
										yield return _Tags.Add(new TagSpan<ClassificationTag>(ss, (ClassificationTag)tagSpan.Tag));
									}
								}
								continue;
							default:
								break;
						}
					}

					var ts = TagComments(c, ss, tagSpan);
					if (ts != null) {
						yield return _Tags.Add(ts);
					}
				}
			}

			TagSpan<ClassificationTag> TagComments(string className, SnapshotSpan snapshotSpan, IMappingTagSpan<IClassificationTag> tagSpan) {
				// find spans that the language service has already classified as comments ...
				if (className.IndexOf("Comment", StringComparison.OrdinalIgnoreCase) == -1) {
					return null;
				}

				var text = snapshotSpan.GetText();
				//NOTE: markup comment span does not include comment start token
				var endOfCommentToken = 0;
				foreach (string t in _CodeType == CodeType.CSharp ? __CSharpComments : __Comments) {
					if (text.StartsWith(t, StringComparison.OrdinalIgnoreCase)) {
						endOfCommentToken = t.Length;
						break;
					}
				}

				if (endOfCommentToken == 0 && _CodeType != CodeType.Markup) {
					return null;
				}

				var tl = text.Length;
				var commentStart = endOfCommentToken;
				while (commentStart < tl) {
					if (Char.IsWhiteSpace(text[commentStart])) {
						++commentStart;
					}
					else {
						break;
					}
				}

				//TODO: code type context-awared end of comment
				var endOfContent = tl;
				if (_CodeType == CodeType.Markup && commentStart > 0) {
					if (!text.EndsWith("-->", StringComparison.Ordinal)) {
						return null;
					}

					endOfContent -= 3;
				}
				else if (text.StartsWith("/*", StringComparison.Ordinal)) {
					endOfContent -= 2;
				}

				ClassificationTag ctag = null;
				CommentLabel label = null;
				var startOfContent = 0;
				foreach (var item in Config.Instance.Labels) {
					var c = commentStart + item.LabelLength;
					if (c >= tl
						|| text.IndexOf(item.Label, commentStart, item.Comparison) != commentStart) {
						continue;
					}

					var followingChar = text[c];
					if (item.AllowPunctuationDelimiter && Char.IsPunctuation(followingChar)) {
						c++;
					}
					else if (!Char.IsWhiteSpace(followingChar)) {
						continue;
					}

					if (label == null || label.LabelLength < item.LabelLength) {
						ctag = __CommentClassifications[(int)item.StyleID];
						label = item;
						startOfContent = c;
					}
				}

				if (startOfContent == 0 || ctag == null) {
					return null;
				}

				// ignore whitespaces in content
				while (startOfContent < tl) {
					if (Char.IsWhiteSpace(text, startOfContent)) {
						++startOfContent;
					}
					else {
						break;
					}
				}
				while (endOfContent > startOfContent) {
					if (Char.IsWhiteSpace(text, endOfContent - 1)) {
						--endOfContent;
					}
					else {
						break;
					}
				}

				var span = label.StyleApplication == CommentStyleApplication.Tag
					? new SnapshotSpan(snapshotSpan.Snapshot, snapshotSpan.Start + commentStart, label.LabelLength)
					: label.StyleApplication == CommentStyleApplication.Content
					? new SnapshotSpan(snapshotSpan.Snapshot, snapshotSpan.Start + startOfContent, endOfContent - startOfContent)
					: new SnapshotSpan(snapshotSpan.Snapshot, snapshotSpan.Start + commentStart, endOfContent - commentStart);
				return new TagSpan<ClassificationTag>(span, ctag);
			}

			static bool Matches(SnapshotSpan span, string text) {
				if (span.Length < text.Length) {
					return false;
				}
				int start = span.Start;
				int end = span.End;
				var s = span.Snapshot;
				// the span can contain white spaces at the start or at the end, skip them
				while (Char.IsWhiteSpace(s[--end]) && end > 0) {
				}
				while (Char.IsWhiteSpace(s[start]) && start < end) {
					start++;
				}
				if (++end - start != text.Length) {
					return false;
				}
				for (int i = start, ti = 0; i < end; i++, ti++) {
					if (s[i] != text[ti]) {
						return false;
					}
				}
				return true;
			}

			void AggregatorBatchedTagsChanged(object sender, EventArgs args) {
				if (Margin != null) {
					Margin.InvalidateVisual();
				}
			}

			#region IDisposable Support
			private bool disposedValue = false;

			void Dispose(bool disposing) {
				if (!disposedValue) {
					if (disposing) {
						_Aggregator.BatchedTagsChanged -= AggregatorBatchedTagsChanged;
					}
					disposedValue = true;
				}
			}

			public void Dispose() {
				Dispose(true);
			}
			#endregion
		}
	}
}