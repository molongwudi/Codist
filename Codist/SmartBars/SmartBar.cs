﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Editor;
using AppHelpers;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
using System.Threading.Tasks;

namespace Codist.SmartBars
{
	//todo Make this class async
	/// <summary>The contextual toolbar.</summary>
	internal partial class SmartBar
	{
		const int Selecting = 1, Working = 2;
		/// <summary>The layer for the smart bar adornment.</summary>
		readonly IAdornmentLayer _ToolBarLayer;
		readonly ToolBarTray _ToolBarTray;
		CancellationTokenSource _Cancellation = new CancellationTokenSource();
		DateTime _LastExecute;
		DateTime _LastShiftHit;
		private int _TimerStatus;

		/// <summary>
		/// Initializes a new instance of the <see cref="SmartBar"/> class.
		/// </summary>
		/// <param name="view">The <see cref="IWpfTextView"/> upon which the adornment will be drawn</param>
		public SmartBar(IWpfTextView view) {
			View = view ?? throw new ArgumentNullException(nameof(view));
			_ToolBarLayer = view.GetAdornmentLayer(nameof(SmartBar));
			Config.Updated += ConfigUpdated;
			if (Config.Instance.SmartBarOptions.MatchFlags(SmartBarOptions.ShiftToggleDisplay)) {
				View.VisualElement.PreviewKeyUp += ViewKeyUpAsync;
			}
			if (Config.Instance.SmartBarOptions.MatchFlags(SmartBarOptions.ManualDisplaySmartBar) == false) {
				View.Selection.SelectionChanged += ViewSelectionChanged;
			}
			View.Closed += ViewClosed;
			ToolBar = new ToolBar {
				BorderThickness = new Thickness(1),
				BorderBrush = Brushes.Gray,
				Band = 1,
				IsOverflowOpen = false
			}.HideOverflow();
			ToolBar.SetResourceReference(Control.BackgroundProperty, VsBrushes.CommandBarGradientBeginKey);
			ToolBar2 = new ToolBar {
				BorderThickness = new Thickness(1),
				BorderBrush = Brushes.Gray,
				Band = 2,
				IsOverflowOpen = false
			}.HideOverflow();
			ToolBar2.SetResourceReference(Control.BackgroundProperty, VsBrushes.CommandBarGradientBeginKey);
			_ToolBarTray = new ToolBarTray {
				ToolBars = { ToolBar, ToolBar2 },
				IsLocked = true,
				Cursor = Cursors.Arrow,
				Background = Brushes.Transparent,
			};
			_ToolBarTray.MouseEnter += ToolBarMouseEnter;
			_ToolBarTray.MouseLeave += ToolBarMouseLeave;
			_ToolBarTray.DragEnter += HideToolBar;
			_ToolBarLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, _ToolBarTray, null);
			_ToolBarTray.Visibility = Visibility.Hidden;
		}

		protected ToolBar ToolBar { get; }
		protected ToolBar ToolBar2 { get; }
		protected IWpfTextView View { get; }

		protected void AddCommand(ToolBar toolBar, int imageId, string tooltip, Action<CommandContext> handler) {
			var b = CreateButton(imageId, tooltip);
			b.Click += (s, args) => {
				var ctx = new CommandContext(this, s as Control, args);
				handler(ctx);
				if (ctx.KeepToolBarOnClick == false) {
					HideToolBar(s, args);
				}
			};
			b.MouseRightButtonUp += (s, args) => {
				var ctx = new CommandContext(this, s as Control, args, true);
				handler(ctx);
				if (ctx.KeepToolBarOnClick == false) {
					HideToolBar(s, args);
				}
				args.Handled = true;
			};
			toolBar.Items.Add(b);
		}

		protected virtual async Task AddCommandsAsync(CancellationToken cancellationToken) {
			var readOnly = View.IsCaretInReadOnlyRegion();
			if (readOnly == false) {
				AddCutCommand();
			}
			AddCopyCommand();
			if (readOnly == false) {
				AddPasteCommand();
				AddDuplicateCommand();
				AddDeleteCommand();
				AddSpecialDataFormatCommand();
				//var selection = View.Selection;
				//if (View.Selection.Mode == TextSelectionMode.Stream && View.TextViewLines.GetTextViewLineContainingBufferPosition(selection.Start.Position) != View.TextViewLines.GetTextViewLineContainingBufferPosition(selection.End.Position)) {
				//	AddCommand(ToolBar, KnownImageIds.Join, "Join lines", ctx => {
				//		var span = View.Selection.SelectedSpans[0];
				//		var t = span.GetText();
				//		View.TextBuffer.Replace(span, System.Text.RegularExpressions.Regex.Replace(t, @"[ \t]*\r?\n[ \t]*", " "));
				//	});
				//}
			}
			//if (CodistPackage.DebuggerStatus != DebuggerStatus.Design) {
			//	AddEditorCommand(ToolBar, KnownImageIds.ToolTip, "Edit.QuickInfo", "Show quick info");
			//}
			AddFindAndReplaceCommands();
			//AddEditorCommand(ToolBar, KnownImageIds.FindNext, "Edit.FindNextSelected", "Find next selected text\nRight click: Find previous selected", "Edit.FindPreviousSelected");
			//AddEditorCommand(ToolBar, "Edit.Capitalize", KnownImageIds.ASerif, "Capitalize");
		}

		protected void AddCommands(ToolBar toolBar, int imageId, string tooltip, Func<CommandContext, Task<IEnumerable<CommandItem>>> getItemsHandler) {
			AddCommands(toolBar, imageId, tooltip, null, getItemsHandler);
		}

		protected void AddCommands(ToolBar toolBar, int imageId, string tooltip, Action<CommandContext> leftClickHandler, Func<CommandContext, Task<IEnumerable<CommandItem>>> getItemsHandler) {
			var b = CreateButton(imageId, tooltip);
			if (leftClickHandler != null) {
				b.Click += (s, args) => {
					leftClickHandler(new CommandContext(this, s as Control, args));
				};
			}
			else {
				b.Click += (s, args) => {
					ButtonEventHandler(s as Button, new CommandContext(this, s as Control, args));
				};
			}
			b.MouseRightButtonUp += (s, args) => {
				ButtonEventHandler(s as Button, new CommandContext(this, s as Control, args, true));
				args.Handled = true;
			};
			toolBar.Items.Add(b);

			async void ButtonEventHandler(Button btn, CommandContext ctx) {
				var m = SetupContextMenu(btn);
				if (m.Tag == null || (bool)m.Tag != ctx.RightClick) {
					m.Items.Clear();
					foreach (var item in await getItemsHandler(ctx)) {
						if (ctx.CancellationToken.IsCancellationRequested) {
							return;
						}
						m.Items.Add(new CommandMenuItem(this, item));
					}
					m.Tag = ctx.RightClick;
				}
			}

		}

		protected void AddCommands(ToolBar toolBar, int imageId, string tooltip, Action<CommandContext> leftClickHandler, Func<CommandContext, IEnumerable<CommandItem>> getItemsHandler) {
			var b = CreateButton(imageId, tooltip);
			void ButtonEventHandler(Button btn, CommandContext ctx) {
				var m = SetupContextMenu(btn);
				if (m.Tag == null || (bool)m.Tag != ctx.RightClick) {
					m.Items.Clear();
					foreach (var item in getItemsHandler(ctx)) {
						if (ctx.CancellationToken.IsCancellationRequested) {
							return;
						}
						m.Items.Add(new CommandMenuItem(this, item));
					}
					m.Tag = ctx.RightClick;
				}
			}
			if (leftClickHandler != null) {
				b.Click += (s, args) => {
					leftClickHandler(new CommandContext(this, s as Control, args));
				};
			}
			else {
				b.Click += (s, args) => {
					ButtonEventHandler(s as Button, new CommandContext(this, s as Control, args));
				};
			}
			b.MouseRightButtonUp += (s, args) => {
				ButtonEventHandler(s as Button, new CommandContext(this, s as Control, args, true));
				args.Handled = true;
			};
			toolBar.Items.Add(b);
		}

		static ContextMenu SetupContextMenu(Button btn) {
			var m = new ContextMenu {
				Resources = SharedDictionaryManager.ContextMenu,
				Foreground = ThemeHelper.ToolWindowTextBrush,
				IsEnabled = true,
				PlacementTarget = btn,
				Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
				IsOpen = true
			};
			ImageThemingUtilities.SetImageBackgroundColor(m, ThemeHelper.TitleBackgroundColor);
			return m;
		}

		protected void AddEditorCommand(ToolBar toolBar, int imageId, string command, string tooltip) {
			ThreadHelper.ThrowIfNotOnUIThread();
			if (CodistPackage.DTE.Commands.Item(command).IsAvailable) {
				AddCommand(toolBar, imageId, tooltip, (ctx) => {
					TextEditorHelper.ExecuteEditorCommand(command);
					//View.Selection.Clear();
				});
			}
		}

		protected void AddEditorCommand(ToolBar toolBar, int imageId, string command, string tooltip, string command2) {
			ThreadHelper.ThrowIfNotOnUIThread();
			if (CodistPackage.DTE.Commands.Item(command).IsAvailable) {
				AddCommand(toolBar, imageId, tooltip, (ctx) => {
					TextEditorHelper.ExecuteEditorCommand(ctx.RightClick ? command2 : command);
					//View.Selection.Clear();
				});
			}
		}

		static Button CreateButton(int imageId, string tooltip) {
			var b = new Button {
				Content = ThemeHelper.GetImage(imageId, Config.Instance.SmartBarButtonSize),
				ToolTip = tooltip,
				Cursor = Cursors.Hand
			};
			ImageThemingUtilities.SetImageBackgroundColor(b, ThemeHelper.TitleBackgroundColor);
			return b;
		}

		async Task CreateToolBarAsync(CancellationToken cancellationToken) {
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
			while ((Mouse.LeftButton == MouseButtonState.Pressed || Keyboard.Modifiers != ModifierKeys.None)
				&& cancellationToken.IsCancellationRequested == false) {
				// postpone the even handler until the mouse button is released
				await Task.Delay(100);
			}
			if (View.Selection.IsEmpty || Interlocked.Exchange(ref _TimerStatus, Working) != Selecting) {
				goto EXIT;
			}
			await InternalCreateToolBarAsync(cancellationToken);
			EXIT:
			_TimerStatus = 0;
		}

		async Task InternalCreateToolBarAsync(CancellationToken cancellationToken = default) {
			_ToolBarTray.Visibility = Visibility.Hidden;
			ToolBar.Items.Clear();
			ToolBar2.Items.Clear();
			await AddCommandsAsync(cancellationToken);
			SetToolBarPosition();
			if (ToolBar2.Items.Count == 0) {
				ToolBar2.Visibility = Visibility.Collapsed;
			}
			else if (ToolBar2.Visibility == Visibility.Collapsed) {
				ToolBar2.Visibility = Visibility.Visible;
				ToolBar2.HideOverflow();
			}
			_ToolBarTray.Visibility = Visibility.Visible;
			_ToolBarTray.Opacity = 0.3;
			_ToolBarTray.SizeChanged += ToolBarSizeChanged;
			View.VisualElement.MouseMove += ViewMouseMove;
		}

		void HideToolBar() {
			_ToolBarTray.Visibility = Visibility.Hidden;
			View.VisualElement.MouseMove -= ViewMouseMove;
			_LastShiftHit = DateTime.MinValue;
		}

		void HideToolBar(object sender, RoutedEventArgs e) {
			HideToolBar();
		}

		void KeepToolbar() {
			_LastExecute = DateTime.Now;
		}

		void SetToolBarPosition() {
			// keep tool bar position when the selection is restored and the tool bar reappears after executing command
			if (DateTime.Now > _LastExecute.AddSeconds(1)) {
				var pos = Mouse.GetPosition(View.VisualElement);
				var rs = _ToolBarTray.RenderSize;
				var x = pos.X - 35;
				var y = pos.Y - rs.Height - 10;
				Canvas.SetLeft(_ToolBarTray, x < View.ViewportLeft ? View.ViewportLeft
					: x + rs.Width < View.ViewportRight ? x
					: View.ViewportRight - rs.Width);
				Canvas.SetTop(_ToolBarTray, (y < 0 || x < View.ViewportLeft && View.Selection.IsReversed == false ? y + rs.Height + 30 : y) + View.ViewportTop);
			}
		}

		#region Event handlers
		void ConfigUpdated(object sender, ConfigUpdatedEventArgs e) {
			if (e.UpdatedFeature.MatchFlags(Features.SmartBar)) {
				View.VisualElement.PreviewKeyUp -= ViewKeyUpAsync;
				if (Config.Instance.SmartBarOptions.MatchFlags(SmartBarOptions.ShiftToggleDisplay)) {
					View.VisualElement.PreviewKeyUp += ViewKeyUpAsync;
				}
				View.Selection.SelectionChanged -= ViewSelectionChanged;
				if (Config.Instance.SmartBarOptions.MatchFlags(SmartBarOptions.ManualDisplaySmartBar) == false) {
					View.Selection.SelectionChanged += ViewSelectionChanged;
				}
			}
		}

		void ToolBarMouseEnter(object sender, EventArgs e) {
			View.VisualElement.MouseMove -= ViewMouseMove;
			((ToolBarTray)sender).Opacity = 1;
			View.Properties[nameof(SmartBar)] = true;
		}

		void ToolBarMouseLeave(object sender, EventArgs e) {
			View.VisualElement.MouseMove += ViewMouseMove;
			View.Properties.RemoveProperty(nameof(SmartBar));
		}

		void ToolBarSizeChanged(object sender, SizeChangedEventArgs e) {
			SetToolBarPosition();
			_ToolBarTray.SizeChanged -= ToolBarSizeChanged;
		}

		void ViewClosed(object sender, EventArgs e) {
			_ToolBarTray.ToolBars.Clear();
			_ToolBarTray.MouseEnter -= ToolBarMouseEnter;
			_ToolBarTray.MouseLeave -= ToolBarMouseLeave;
			View.Selection.SelectionChanged -= ViewSelectionChanged;
			View.VisualElement.MouseMove -= ViewMouseMove;
			View.VisualElement.PreviewKeyUp -= ViewKeyUpAsync;
			//View.LayoutChanged -= ViewLayoutChanged;
			View.Closed -= ViewClosed;
			Config.Updated -= ConfigUpdated;
		}

		void ViewKeyUpAsync(object sender, KeyEventArgs e) {
			if (e.Key != Key.LeftShift && e.Key != Key.RightShift) {
				_LastShiftHit = DateTime.MinValue;
				return;
			}
			var now = DateTime.Now;
			// ignore the shift hit after shift clicking a SmartBar button
			if ((now - _LastExecute).Ticks < TimeSpan.TicksPerSecond) {
				return;
			}
			e.Handled = true;
			if (_ToolBarTray.Visibility == Visibility.Visible) {
				HideToolBar(this, null);
				return;
			}
			if ((now - _LastShiftHit).Ticks < TimeSpan.TicksPerSecond) {
				CreateToolBar();
			}
			else {
				_LastShiftHit = DateTime.Now;
			}
			async void CreateToolBar() {
				await InternalCreateToolBarAsync(_Cancellation.Token);
			}
		}

		void ViewLayoutChanged(object sender, EventArgs e) {
			HideToolBar(sender, null);
		}

		void ViewMouseMove(object sender, MouseEventArgs e) {
			if (_ToolBarTray.IsVisible == false) {
				return;
			}
			const double SensibleRange = 100;
			var p = e.GetPosition(_ToolBarTray);
			double x = p.X, y = p.Y;
			var s = _ToolBarTray.RenderSize;
			if (x > 0 && x <= s.Width) {
				x = 0;
			}
			else if (x > s.Width) {
				x -= s.Width;
			}
			if (y > 0 && y <= s.Height) {
				y = 0;
			}
			else if (y > s.Height) {
				y -= s.Height;
			}
			var op = Math.Abs(x) + Math.Abs(y);
			if (op > SensibleRange) {
				HideToolBar(this, null);
				return;
			}
			_ToolBarTray.Opacity = (SensibleRange - op) / SensibleRange;
		}

		void ViewSelectionChanged(object sender, EventArgs e) {
			if (View.Selection.IsEmpty) {
				_ToolBarTray.Visibility = Visibility.Hidden;
				View.VisualElement.MouseMove -= ViewMouseMove;
				_Cancellation.Cancel();
				_TimerStatus = 0;
				return;
			}
			if (Interlocked.CompareExchange(ref _TimerStatus, Selecting, 0) != 0) {
				return;
			}
			CreateToolBar();
			async void CreateToolBar (){
				try {
					_Cancellation.Cancel();
					_Cancellation = new CancellationTokenSource();
					await Task.Delay(400);
					if (_Cancellation.IsCancellationRequested == false) {
						await CreateToolBarAsync(_Cancellation.Token);
					}
				}
				catch (TaskCanceledException) {
					// ignore
				}
			}
		}
		#endregion

		protected sealed class CommandContext
		{
			readonly SmartBar _Bar;

			public CommandContext(SmartBar bar, Control control, RoutedEventArgs eventArgs) {
				View = bar.View;
				_Bar = bar;
				Sender = control;
				EventArgs = eventArgs;
			}
			public CommandContext(SmartBar bar, Control control, RoutedEventArgs eventArgs, bool rightClick) : this(bar, control, eventArgs) {
				RightClick = rightClick;
			}
			public RoutedEventArgs EventArgs { get; }
			public bool KeepToolBarOnClick { get; set; }
			public bool RightClick { get; }
			public Control Sender { get; }
			public IWpfTextView View { get; }
			public CancellationToken CancellationToken => _Bar._Cancellation.Token;
			public void CancelCommand() {
				_Bar._Cancellation.Cancel();
			}
			public async Task KeepToolBarAsync(bool refresh) {
				_Bar.KeepToolbar();
				KeepToolBarOnClick = true;
				if (refresh) {
					await _Bar.InternalCreateToolBarAsync();
				}
			}
		}

		protected sealed class CommandItem
		{
			public CommandItem(ISymbol symbol, string alias) {
				Name = alias;
				ImageId = symbol.GetImageId();
			}

			public CommandItem(string name, int imageId, Action<MenuItem> controlInitializer, Action<CommandContext> action) {
				Name = name;
				ImageId = imageId;
				ItemInitializer = controlInitializer;
				Action = action;
			}

			public Action<CommandContext> Action { get; }
			public int ImageId { get; }
			public Action<MenuItem> ItemInitializer { get; }
			public string Name { get; }
		}

		protected class CommandMenuItem : MenuItem
		{
			public CommandMenuItem(SmartBar bar, CommandItem item) {
				SmartBar = bar;
				CommandItem = item;
				Icon = ThemeHelper.GetImage(item.ImageId);
				Header = new TextBlock { Text = item.Name };
				item.ItemInitializer?.Invoke(this);
				// the action is installed only when called by this method
				if (item.Action != null) {
					Click += ClickHandler;
				}
				MaxHeight = SmartBar.View.ViewportHeight / 2;
			}

			public CommandItem CommandItem { get; }
			protected SmartBar SmartBar { get; }

			void ClickHandler(object s, RoutedEventArgs e) {
				var ctx2 = new CommandContext(SmartBar, s as Control, e);
				CommandItem.Action(ctx2);
				if (ctx2.KeepToolBarOnClick == false) {
					SmartBar.HideToolBar();
				}
			}
		}
	}
}
