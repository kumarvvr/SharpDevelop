// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Windows.Forms;
using System.Drawing;
using System.CodeDom.Compiler;
using System.Collections;
using System.IO;
using System.Diagnostics;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Project;

namespace ICSharpCode.SharpDevelop.Gui
{
	public class ShowOutputFromComboBox : AbstractComboBoxCommand
	{
		ComboBox comboBox;
		
		protected override void OnOwnerChanged(EventArgs e) 
		{
			base.OnOwnerChanged(e);
			ToolBarComboBox toolbarItem = (ToolBarComboBox)Owner;
			comboBox = toolbarItem.ComboBox;
			SetItems();
			CompilerMessageView.Instance.MessageCategoryAdded         += new EventHandler(CompilerMessageViewMessageCategoryAdded);
			CompilerMessageView.Instance.SelectedCategoryIndexChanged += new EventHandler(CompilerMessageViewSelectedCategoryIndexChanged);
			comboBox.SelectedIndex = 0;
			comboBox.SelectedIndexChanged += new EventHandler(ComboBoxSelectedIndexChanged);
		}
		void CompilerMessageViewSelectedCategoryIndexChanged(object sender, EventArgs e)
		{
			if (comboBox.SelectedIndex != CompilerMessageView.Instance.SelectedCategoryIndex) {
				comboBox.SelectedIndex = CompilerMessageView.Instance.SelectedCategoryIndex;
			}
		}
		void ComboBoxSelectedIndexChanged(object sender, EventArgs e)
		{
			if (comboBox.SelectedIndex != CompilerMessageView.Instance.SelectedCategoryIndex) {
				CompilerMessageView.Instance.SelectedCategoryIndex = comboBox.SelectedIndex;
			}
		}
		void CompilerMessageViewMessageCategoryAdded(object sender, EventArgs e)
		{
			SetItems();
		}
		
		void SetItems()
		{
			comboBox.Items.Clear();
			foreach (MessageViewCategory category in CompilerMessageView.Instance.MessageCategories) {
				comboBox.Items.Add(StringParser.Parse(category.DisplayCategory));
			}
		}
		
		public override void Run()
		{
		}
	}
	
	public class ClearOutputWindow : AbstractCommand
	{
		public override void Run()
		{
			MessageViewCategory selectedMessageViewCategory = CompilerMessageView.Instance.SelectedMessageViewCategory;
			if (selectedMessageViewCategory != null) {
				selectedMessageViewCategory.ClearText();
			}
		}
	}
	
	public class ToggleeMessageViewWordWrap : AbstractCheckableMenuCommand
	{
		ToolBarCheckBox checkBox;
		
		public override bool IsChecked {
			get {
				return CompilerMessageView.Instance.WordWrap;
			}
			set {
				CompilerMessageView.Instance.WordWrap = value;
			}
		}
		
		public override object Owner {
			set {
				base.Owner = value;
				checkBox = (ToolBarCheckBox)Owner;
			}
		}
		
			
		public override void Run()
		{
			IsChecked = !IsChecked;
		}
	}
	
}
