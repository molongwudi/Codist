﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using Codist.SyntaxHighlight;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Classification;

namespace Codist.Options
{
	[ToolboxItem(false)]
	public partial class SyntaxStyleOptionPage : UserControl
	{
		readonly Func<IEnumerable<StyleBase>> _defaultStyleLoader;
		readonly ConfigPage _service;
		readonly Func<IEnumerable<StyleBase>> _styleLoader;
		readonly IEditorFormatMap _FormatMap = ServicesHelper.Instance.EditorFormatMap.GetEditorFormatMap("text");
		StyleBase _activeStyle;
		bool _loaded;
		bool _uiLock;
		public SyntaxStyleOptionPage() {
			InitializeComponent();
			_BackgroundEffectBox.Items.AddRange(new[] { "Solid", "Bottom gradient", "Top gradient", "Right gradient", "Left gradient" });
		}
		internal SyntaxStyleOptionPage(ConfigPage service, Func<IEnumerable<StyleBase>> styleLoader, Func<IEnumerable<StyleBase>> defaultStyleLoader) : this() {
			_service = service;
			_styleLoader = styleLoader;
			_defaultStyleLoader = defaultStyleLoader;
		}

		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);
			_StyleSettingsBox.Enabled = _SyntaxListBox.SelectedIndices.Count > 0 && _SyntaxListBox.SelectedItems[0] is SyntaxListViewItem;
			if (_loaded) {
				return;
			}
			LoadStyleList();
			_BackColorButton.Click += SetBackColor;
			_BackColorTransBox.ValueChanged += SetBackColor;
			_ForeColorButton.Click += SetForeColor;
			_ForeColorTransBox.ValueChanged += SetForeColor;
			var ff = new InstalledFontCollection().Families;
			_FontBox.Items.Add(new FontFamilyItem());
			_FontBox.SelectedIndex = 0;
			foreach (var item in ff) {
				_FontBox.Items.Add(new FontFamilyItem(item));
			}
			_FontBox.SelectedIndexChanged += (s, args) => {
				if (_uiLock == false && _activeStyle != null) {
					_activeStyle.Font = _FontBox.Text.Length == 0 ? null : _FontBox.Text;
				}
			};
			_BoldBox.CheckStateChanged += (s, args) => {
				if (_uiLock == false && _activeStyle != null) {
					_activeStyle.Bold = ToBool(_BoldBox.CheckState);
				}
			};
			_ItalicBox.CheckStateChanged += (s, args) => {
				if (_uiLock == false && _activeStyle != null) {
					_activeStyle.Italic = ToBool(_ItalicBox.CheckState);
				}
			};
			_UnderlineBox.CheckStateChanged += (s, args) => {
				if (_uiLock == false && _activeStyle != null) {
					_activeStyle.Underline = ToBool(_UnderlineBox.CheckState);
				}
			};
			_StrikeBox.CheckStateChanged += (s, args) => {
				if (_uiLock == false && _activeStyle != null) {
					_activeStyle.Strikethrough = ToBool(_StrikeBox.CheckState);
				}
			};
			_BackgroundEffectBox.SelectedIndexChanged += (s, args) => {
				if (_uiLock == false && _activeStyle != null) {
					_activeStyle.BackgroundEffect = (BrushEffect)_BackgroundEffectBox.SelectedIndex;
				}
			};
			_FontSizeBox.ValueChanged += (s, args) => {
				if (_uiLock == false && _activeStyle != null) {
					_activeStyle.FontSize = (double)_FontSizeBox.Value;
				}
			};
			foreach (var item in new[] { _FontBox, _BackgroundEffectBox }) {
				item.SelectedIndexChanged += MarkChanged;
			}
			foreach (var item in new Control[] { _BackColorButton, _ForeColorButton }) {
				item.Click += MarkChanged;
			}
			foreach (var item in new[] { _BoldBox, _ItalicBox, _UnderlineBox, _StrikeBox }) {
				item.CheckStateChanged += MarkChanged;
			}
			foreach (var item in new[] { _BackColorTransBox, _ForeColorTransBox, _FontSizeBox }) {
				item.ValueChanged += MarkChanged;
			}
			_PreviewBox.SizeChanged += (s, args) => UpdatePreview();
			_SyntaxListBox.ItemSelectionChanged += _SyntaxListBox_ItemSelectionChanged;
			Config.Loaded += (s, args) => { _activeStyle = null; LoadStyleList(); };
			_loaded = true;
		}

		void RenderPreview(Bitmap bmp, string fontName, int fontSize, StyleBase style) {
			var size = (float)(fontSize + style.FontSize);
			if (size < 2) {
				return;
			}
			UIHelper.MixStyle(style, out var fs, out var fc, out var bc);
			_ForeColorButton.DefaultColor = fc;
			using (var g = Graphics.FromImage(bmp))
			using (var f = new Font(String.IsNullOrEmpty(style.Font) ? fontName : style.Font, size, fs))
			using (var b = new SolidBrush(fc))
			using (var bg = new SolidBrush(ThemeHelper.DocumentPageColor)) {
				g.FillRectangle(bg, 0, 0, bmp.Width, bmp.Height);
				const string t = "Preview 01ioIOlLWM";
				var m = g.MeasureString(t, f, bmp.Size);
				g.SmoothingMode = SmoothingMode.HighQuality;
				g.TextRenderingHint = TextRenderingHint.AntiAlias;
				g.CompositingQuality = CompositingQuality.HighQuality;
				using (var bb = ConfigPage.GetPreviewBrush(style.BackgroundEffect, bc, ref m)) {
					g.FillRectangle(bb, new Rectangle(0, 0, (int)m.Width, (int)m.Height));
				}
				g.DrawString(t, f, b, new RectangleF(PointF.Empty, bmp.PhysicalDimension));
			}
		}

		static bool? ToBool(CheckState state) {
			switch (state) {
				case CheckState.Unchecked: return false;
				case CheckState.Checked: return true;
				default: return null;
			}
		}

		static CheckState ToCheckState(bool? value) {
			return value.HasValue == false
				? CheckState.Indeterminate
				: value.Value
				? CheckState.Checked
				: CheckState.Unchecked;
		}

		void _SyntaxListBox_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e) {
			if (e.ItemIndex == -1) {
				return;
			}
			var i = (e.Item as SyntaxListViewItem)?.Style;
			if (i == null) {
				e.Item.Selected = false;
				_StyleSettingsBox.Enabled = false;
				return;
			}
			_StyleSettingsBox.Enabled = true;
			_uiLock = true;
			_activeStyle = i;
			UpdateUIControls(i);

			UpdatePreview();
			_uiLock = false;
		}

		void UpdateUIControls(StyleBase style) {
			_BoldBox.CheckState = ToCheckState(style.Bold);
			_ItalicBox.CheckState = ToCheckState(style.Italic);
			_StrikeBox.CheckState = ToCheckState(style.Strikethrough);
			_UnderlineBox.CheckState = ToCheckState(style.Underline);
			_BackgroundEffectBox.SelectedIndex = (int)style.BackgroundEffect;

			_FontBox.Text = style.Font;
			_FontSizeBox.Value = style.FontSize > 100 ? 100m : style.FontSize < -10 ? -10m : (decimal)style.FontSize;
			_ForeColorTransBox.Value = style.ForeColor.A;
			_BackColorTransBox.Value = style.BackColor.A;
			_ForeColorButton.SelectedColor = style.ForeColor.ToGdiColor();
			_BackColorButton.SelectedColor = style.BackColor.ToGdiColor();
		}

		static ListViewItem GetListItemForStyle(string category, ListViewItem vi) {
			vi.Text = category;
			vi.IndentCount = 1;
			switch (category) {
				case Constants.SyntaxCategory.Comment: vi.BackColor = Color.LightGreen; vi.ForeColor = Color.Black; break;
				case Constants.SyntaxCategory.CompilerMarked: vi.BackColor = Color.LightGray; vi.ForeColor = Color.Black; break;
				case Constants.SyntaxCategory.Declaration: vi.BackColor = Color.LightCyan; vi.ForeColor = Color.Black; break;
				case Constants.SyntaxCategory.Keyword: vi.BackColor = Color.LightBlue; vi.ForeColor = Color.Black; break;
				case Constants.SyntaxCategory.Preprocessor: vi.BackColor = Color.Gray; vi.ForeColor = Color.Black; break;
				case Constants.SyntaxCategory.Member: vi.BackColor = Color.LightCoral; vi.ForeColor = Color.Black; break;
				case Constants.SyntaxCategory.TypeDefinition: vi.BackColor = Color.LightYellow; vi.ForeColor = Color.Black; break;
			}
			return vi;
		}

		void LoadStyleList() {
			_uiLock = true;
			_SyntaxListBox.Items.Clear();
			//_SyntaxListBox.Groups.Clear();
			//var groups = new List<ListViewGroup>(5);
			var defaultStyles = _defaultStyleLoader();
			var styles = _styleLoader();
			//foreach (var item in defaultStyles) {
			//	if (groups.FirstOrDefault(i => i.Header == item.Category) != null) {
			//		continue;
			//	}
			//	groups.Add(new ListViewGroup(item.Category, HorizontalAlignment.Center));
			//}
			//_SyntaxListBox.Groups.AddRange(groups.ToArray());
			string category = null;
			foreach (var item in defaultStyles) {
				if (item.Category.Length == 0) {
					continue;
				}
				var style = styles.FirstOrDefault(i => i.Id == item.Id) ?? item;
				if (item.Category != category) {
					_SyntaxListBox.Items.Add(new ListViewItem("   - " + (category = item.Category) + " -") {
						Font = new Font(_SyntaxListBox.Font, FontStyle.Bold)
					});
				}
				_SyntaxListBox.Items.Add(new SyntaxListViewItem(item.ToString(), style) {
					//Group = groups.FirstOrDefault(i => i.Header == item.Category),
					Font = new Font(_SyntaxListBox.Font, style.GetFontStyle())
				});
			}
			_uiLock = false;
		}

		void MarkChanged(object sender, EventArgs args) {
			if (_uiLock || _activeStyle == null) {
				return;
			}
			_uiLock = true;
			Config.Instance.FireConfigChangedEvent(Features.SyntaxHighlight);
			UpdatePreview();
			_uiLock = false;
		}

		void ResetButton_Click(object sender, EventArgs e) {
			if (_activeStyle == null) {
				return;
			}
			_uiLock = true;
			_activeStyle.Reset();
			Config.Instance.FireConfigChangedEvent(Features.SyntaxHighlight);
			UpdateUIControls(_activeStyle);
			UpdatePreview();
			_uiLock = false;
		}

		private void SetBackColor(object sender, EventArgs args) {
			if (_uiLock || _activeStyle == null) {
				return;
			}
			if (sender == _BackColorButton && _BackColorTransBox.Value == 0) {
				_BackColorTransBox.Value = 255;
			}
			_BackColorButton.SelectedColor = _BackColorButton.SelectedColor.Alpha((byte)_BackColorTransBox.Value);
			_activeStyle.BackColor = _BackColorButton.SelectedColor.ToWpfColor();
		}

		private void SetForeColor(object sender, EventArgs args) {
			if (_uiLock || _activeStyle == null) {
				return;
			}
			if (sender == _ForeColorButton && _ForeColorTransBox.Value == 0) {
				_ForeColorTransBox.Value = 255;
			}
			_ForeColorButton.SelectedColor = _ForeColorButton.SelectedColor.Alpha((byte)_ForeColorTransBox.Value);
			_activeStyle.ForeColor = _ForeColorButton.SelectedColor.ToWpfColor();
		}
		void UpdatePreview() {
			if (_activeStyle == null) {
				return;
			}
			var bmp = new Bitmap(_PreviewBox.Width, _PreviewBox.Height);
			ThemeHelper.GetFontSettings(FontsAndColorsCategory.TextEditor, out var fontName, out var fontSize);
			RenderPreview(bmp, fontName, fontSize, _activeStyle);
			(_SyntaxListBox.FocusedItem as SyntaxListViewItem)?.ApplyTheme();
			_PreviewBox.Image = bmp;
		}
		struct FontFamilyItem
		{
			internal readonly FontFamily FontFamily;
			internal readonly string Name;
			public FontFamilyItem(FontFamily fontFamily) {
				Name = fontFamily.GetName(0);
				FontFamily = fontFamily;
			}
			public override string ToString() {
				return Name ?? String.Empty;
			}
		}
	}
}
