﻿/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using HotDocs.Sdk.Server;
using HotDocs.Sdk.Server.Cloud;
using HotDocs.Sdk.Server.Contracts;
using System.Reflection;

namespace HotDocs.Sdk.ServerTest
{
	/// <summary>
	/// Summary description for UnitTest1
	/// </summary>
	[TestClass]
	public class UnitTest1
	{
		public UnitTest1()
		{
			HotDocs.Sdk.TemplateLocation.RegisterLocation(typeof(HotDocs.Sdk.PackagePathTemplateLocation));
		}

		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}

		#region Additional test attributes
		//
		// You can use the following additional attributes as you write your tests:
		//
		// Use ClassInitialize to run code before running the first test in the class
		// [ClassInitialize()]
		// public static void MyClassInitialize(TestContext testContext) { }
		//
		// Use ClassCleanup to run code after all tests in a class have run
		// [ClassCleanup()]
		// public static void MyClassCleanup() { }
		//
		// Use TestInitialize to run code before running each test 
		// [TestInitialize()]
		// public void MyTestInitialize() { }
		//
		// Use TestCleanup to run code after each test has run
		// [TestCleanup()]
		// public void MyTestCleanup() { }
		//
		#endregion

		#region IServices Constructor Tests

		[TestMethod]
		public void Services_Constructor_Cloud()
		{
			// Verify that we can construct a new Cloud.Services object or get an appropriate exception if one or both of the parameters are null.

			CloudServiceConstructorTester("subscriberID", "signingKey");
			CloudServiceConstructorTester(null, "signingKey");
			CloudServiceConstructorTester("subscriberID", null);
			CloudServiceConstructorTester(null, null);
		}

		private void CloudServiceConstructorTester(string id, string key)
		{
			try
			{
				IServices services = new HotDocs.Sdk.Server.Cloud.Services(id, key);
				Assert.IsTrue(services is HotDocs.Sdk.Server.Cloud.Services);

				if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(key))
					Assert.Fail(); // We should have had an exception before reaching this.
			}
			catch (ArgumentNullException)
			{
				Assert.IsTrue(string.IsNullOrEmpty(id) || string.IsNullOrEmpty(key));
			}
			catch (Exception)
			{
				Assert.Fail();
			}
		}

		#endregion

		#region GetInterview Tests

		[TestMethod]
		public void GetInterview_Local()
		{
			IServices services = Util.GetLocalServicesInterface();
			Template template = Util.OpenTemplate("d1f7cade-cb74-4457-a9a0-27d94f5c2d5b");
			string logRef = "GetInterview_Local Unit Test";

			GetInterview(services, template, logRef);
		}

		[TestMethod]
		public void GetInterview_WebService()
		{
			IServices services = Util.GetWebServiceServicesInterface();
			Template template = Util.OpenTemplate("d1f7cade-cb74-4457-a9a0-27d94f5c2d5b");
			string logRef = "GetInterview_WebService Unit Test";

			GetInterview(services, template, logRef);
		}

		[TestMethod]
		public void GetInterview_Cloud()
		{
			IServices services = Util.GetCloudServicesInterface();
			Template template = Util.OpenTemplate("d1f7cade-cb74-4457-a9a0-27d94f5c2d5b");
			string logRef = "GetInterview_Cloud Unit Test";

			GetInterview(services, template, logRef);
		}

		private void GetInterview(IServices svc, Template tmp, string logRef)
		{
			// Set up the InterviewOptions for the test.
			string postInterviewUrl = "PostInterview.aspx";
			string styleSheetUrl = "HDServerFiles/Stylesheets";
			string runtimeUrl = "HDServerFiles/js";
			string interviewDefUrl = "GetInterviewDef.ashx";
			string interviewImgUrl = "GetImage.ashx";
			InterviewSettings settings = new InterviewSettings(postInterviewUrl, runtimeUrl, styleSheetUrl, interviewDefUrl, interviewImgUrl);

			// Set up the Marked Variables for the test.
			string[] markedVars = null; // new string[] { };

			InterviewResult result;

			// Make sure that the parameters are being validated correctly.
			try
			{
				svc.GetInterview(null, null, null, null, null);
				Assert.Fail(); // If we get here then the exceptions were not fired as they should have been with all null parameters.
			}
			catch (ArgumentNullException ex)
			{
				Assert.IsTrue(ex.Message.IndexOf(": template") > 0);
			}
			catch (Exception)
			{
				Assert.Fail(); // Not expecting a generic exception.
			}

			result = svc.GetInterview(tmp, null, settings, markedVars, logRef);
			Assert.AreNotEqual(result.HtmlFragment, "");
			Assert.IsTrue(result.HtmlFragment.Contains(settings.PostInterviewUrl));
			Assert.IsTrue(result.HtmlFragment.Contains(runtimeUrl));
			Assert.IsTrue(result.HtmlFragment.Contains(styleSheetUrl));
			Assert.IsTrue(result.HtmlFragment.Contains(interviewDefUrl));
			Assert.IsTrue(result.HtmlFragment.Contains(interviewImgUrl));
			Assert.IsTrue(result.HtmlFragment.Contains("hdMainDiv"));
			Assert.IsFalse(result.HtmlFragment.Contains("Employee Name\": { t: \"TX\", m:true")); // Employee Name should not be "marked"

			// Now get another interview, but this time specify a url for doc preview and save answers.
			settings.DocumentPreviewUrl = "DocPreview.aspx";
			settings.SaveAnswersUrl = "SaveAnswers.aspx";
			result = svc.GetInterview(tmp, null, settings, markedVars, logRef);
			Assert.IsTrue(result.HtmlFragment.Contains(settings.DocumentPreviewUrl));
			Assert.IsTrue(result.HtmlFragment.Contains(settings.SaveAnswersUrl));

			// Now get another interview, but this time do the following:
			// 1. Disable the doc preview and save answers urls.
			// 2. Do not include the hdMainDiv.
			// 3. "Mark" the Employee Name variable.
			settings.DisableDocumentPreview = Tristate.True;
			settings.DisableSaveAnswers = Tristate.True;
			settings.AddHdMainDiv = Tristate.False;
			markedVars = new string[] { "Employee Name" };
			result = svc.GetInterview(tmp, null, settings, markedVars, logRef);
			Assert.IsFalse(result.HtmlFragment.Contains(settings.DocumentPreviewUrl));
			Assert.IsFalse(result.HtmlFragment.Contains(settings.SaveAnswersUrl), "No Save Ans Url because it is disabled");
			Assert.IsTrue(result.HtmlFragment.Contains("Employee Name\": { t: \"TX\", m:true")); // This interview does "mark" Employee Name.

			// Only HotDocs Cloud Services honors the AddHdMainDiv property of InterviewSettings, so only bother checking it if we are running a test against cloud services.
			if (svc is HotDocs.Sdk.Server.Cloud.Services)
				Assert.IsFalse(result.HtmlFragment.Contains("hdMainDiv"));

			// Now try once more with a null value for settings to allow the default settings to be used.
			// Also, in this test we are using an actual answer file so that we can test using answers.
			TextReader answers = Util.GetTestFile("Freddy.xml");
			result = svc.GetInterview(tmp, answers, null, markedVars, logRef);
			Assert.IsTrue(result.HtmlFragment.Contains("Freddy"));

		}

		#endregion

		#region AssembleDocument Tests
		[TestMethod]
		public void AssembleDocument_Local()
		{
			IServices services = Util.GetLocalServicesInterface();
			string logRef = "AssembleDocument_Local Unit Test";
			AssembleDocument(services, logRef);
		}

		[TestMethod]
		public void AssembleDocument_WebService()
		{
			IServices services = Util.GetWebServiceServicesInterface();
			string logRef = "AssembleDocument_WebService Unit Test";
			AssembleDocument(services, logRef);
		}

		[TestMethod]
		public void AssembleDocument_Cloud()
		{
			IServices services = Util.GetCloudServicesInterface();
			string logRef = "AssembleDocument_Cloud Unit Test";
			AssembleDocument(services, logRef);
		}

		private void AssembleDocument(IServices svc, string logRef)
		{
			Template tmp = Util.OpenTemplate("d1f7cade-cb74-4457-a9a0-27d94f5c2d5b");
			TextReader answers = new StringReader("");
			AssembleDocumentSettings settings = new AssembleDocumentSettings();
			AssembleDocumentResult result;

			// Verify that a null template throws the right exception.
			try
			{
				result = svc.AssembleDocument(null, answers, settings, logRef);
				Assert.Fail(); // We should not have reached this point.
			}
			catch (ArgumentNullException ex)
			{
				Assert.IsTrue(ex.Message.Contains("template"));
			}
			catch (Exception)
			{
				Assert.Fail(); // We are not expecting a generic exception.
			}

			// Pass a null for settings and answers to ensure that defaults are used.
			result = svc.AssembleDocument(tmp, null, null, logRef);
			Assert.AreEqual(result.PendingAssemblies.Count<HotDocs.Sdk.Template>(), 0);
			Assert.AreEqual(0, result.Document.SupportingFiles.Count<NamedStream>());
			Assert.AreEqual(0, result.PendingAssemblies.Count<Template>());

			settings.Format = DocumentType.MHTML;
			result = svc.AssembleDocument(tmp, answers, settings, logRef);
			Assert.AreEqual(0, result.Document.SupportingFiles.Count<NamedStream>()); // The MHTML is a single file (no external images).

			settings.Format = DocumentType.HTMLwDataURIs;
			result = svc.AssembleDocument(tmp, answers, settings, logRef);
			Assert.AreEqual(0, result.Document.SupportingFiles.Count<NamedStream>()); // The HTML with Data URIs is a single file (no external images).

			settings.Format = DocumentType.HTML;
			result = svc.AssembleDocument(tmp, answers, settings, logRef);
			Assert.AreEqual(1, result.Document.SupportingFiles.Count<NamedStream>()); // The HTML contains one external image file.

			// Now try with another template, which contains an ASSEMBLE instruction.
			tmp = Util.OpenTemplate("TemplateWithAssemble");
			result = svc.AssembleDocument(tmp, null, null, logRef);
			Assert.AreEqual(1, result.PendingAssemblies.Count<Template>());

		}

		#endregion

		#region GetComponentInfo Tests

		[TestMethod]
		public void GetComponentInfo_Local()
		{
			IServices services = Util.GetLocalServicesInterface();
			Template template = Util.OpenTemplate("d1f7cade-cb74-4457-a9a0-27d94f5c2d5b");
			string logRef = "GetComponentInfo_Local Unit Test";

			GetComponentInfo(services, template, logRef);
		}

		[TestMethod]
		public void GetComponentInfo_WebService()
		{
			IServices services = Util.GetWebServiceServicesInterface();
			Template template = Util.OpenTemplate("d1f7cade-cb74-4457-a9a0-27d94f5c2d5b");
			string logRef = "GetComponentInfo_WebService Unit Test";

			GetComponentInfo(services, template, logRef);
		}

		[TestMethod]
		public void GetComponentInfo_Cloud()
		{
			IServices services = Util.GetCloudServicesInterface();
			Template template = Util.OpenTemplate("d1f7cade-cb74-4457-a9a0-27d94f5c2d5b");
			string logRef = "GetComponentInfo_Local Unit Test";

			GetComponentInfo(services, template, logRef);
		}

		private void GetComponentInfo(IServices svc, Template tmp, string logRef)
		{
			Server.Contracts.ComponentInfo result;

			// Ensure that invalid parameters are throwing appropriate exceptions.
			try
			{
				result = svc.GetComponentInfo(null, false, null);
				Assert.Fail(); // Should not reach here with a null template.
			}
			catch (ArgumentNullException ex)
			{
				Assert.IsTrue(ex.Message.Contains("template"));
			}
			catch (Exception)
			{
				Assert.Fail(); // We are not expecting any generic exceptions.
			}

			result = svc.GetComponentInfo(tmp, false, logRef);
			Assert.IsNull(result.Dialogs); // We did not request dialogs.
			Assert.AreEqual(result.Variables.Count, 20);

			// Now try it again, but request dialogs this time.
			result = svc.GetComponentInfo(tmp, true, logRef);
			Assert.IsNotNull(result.Dialogs); // We did request dialogs this time.
			Assert.AreEqual(result.Dialogs.Count, 4);
			Assert.AreEqual(result.Variables.Count, 20);
		}

		#endregion

		#region GetAnswers Tests

		[TestMethod]
		public void GetAnswers_Local()
		{
			IServices services = Util.GetLocalServicesInterface();
			string logRef = "GetAnswers_Local Unit Test";
			GetAnswers(services, logRef);
		}

		[TestMethod]
		public void GetAnswers_WebService()
		{
			IServices services = Util.GetWebServiceServicesInterface();
			string logRef = "GetAnswers_WebService Unit Test";
			GetAnswers(services, logRef);
		}

		[TestMethod]
		public void GetAnswers_Cloud()
		{
			IServices services = Util.GetCloudServicesInterface();
			string logRef = "GetAnswers_Cloud Unit Test";
			GetAnswers(services, logRef);
		}

		private void GetAnswers(IServices svc, string logRef)
		{
			List<TextReader> answersList = new List<TextReader>();
			string xml;

			// Ensure that invalid parameters are throwing an appropriate exception.
			try
			{
				xml = svc.GetAnswers(null, logRef);
				Assert.Fail(); // Should have failed instead of reaching here.
			}
			catch (ArgumentNullException ex)
			{
				Assert.IsTrue(ex.Message.IndexOf("answers") > 0);
			}

			// Perform all of these tests a number of times using different formats of the answers each time.
			// The .txt files are base64 encoded versions of the matching .xml files.
			for (int i = 0; i < 4; i++)
			{
				string answerFile1 = "Freddy.txt"; ;
				string answerFile2 = "Frederick.txt";
				switch (i)
				{
					case 0:
						answerFile1 = "Freddy.txt";
						answerFile2 = "Frederick.txt";
						break;
					case 1:
						answerFile1 = "Freddy.txt";
						answerFile2 = "Frederick.xml";
						break;
					case 2:
						answerFile1 = "Freddy.xml";
						answerFile2 = "Frederick.txt";
						break;
					case 3:
						answerFile1 = "Freddy.xml";
						answerFile2 = "Frederick.xml";
						break;
				}

				// Get the Freddy answer file.
				answersList.Clear();
				answersList.Add(Util.GetTestFile(answerFile1));
				xml = svc.GetAnswers(answersList.ToArray(), logRef);
				Assert.IsTrue(xml.IndexOf("running the company") > 0);
				Assert.IsTrue(xml.IndexOf("Freddy") > 0);

				// Get the Frederick answer file.
				answersList.Clear();
				answersList.Add(Util.GetTestFile(answerFile2));
				xml = svc.GetAnswers(answersList.ToArray(), logRef);
				Assert.IsTrue(xml.IndexOf("Here is your new car") > 0);
				Assert.IsTrue(xml.IndexOf("Frederick") > 0);

				// Get the combined answer file with Frederick answers overlayed on top of Freddy answers.
				answersList.Clear();
				answersList.Add(Util.GetTestFile(answerFile1));
				answersList.Add(Util.GetTestFile(answerFile2));
				xml = svc.GetAnswers(answersList.ToArray(), logRef);
				Assert.IsTrue(xml.IndexOf("running the company") > 0);
				Assert.IsTrue(xml.IndexOf("Here is your new car") > 0);
				Assert.IsTrue(xml.IndexOf("Frederick") > 0);
				Assert.IsTrue(xml.IndexOf("Freddy") < 0);

				// Get the combined answer file with Freddy answers overlayed on top of Frederick answers.
				answersList.Clear();
				answersList.Add(Util.GetTestFile(answerFile2));
				answersList.Add(Util.GetTestFile(answerFile1));
				xml = svc.GetAnswers(answersList.ToArray(), logRef);
				Assert.IsTrue(xml.IndexOf("running the company") > 0);
				Assert.IsTrue(xml.IndexOf("Here is your new car") > 0);
				Assert.IsTrue(xml.IndexOf("Freddy") > 0);
				Assert.IsTrue(xml.IndexOf("Frederick") < 0);
			}

		}

		#endregion

		#region BuildSupportFiles

		[TestMethod]
		public void BuildSupportFiles_Local() { }

		[TestMethod]
		public void BuildSupportFiles_WebService() { }

		[TestMethod]
		public void BuildSupportFiles_Cloud()
		{
			BuildSupportFiles(Util.GetCloudServicesInterface());
		}

		private void BuildSupportFiles(IServices svc)
		{
			Template template = Util.OpenTemplate("d1f7cade-cb74-4457-a9a0-27d94f5c2d5b");

			try
			{
				svc.BuildSupportFiles(template, HDSupportFilesBuildFlags.BuildJavaScriptFiles);
			}
			catch (Exception ex)
			{
				Assert.Fail(ex.Message);
			}
		}

		#endregion

		#region RemoveSupportFiles

		[TestMethod]
		public void RemoveSupportFiles_Local() { }

		[TestMethod]
		public void RemoveSupportFiles_WebService() { }

		[TestMethod]
		public void RemoveSupportFiles_Cloud()
		{
			RemoveSupportFiles(Util.GetCloudServicesInterface());
		}

		private void RemoveSupportFiles(IServices svc)
		{
			Template tmp = Util.OpenTemplate("d1f7cade-cb74-4457-a9a0-27d94f5c2d5b");
			try
			{
				svc.RemoveSupportFiles(tmp);
			}
			catch (Exception ex)
			{
				Assert.Fail(ex.Message);
			}
		}

		#endregion

		#region GetInterviewDefinition Tests

		[TestMethod]
		public void GetInterviewDefinition_Local() { }

		[TestMethod]
		public void GetInterviewDefinition_WebService() { }

		[TestMethod]
		public void GetInterviewDefinition_Cloud()
		{
			GetInterviewDefinition(Util.GetCloudServicesInterface());
		}

		private void GetInterviewDefinition(IServices svc)
		{
			Template template = Util.OpenTemplate("d1f7cade-cb74-4457-a9a0-27d94f5c2d5b");
			string templateState = null;
			string templateFile = null;
			InterviewFormat fmt = InterviewFormat.Unspecified;

			for (int i = 0; i < 7; i++)
			{
				switch (i)
				{
					case 0:
						templateState = null;
						templateFile = "filename";
						break;
					case 1:
						templateState = "state";
						templateFile = null;
						break;
					case 2:
						templateState = "";
						templateFile = "filename";
						break;
					case 3:
						templateState = "state";
						templateFile = "";
						break;
					default:
						templateState = template.CreateLocator();
						templateFile = "Demo Employment Agreement.docx";
						break;
				}

				if (i == 5)
					fmt = InterviewFormat.JavaScript;

				if (i == 6)
					fmt = InterviewFormat.Silverlight;

				try
				{
					using (Stream definitionFile = svc.GetInterviewDefinition(templateState, templateFile, fmt))
					{

						if (string.IsNullOrEmpty(templateState) || string.IsNullOrEmpty(templateFile))
							Assert.Fail(); // Should have hit an exception instead of reaching this.

						Assert.IsTrue(definitionFile.Length > 0);
					}


				}
				catch (ArgumentNullException ex)
				{
					Assert.IsTrue(ex.Message.Contains(string.IsNullOrEmpty(templateState) ? "state" : "templateFile"));
				}
			}
		}


		#endregion

		#region Template and TemplateLocation Tests
		[TestMethod]
		public void TestPackagePathLocation()
		{
			string packageID = "d1f7cade-cb74-4457-a9a0-27d94f5c2d5b";
			string templateFileName = "Demo Employment Agreement.docx";
			HotDocs.Sdk.PackagePathTemplateLocation location = Util.CreatePackagePathLocation(packageID);

			Assert.IsTrue(File.Exists(location.PackagePath));

			string switches = "/ni";
			string key = "Test file key";
			Template template = new Template(location, switches, key);

			Assert.AreEqual(templateFileName, template.FileName);
			Assert.AreEqual(key, template.Key);
			Assert.AreEqual(switches, template.Switches);

			//TODO: Update the package so that the template title and template type agree.
			Assert.AreEqual("Employment Agreement (Word RTF version)", template.GetTitle().Trim());

			Assert.AreEqual(template.TemplateType, TemplateType.WordDOCX);
			Assert.IsFalse(template.HasInterview);
			Assert.IsTrue(template.GeneratesDocument);
			Assert.AreEqual(template.NativeDocumentType, DocumentType.WordDOCX);
			Assert.AreEqual(template.TemplateType, TemplateType.WordDOCX);

			string filePath = template.GetFullPath();
			Assert.IsTrue(File.Exists(filePath));
			Directory.Delete(Path.GetDirectoryName(filePath));

			//Check the second time since the folder has been deleted.
			filePath = template.GetFullPath();//The folder should come into existence here.
			Assert.IsTrue(File.Exists(filePath));
			Directory.Delete(Path.GetDirectoryName(filePath));//Clean up.

			string locator = template.CreateLocator();
			Template template2 = Template.Locate(locator);

			Assert.AreEqual(template.FileName, template2.FileName);
			Assert.AreEqual(template.Key, template2.Key);
			Assert.AreEqual(template.Switches, template2.Switches);
			Assert.AreEqual(template.GetTitle(), template2.GetTitle());
			Assert.AreEqual(template.GeneratesDocument, template2.GeneratesDocument);
			Assert.AreEqual(template.HasInterview, template2.HasInterview);
			Assert.AreEqual(template.NativeDocumentType, template2.NativeDocumentType);
			Assert.AreEqual(template.TemplateType, template2.TemplateType);

			template.UpdateFileName();
			Assert.AreEqual(template.FileName, template2.FileName);
		}

		[TestMethod]
		public void TestPathTemplateLocation()
		{
		}

		private void TestTemplate(Template template)
		{
			string filePath = template.GetFullPath();
			Assert.IsTrue(File.Exists(filePath));
		}

		#endregion
	}
}
