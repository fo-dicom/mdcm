using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using System.Windows.Forms;
using System.Drawing;

using NLog;
using NLog.Config;
using NLog.Targets;

namespace Dicom.Forms {
	[Target("DicomRichTextBox")]
	public sealed class DicomRichTextBoxTarget : TargetWithLayout {
		private int _maxLength = 1048576;
		private RichTextBox _control;
		private bool _useDefaultRowColoringRules = false;
		private RichTextBoxRowColoringRuleCollection _richTextBoxRowColoringRules = new RichTextBoxRowColoringRuleCollection();
		private RichTextBoxWordColoringRuleCollection _richTextBoxWordColoringRules = new RichTextBoxWordColoringRuleCollection();
		private static RichTextBoxRowColoringRuleCollection _defaultRichTextBoxRowColoringRules = new RichTextBoxRowColoringRuleCollection();

		static DicomRichTextBoxTarget() {
			_defaultRichTextBoxRowColoringRules.Add(new RichTextBoxRowColoringRule("level == LogLevel.Fatal", "Red", "Empty"));
			_defaultRichTextBoxRowColoringRules.Add(new RichTextBoxRowColoringRule("level == LogLevel.Error", "Yellow", "Empty"));
			_defaultRichTextBoxRowColoringRules.Add(new RichTextBoxRowColoringRule("level == LogLevel.Warn", "Orange", "Empty"));
			_defaultRichTextBoxRowColoringRules.Add(new RichTextBoxRowColoringRule("level == LogLevel.Info", "White", "Empty"));
			_defaultRichTextBoxRowColoringRules.Add(new RichTextBoxRowColoringRule("level == LogLevel.Debug", "Blue", "Empty"));
			_defaultRichTextBoxRowColoringRules.Add(new RichTextBoxRowColoringRule("level == LogLevel.Trace", "DarkGray", "Empty"));
		}

		public RichTextBox Control {
			get { return _control; }
			set { _control = value; }
		}

		[System.ComponentModel.DefaultValue(false)]
		public bool UseDefaultRowColoringRules {
			get { return _useDefaultRowColoringRules; }
			set { _useDefaultRowColoringRules = value; }
		}

		[ArrayParameter(typeof(RichTextBoxRowColoringRule), "row-coloring")]
		public RichTextBoxRowColoringRuleCollection RowColoringRules {
			get { return _richTextBoxRowColoringRules; }
		}

		[ArrayParameter(typeof(RichTextBoxWordColoringRule), "word-coloring")]
		public RichTextBoxWordColoringRuleCollection WordColoringRules {
			get { return _richTextBoxWordColoringRules; }
		}

		protected override void Write(LogEventInfo logEvent) {
			RichTextBoxRowColoringRule matchingRule = null;

			foreach (RichTextBoxRowColoringRule rr in RowColoringRules) {
				if (rr.CheckCondition(logEvent)) {
					matchingRule = rr;
					break;
				}
			}

			if (UseDefaultRowColoringRules && matchingRule == null) {
				foreach (RichTextBoxRowColoringRule rr in _defaultRichTextBoxRowColoringRules) {
					if (rr.CheckCondition(logEvent)) {
						matchingRule = rr;
						break;
					}
				}
			}

			if (matchingRule == null)
				matchingRule = RichTextBoxRowColoringRule.Default;

			string logMessage = CompiledLayout.GetFormattedMessage(logEvent);

			FindRichTextBoxAndSendTheMessage(logMessage, matchingRule);
		}

		private void FindRichTextBoxAndSendTheMessage(string logMessage, RichTextBoxRowColoringRule rule) {
			if (_control != null)
				_control.Invoke(new DelSendTheMessageToRichTextBox(SendTheMessageToRichTextBox), new object[] { _control, logMessage, rule });
		}

		private delegate void DelSendTheMessageToRichTextBox(RichTextBox rtbx, string logMessage, RichTextBoxRowColoringRule rule);

		private void SendTheMessageToRichTextBox(RichTextBox rtbx, string logMessage, RichTextBoxRowColoringRule rule) {
			int startIndex = rtbx.TextLength;
			rtbx.SelectionStart = startIndex;
			rtbx.SelectionBackColor = GetColorFromString(rule.BackgroundColor, rtbx.BackColor);
			rtbx.SelectionColor = GetColorFromString(rule.FontColor, rtbx.ForeColor);
			rtbx.SelectionFont = new Font(rtbx.SelectionFont, rtbx.SelectionFont.Style ^ rule.Style);
			rtbx.AppendText(logMessage + "\n");
			int newLength = rtbx.TextLength;

			if (newLength > _maxLength) {
				rtbx.ReadOnly = false;
				rtbx.Select(0, newLength - (int)(_maxLength * 0.8));
				rtbx.SelectedText = "";
				rtbx.ReadOnly = true;
			}

			// find word to color
			//foreach (RichTextBoxWordColoringRule wordRule in WordColoringRules)
			//{
			//    MatchCollection mc = wordRule.CompiledRegex.Matches(rtbx.Text, startIndex);
			//    foreach (Match m in mc)
			//    {
			//        rtbx.SelectionStart = m.Index;
			//        rtbx.SelectionLength = m.Length;
			//        rtbx.SelectionBackColor = GetColorFromString(wordRule.BackgroundColor, rtbx.BackColor);
			//        rtbx.SelectionColor = GetColorFromString(wordRule.FontColor, rtbx.ForeColor);
			//        rtbx.SelectionFont = new Font(rtbx.SelectionFont, rtbx.SelectionFont.Style ^ wordRule.Style);
			//    }
			//}

			ScrollTextBoxEnd(rtbx);
		}

		private Color GetColorFromString(string color, Color defaultColor) {
			if (color == "Empty") return defaultColor;

			Color c = Color.FromName(color);
			if (c == Color.Empty) return defaultColor;

			return c;
		}

		private void ScrollTextBoxEnd(RichTextBox tb) {
			const int WM_VSCROLL = 277;
			const int SB_BOTTOM = 7;

			IntPtr ptrWparam = new IntPtr(SB_BOTTOM);
			IntPtr ptrLparam = new IntPtr(0);
			SendMessage(tb.Handle, WM_VSCROLL, ptrWparam, ptrLparam);
		}

		[DllImport("User32.dll", CharSet = CharSet.Auto, EntryPoint = "SendMessage")]
		static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
	}
}
