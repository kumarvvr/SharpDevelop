﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using ICSharpCode.RubyBinding;
using ICSharpCode.Scripting.Tests.Utils;
using NUnit.Framework;
using RubyBinding.Tests.Utils;

namespace RubyBinding.Tests.Designer
{
	[TestFixture]
	public class LoadSimpleUserControlTestFixture
	{	
		MockComponentCreator componentCreator = new MockComponentCreator();
		UserControl userControl;
		
		public string RubyCode {
			get {
				return "class MainForm < System::Windows::Forms::UserControl\r\n" +
							"    def InitializeComponent()\r\n" +
							"        self.SuspendLayout()\r\n" +
							"        # \r\n" +
							"        # userControl1\r\n" +
							"        # \r\n" +
							"        self.ClientSize = System::Drawing::Size.new(300, 400)\r\n" +
							"        self.Name = \"userControl1\"\r\n" +
							"        self.ResumeLayout(false)\r\n" +
							"    end\r\n" +
							"end";
			}
		}

		[TestFixtureSetUp]
		public void SetUpFixture()
		{			
			RubyComponentWalker walker = new RubyComponentWalker(componentCreator);
			userControl = walker.CreateComponent(RubyCode) as UserControl;
		}

		[TestFixtureTearDown]
		public void TearDownFixture()
		{
			userControl.Dispose();
		}
		
		[Test]
		public void UserControlCreated()
		{			
			Assert.IsNotNull(userControl);
		}
		
		[Test]
		public void UserControlName()
		{
			Assert.AreEqual("userControl1", userControl.Name);
		}
		
		[Test]
		public void UserControlClientSize()
		{
			Size size = new Size(300, 400);
			Assert.AreEqual(size, userControl.ClientSize);
		}
	}
}
