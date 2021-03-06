﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Codist.Classifiers
{
	sealed class TaggerResult
	{
		/// <summary>The snapshot version.</summary>
		public int Version { get; set; }
		/// <summary>The first parsed position.</summary>
		public int Start { get; set; }
		/// <summary>The last parsed position.</summary>
		public int LastParsed { get; set; }
		/// <summary>The parsed tags.</summary>
		public List<SpanTag> Tags { get; set; } = new List<SpanTag>();

		public TagSpan<ClassificationTag> Add(TagSpan<ClassificationTag> tag) {
			var s = tag.Span;
			if (s.Start < Start) {
				Start = s.Start;
			}
			for (int i = Tags.Count - 1; i >= 0; i--) {
				if (Tags[i].Contains(s.Start)) {
					Tags[i] = new SpanTag(tag);
					return tag;
				}
			}
			Tags.Add(new SpanTag(tag));
			return tag;
		}

		public void Reset() {
			Start = LastParsed = 0;
			Tags.Clear();
		}
	}

	[DebuggerDisplay("{Start}..{End} {Tag.ClassificationType}")]
	sealed class SpanTag
	{
		public int Start { get; set; }
		public int Length { get; set; }
		public int End => Start + Length;
		//todo: customizable marker style
		public ClassificationTag Tag { get; set; }
		public SpanTag (TagSpan<ClassificationTag> tagSpan) {
			Start = tagSpan.Span.Start;
			Length = tagSpan.Span.Length;
			Tag = tagSpan.Tag;
		}
		public bool Contains(int position) {
			return position >= Start && position < Start + Length;
		}
	}
}
