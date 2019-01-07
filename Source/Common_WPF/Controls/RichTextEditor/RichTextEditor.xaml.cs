using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml;
using SilverlightContrib.Xaml;
using System.ComponentModel;

namespace Common.XAML.Controls
{
    public partial class RichTextEditor : UserControl, INotifyPropertyChanged
    {
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Triggered when the editor is updated (except during view-only mode).
        /// </summary>
        public event Action<RichTextEditor> ContentChanged;

        // ------------------------------------------------------------------------------------------------------------

        Section _ClipBoard;

        // ------------------------------------------------------------------------------------------------------------

        public RichTextEditor()
        {
            InitializeComponent();

            _OnIsViewOnlyChanged(); // (make sure this is in sync with the value)

            rtbEditor.ContentChanged += rtbEditor_ContentChanged;
        }

        string _LastEditorXaml;

        void rtbEditor_ContentChanged(object sender, ContentChangedEventArgs e)
        {
            if (rtbEditor.Xaml != _LastEditorXaml)
                Xaml = rtbEditor.Xaml;
        }

        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Flattens and clears all the section block elements, and returns all paragraph level block elements.
        /// </summary>
        List<Block> _FlattenSections(Block block)
        {
            var blocks = new List<Block>();

            if (block is Section)
            {
                var section = (Section)block;

                foreach (var childBlock in section.Blocks)
                {
                    if (childBlock is Section)
                        blocks.AddArray(_FlattenSections(childBlock).ToArray());
                    else
                        blocks.Add(childBlock);
                }

                section.Blocks.Clear();
            }
            else
                blocks.Add(block);

            return blocks;
        }

        Section _GetSelectionElements(TextSelection selection, out Inline[] inlines)
        {
            inlines = null;
            Section section = null;

            if (selection != null && !string.IsNullOrWhiteSpace(selection.Xaml))
            {
                section = (Section)XamlReader.Load(selection.Xaml);

                if (section != null)
                {
                    var paragraphs = _FlattenSections(section); // (note: 'section.Blocks' is cleared after this call)

                    if (paragraphs.Count > 0)
                    {
                        section.Blocks.AddArray(paragraphs.ToArray());

                        inlines = (from i in paragraphs.Cast<Paragraph>().SelectMany(p => p.Inlines)
                                   select i).ToArray();
                    }
                }
            }

            if (section == null)
                section = (Section)XamlReader.Load("<Section xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' />");

            if (inlines == null)
                inlines = new Inline[0];

            return section;
        }

        // ------------------------------------------------------------------------------------------------------------

        T _CloneBaseProperties<T>(T destination, TextElement source) where T : TextElement
        {
            destination.CharacterSpacing = source.CharacterSpacing;
            destination.FontFamily = source.FontFamily;
            destination.FontSize = source.FontSize;
            destination.FontStretch = source.FontStretch;
            destination.FontStyle = source.FontStyle;
            destination.FontWeight = source.FontWeight;
            destination.Foreground = source.Foreground;

            if (destination is Inline && source is Inline)
                ((Inline)(object)destination).TextDecorations = ((Inline)(object)source).TextDecorations;
            else if (destination is Block && source is Block)
            {
                ((Block)(object)destination).LineHeight = ((Block)(object)source).LineHeight;
                ((Block)(object)destination).LineStackingStrategy = ((Block)(object)source).LineStackingStrategy;
                ((Block)(object)destination).TextAlignment = ((Block)(object)source).TextAlignment;
            }

            return destination;
        }

        T _CloneTextElement<T>(T element) where T : TextElement
        {
            if (element is Section)
            {
                var section = element as Section;
                var newSection = _CloneBaseProperties((Section)XamlReader.Load("<Section xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' />"), section);
                newSection.Blocks.AddArray((from b in section.Blocks select (Block)_CloneTextElement(b)).ToArray());
                return newSection as T;
            }
            else if (element is Paragraph)
            {
                var paragraph = element as Paragraph;
                var newParagraph = _CloneBaseProperties(new Paragraph(), paragraph);
                newParagraph.Inlines.AddArray((from i in paragraph.Inlines select (Inline)_CloneTextElement(i)).ToArray());
                return newParagraph as T;
            }
            else if (element is Run)
            {
                var run = element as Run;
                var newRun = _CloneBaseProperties(new Run(), run);
                newRun.Text = run.Text;
                return newRun as T;
            }
            else if (element is Span)
            {
                var span = element as Span;
                var newSpan = _CloneBaseProperties(new Span(), span);
                newSpan.Inlines.AddArray((from i in span.Inlines select (Inline)_CloneTextElement(i)).ToArray());
                return newSpan as T;
            }
            else if (element is InlineUIContainer)
            {
                var inlineUIContainer = element as InlineUIContainer;
                var newInlineUIContainer = _CloneBaseProperties(new InlineUIContainer(), inlineUIContainer);
                newInlineUIContainer.Child = null; // (note: cloning UIElements is not supported)
                return newInlineUIContainer as T;
            }
            else if (element is LineBreak)
            {
                var lineBreak = element as LineBreak;
                var newLineBreak = _CloneBaseProperties(new LineBreak(), lineBreak);
                return newLineBreak as T;
            }

            return element as T;
        }

        // ------------------------------------------------------------------------------------------------------------

        RichTextBox _TempRTB = new RichTextBox();

        void _InsertNewSelection(Section section)
        {
            rtbEditor.Focus();

            // ... merge first paragraph properties and insert all the inline elements ...

            Paragraph paragraph;
            //??Inline inline;

            //??var selStart = rtbEditor.Selection.Start;//.GetPositionAtOffset(0, LogicalDirection.Forward);
            //??var selEnd = rtbEditor.Selection.End;//.GetPositionAtOffset(0, LogicalDirection.Backward);

            _TempRTB.Blocks.Clear();

            if (section.Blocks.Count > 0)
            {
                // ... make sure the first paragraph properties are merged ...

                _CloneBaseProperties((Paragraph)((TextElement)rtbEditor.Selection.Start.Parent).ElementStart.Parent, (Paragraph)section.Blocks[0]);

                // ... transfer the section blocks, get the xaml, and set the selection to the new content ...

                while (section.Blocks.Count > 0)
                {
                    paragraph = (Paragraph)section.Blocks[0];
                    section.Blocks.RemoveAt(0);
                    _TempRTB.Blocks.Add(paragraph);
                }

                rtbEditor.Selection.Xaml = _TempRTB.Xaml;

                //rtbEditor.Selection.Select(selStart, selEnd);
            }
        }

        // ------------------------------------------------------------------------------------------------------------

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            Inline[] inlines;
            _ClipBoard = _GetSelectionElements(rtbEditor.Selection, out inlines);
            Clipboard.SetText(rtbEditor.Selection.Text);
        }

        private void btnCut_Click(object sender, RoutedEventArgs e)
        {
            Inline[] inlines;
            _ClipBoard = _GetSelectionElements(rtbEditor.Selection, out inlines);
            rtbEditor.Selection.Text = "";
            Clipboard.SetText(rtbEditor.Selection.Text);
        }

        private void btnPaste_Click(object sender, RoutedEventArgs e)
        {
            if (_ClipBoard != null)
                _InsertNewSelection(_CloneTextElement(_ClipBoard));
        }

        // ------------------------------------------------------------------------------------------------------------

        private void btnBold_Click(object sender, RoutedEventArgs e)
        {
            Inline[] inlines;
            var section = _GetSelectionElements(rtbEditor.Selection, out inlines);
            FontWeight firstValue = FontWeights.Normal;
            bool firstValueSet = false;
            foreach (var inline in inlines)
            {
                if (!firstValueSet) { firstValue = (inline.FontWeight == FontWeights.Bold) ? FontWeights.Normal : FontWeights.Bold; firstValueSet = true; }
                inline.FontWeight = firstValue;
            }
            _InsertNewSelection(section);
        }

        private void btnItalic_Click(object sender, RoutedEventArgs e)
        {
            Inline[] inlines;
            var section = _GetSelectionElements(rtbEditor.Selection, out inlines);
            FontStyle firstValue = FontStyles.Normal;
            bool firstValueSet = false;
            foreach (var inline in inlines)
            {
                if (!firstValueSet) { firstValue = (inline.FontStyle == FontStyles.Italic) ? FontStyles.Normal : FontStyles.Italic; firstValueSet = true; }
                inline.FontStyle = firstValue;
            }
            _InsertNewSelection(section);
        }

        private void btnUnderline_Click(object sender, RoutedEventArgs e)
        {
            Inline[] inlines;
            var section = _GetSelectionElements(rtbEditor.Selection, out inlines);
            TextDecorationCollection firstValue = null;
            bool firstValueSet = false;
            foreach (var inline in inlines)
            {
                if (!firstValueSet) { firstValue = (inline.TextDecorations == TextDecorations.Underline) ? null : TextDecorations.Underline; firstValueSet = true; }
                inline.TextDecorations = firstValue;
            }
            _InsertNewSelection(section);
        }

        // ------------------------------------------------------------------------------------------------------------

        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            var doAction = false;
            foreach (Paragraph paragraph in rtbEditor.Blocks)
            {
                if (!doAction && paragraph == ((TextElement)rtbEditor.Selection.Start.Parent).ElementStart.Parent)
                    doAction = true;
                if (doAction)
                    paragraph.TextAlignment = TextAlignment.Left;
                if (paragraph == ((TextElement)rtbEditor.Selection.End.Parent).ElementStart.Parent)
                    break;
            }
        }

        private void btnCenter_Click(object sender, RoutedEventArgs e)
        {
            var doAction = false;
            foreach (Paragraph paragraph in rtbEditor.Blocks)
            {
                if (!doAction && paragraph == ((TextElement)rtbEditor.Selection.Start.Parent).ElementStart.Parent)
                    doAction = true;
                if (doAction)
                    paragraph.TextAlignment = TextAlignment.Center;
                if (paragraph == ((TextElement)rtbEditor.Selection.End.Parent).ElementStart.Parent)
                    break;
            }
        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            var doAction = false;
            foreach (Paragraph paragraph in rtbEditor.Blocks)
            {
                if (!doAction && paragraph == ((TextElement)rtbEditor.Selection.Start.Parent).ElementStart.Parent)
                    doAction = true;
                if (doAction)
                    paragraph.TextAlignment = TextAlignment.Right;
                if (paragraph == ((TextElement)rtbEditor.Selection.End.Parent).ElementStart.Parent)
                    break;
            }
        }

        private void btnJustify_Click(object sender, RoutedEventArgs e)
        {
            var doAction = false;
            foreach (Paragraph paragraph in rtbEditor.Blocks)
            {
                if (!doAction && paragraph == ((TextElement)rtbEditor.Selection.Start.Parent).ElementStart.Parent)
                    doAction = true;
                if (doAction)
                    paragraph.TextAlignment = TextAlignment.Justify;
                if (paragraph == ((TextElement)rtbEditor.Selection.End.Parent).ElementStart.Parent)
                    break;
            }
        }

        // ------------------------------------------------------------------------------------------------------------

        private void btnFontColor_Click(object sender, RoutedEventArgs e)
        {
            puFontColorPicker.IsOpen = !puFontColorPicker.IsOpen;
        }

        private void cpFontColorPicker_SelectedColorChanged(object sender, SilverlightContrib.Controls.SelectedColorEventArgs e) //??
        {
            // ...
        }

        private void btnChangeTextColor_Click(object sender, RoutedEventArgs e)
        {
            puFontColorPicker.IsOpen = false;

            Inline[] inlines;
            var section = _GetSelectionElements(rtbEditor.Selection, out inlines);
            foreach (var inline in inlines)
                inline.Foreground = new SolidColorBrush(cpFontColorPicker.SelectedColor);
            _InsertNewSelection(section);
        }

        private void cbFontName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbFontName != null && cbFontName.SelectedValue != null)
            {
                string fontName = ((ComboBoxItem)cbFontName.SelectedValue).Content as string;
                if (!string.IsNullOrWhiteSpace(fontName))
                {
                    Inline[] inlines;
                    var section = _GetSelectionElements(rtbEditor.Selection, out inlines);
                    foreach (var inline in inlines)
                        inline.FontFamily = new FontFamily(fontName);
                    _InsertNewSelection(section);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------

        private void cbFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbFontSize != null && cbFontSize.SelectedValue != null)
            {
                tbFontSize.Text = ((ComboBoxItem)cbFontSize.SelectedValue).Content.ToString();
                _UpdateFontSize();
            }
        }

        private void tbFontSize_LostFocus(object sender, RoutedEventArgs e)
        {
            cbFontSize.SelectedItem = (from item in cbFontSize.Items
                                       where ((ComboBoxItem)item).Content.ToString() == tbFontSize.Text
                                       select item).FirstOrDefault();
        }

        private void tbFontSize_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                tbFontSize.SelectAll();
                tbFontSize_LostFocus(sender, null);
                // ('e.Handled' will be set below...)
            }

            if (e.Key != Key.D0 && e.Key != Key.D1 && e.Key != Key.D2 && e.Key != Key.D3 && e.Key != Key.D4 &&
                e.Key != Key.D5 && e.Key != Key.D6 && e.Key != Key.D7 && e.Key != Key.D8 && e.Key != Key.D9 &&
                e.Key != Key.Decimal)
                e.Handled = true; // (accept numeric values only)
        }

        void _UpdateFontSize()
        {
            double fontSize = 8d;
            if (double.TryParse(tbFontSize.Text, out fontSize))
            {
                Inline[] inlines;
                var section = _GetSelectionElements(rtbEditor.Selection, out inlines);
                foreach (var inline in inlines)
                    inline.FontSize = fontSize;
                _InsertNewSelection(section);
            }
        }

        // ------------------------------------------------------------------------------------------------------------

        void _TransferBlocksToViewer()
        {
            rtbViewer.Blocks.Clear();
            Paragraph paragraph;
            while (rtbEditor.Blocks.Count > 0)
            {
                paragraph = (Paragraph)rtbEditor.Blocks[0];
                rtbEditor.Blocks.RemoveAt(0);
                rtbViewer.Blocks.Add(paragraph);
            }
            _LastEditorXaml = rtbEditor.Xaml; // (block asynchronous 'ContentChanged' event triggered by the RichTextBox)
        }
        void _TransferBlocksToEditor()
        {
            rtbEditor.Blocks.Clear();
            Paragraph paragraph;
            while (rtbViewer.Blocks.Count > 0)
            {
                paragraph = (Paragraph)rtbViewer.Blocks[0];
                rtbViewer.Blocks.RemoveAt(0);
                rtbEditor.Blocks.Add(paragraph);
            }
            _LastEditorXaml = rtbEditor.Xaml; // (block asynchronous 'ContentChanged' event triggered by the RichTextBox)
        }

        void _HideEditor()
        {
            if (rtbEditor.IsVisible())
            {
                // ... transfer over the blocks ...

                _TransferBlocksToViewer();

                // ... show/hide controls ...

                svViewer.Visibility = System.Windows.Visibility.Visible;
                rtbEditor.Visibility = System.Windows.Visibility.Collapsed;
                spToolBar.Visibility = System.Windows.Visibility.Collapsed;

                if (!IsViewOnly) IsViewOnly = true;
            }
        }

        void _ShowEditor()
        {
            if (!rtbEditor.IsVisible())
            {
                // ... transfer over the blocks ...

                _TransferBlocksToEditor();

                // ... show/hide controls ...

                spToolBar.Visibility = System.Windows.Visibility.Visible;
                rtbEditor.Visibility = System.Windows.Visibility.Visible;
                svViewer.Visibility = System.Windows.Visibility.Collapsed;

                if (IsViewOnly) IsViewOnly = false;
            }
        }

        // ------------------------------------------------------------------------------------------------------------

        private void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            // ... if focus is on this user control, then move it to the editor ...
            var element = FocusManager.GetFocusedElement() as FrameworkElement;
            if (element == this && !IsViewOnly)
                rtbEditor.Focus();
        }

        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts specified text into XAML expected by the RichTextBox control.
        /// If the given text is already in the expected XAML format, then it is simply returned as is.
        /// <para>
        /// This method is useful in cases where string lists may have a mix of plain text and XAML.
        /// </para>
        /// </summary>
        public static string ConvertToRTBXAML(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return "<Section xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' />";
            if (plainText.Trim().StartsWith("<Section "))
                return plainText;
            if (plainText.Trim().StartsWith("<Paragraph "))
                return "<Section xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" + plainText + "</Section>";
            return "<Section xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Paragraph><Run>" + (plainText ?? "").Replace("<", "&lt;").Replace(">", "&gt;") + "</Run></Paragraph></Section>";
        }

        /// <summary>
        /// Returns 'true' if the specific text is valid RTB XAML text.
        /// </summary>
        public static bool IsValidRTBXAML(string text)
        {
            if ((text ?? "").Trim().StartsWith("<Section "))
                return true;
            return false;
        }

        // ------------------------------------------------------------------------------------------------------------
    }

    // ################################################################################################################
    // Dependency Properties

    public partial class RichTextEditor
    {
        // ------------------------------------------------------------------------------------------------------------

        public event PropertyChangedEventHandler PropertyChanged;

        void _DoPropertyChanged(string propName)
        {
            if (!string.IsNullOrWhiteSpace(propName) && PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        // ------------------------------------------------------------------------------------------------------------

        public bool IsViewOnly
        {
            get { return (bool)GetValue(IsViewOnlyProperty); }
            set { SetValue(IsViewOnlyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for XAML.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsViewOnlyProperty =
            DependencyProperty.Register("IsViewOnly", typeof(bool), typeof(RichTextEditor), new PropertyMetadata(false, _OnIsViewOnlyPropertyChanged));

        private static void _OnIsViewOnlyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RichTextEditor)d)._OnIsViewOnlyChanged();
        }

        void _OnIsViewOnlyChanged()
        {
            if (IsViewOnly)
                _HideEditor();
            else
                _ShowEditor();

            _DoPropertyChanged("IsViewOnly");
        }

        // ------------------------------------------------------------------------------------------------------------

        public string SelectedText
        {
            get { return IsViewOnly ? rtbViewer.SelectedText : rtbEditor.Selection.Text; }
            set
            {
                if (IsViewOnly)
                    throw new NotSupportedException("Cannot set text while RichTextEditor is in view-only mode.");
                else
                {
                    if (rtbEditor.Selection.Text != value)
                    {
                        rtbEditor.Selection.Text = value;
                        _DoPropertyChanged("SelectedText");
                    }
                }
            }
        }

        public bool IsViewOnlyTextSelectionEnabled
        {
            get { return rtbViewer.IsTextSelectionEnabled; }
            set { if (rtbViewer.IsTextSelectionEnabled != value) { rtbViewer.IsTextSelectionEnabled = value; _DoPropertyChanged("IsViewOnlyTextSelectionEnabled"); } }
        }

        // ------------------------------------------------------------------------------------------------------------

        public string Xaml
        {
            get { return (IsViewOnly) ? (string)GetValue(XamlProperty) : rtbEditor.Xaml; }
            set { SetValue(XamlProperty, ConvertToRTBXAML(value)); }
        }

        // Using a DependencyProperty as the backing store for XAML.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XamlProperty =
            DependencyProperty.Register("Xaml", typeof(string), typeof(RichTextEditor), new PropertyMetadata("", _OnXamlPropertyChanged));

        private static void _OnXamlPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (IsValidRTBXAML((string)e.NewValue))
                ((RichTextEditor)d)._OnXamlChanged();
            else
                ((RichTextEditor)d).Xaml = RichTextEditor.ConvertToRTBXAML((string)e.NewValue);
        }

        void _OnXamlChanged()
        {
            if (_LastEditorXaml != (string)GetValue(XamlProperty))
            {
                if (rtbEditor.Xaml != (string)GetValue(XamlProperty))
                    rtbEditor.Xaml = (string)GetValue(XamlProperty);

                _LastEditorXaml = rtbEditor.Xaml; // (note: this cancels out the content updated event)

                if (ContentChanged != null)
                    ContentChanged(this);

                _DoPropertyChanged("Xaml");

                if (IsViewOnly)
                    _TransferBlocksToViewer();
            }
        }

        // ------------------------------------------------------------------------------------------------------------

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsReadOnly.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(RichTextEditor), new PropertyMetadata(false, _OnIsReadOnlyPropertyChanged));


        private static void _OnIsReadOnlyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RichTextEditor)d)._OnIsReadOnlyChanged();
        }

        void _OnIsReadOnlyChanged()
        {
            rtbEditor.IsReadOnly = IsReadOnly;
            _DoPropertyChanged("IsReadOnly");
        }

        // ------------------------------------------------------------------------------------------------------------
    }
}
