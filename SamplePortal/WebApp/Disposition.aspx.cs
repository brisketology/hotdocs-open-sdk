/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using SamplePortal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls;

public partial class Disposition : System.Web.UI.Page
{
	protected HotDocs.Sdk.Server.WorkSession _session;
	protected string _siteName = Settings.SiteName;

	protected void Page_Load(object sender, EventArgs e)
	{
		_session = SamplePortal.Factory.GetWorkSession(this.Session);

		if (_session == null)
			Response.Redirect("Default.aspx");//If we don't even have a work session open, then the user hasn't chosen anything to be assembled.

		try
		{
			if (!IsPostBack) // first arrival on this page
			{
				//Advance past the interview.

				//TODO: Make sure we're cleaning up the right stuff in the right places.
				Util.SweepTempDirectories(); // first some housekeeping

				// TFS #5532: Set the max length on the title and description fields.
				// These were previously set in the ASPX page, but ASP.NET drops them for multi-line fields when rendering the page.
				// So we set them manually here using values configurable in the web.config.
				txtTitle.Attributes.Add("maxlength", Settings.MaxTitleLength.ToString());
				txtDescription.Attributes.Add("maxlength", Settings.MaxDescriptionLength.ToString());

				//Get the user-modified interview answers from the HTTP request.
				System.IO.StringReader sr = new StringReader(Util.GetInterviewAnswers(Request));

				//Merge (overlay) the user-modified interview answers back into the assembly's answer collection.
				_session.FinishInterview(HotDocs.Sdk.Server.InterviewAnswerSet.GetDecodedInterviewAnswers(sr));

				if (!string.IsNullOrEmpty(_session.AnswerCollection.FilePath))
				{
					using (SamplePortal.Data.Answers answers = new SamplePortal.Data.Answers())
					{
						DataView ansData = answers.SelectFile(Path.GetFileName(_session.AnswerCollection.FilePath));
						if (ansData.Count > 0)
						{
							// populate answer set title and description
							txtTitle.Text = ansData[0]["Title"].ToString();
							txtDescription.Text = ansData[0]["Description"].ToString();
						}
					}
				}

				string switches = _session.CurrentWorkItem.Template.Switches;

				// Note for HotDocs host application developers: an informational log entry could be added 
				// to a log file before (here) and after the call to _session.AssembleDocuments.
				HotDocs.Sdk.Server.Document[] docs = _session.AssembleDocuments(null);
				AssembledDocsCache cache = Util.GetAssembledDocsCache(this.Session);
				foreach (HotDocs.Sdk.Server.Document doc in docs)
					cache.AddDoc(doc);

				try
				{
					//TODO: Update this comment to reflect reality.
					// numberOfAssemblies needs to take into account any assemblies that are going to be added because of 
					// ASSEMBLE instructions in the template just finished. These assemblies are stored in Assembly.PendingAssemblies 
					// until Assemby.Completed = true, at which time they are moved to Session.Assemblies.
					IEnumerable<HotDocs.Sdk.Server.WorkItem> interviewItems = from n in _session.WorkItems where n is HotDocs.Sdk.Server.InterviewWorkItem select n;
					int numberOfInterviews = interviewItems.Count();
					if (numberOfInterviews > 1)
					{
						IEnumerable<HotDocs.Sdk.Server.WorkItem> completeItems = from n in interviewItems where n.IsCompleted select n;
						int completedInterviews = completeItems.Count();
						pnlNextAsm.Visible = true;
						if (completedInterviews < numberOfInterviews)
						{
							lblContinue.Text = String.Format(
								"You have completed {0} of {1} interviews required for this document set. There {2} {3} follow-up interview{4} still to be done. Click here to proceed to the next interview.",
								completedInterviews,
								numberOfInterviews,
								(numberOfInterviews - completedInterviews) == 1 ? "is" : "are",
								(numberOfInterviews - completedInterviews).ToString(),
								(numberOfInterviews - completedInterviews) == 1 ? "" : "s");
						}
						else
						{
							lblContinue.Text = String.Format(
								"You have completed {0} of {1} interviews required for this document set. ",
								completedInterviews,
								numberOfInterviews);
							btnNextInterview.Visible = false;

						}
					}
					else
						pnlNextAsm.Visible = false;
				}
				catch (System.Runtime.InteropServices.COMException ex)
				{
					pnlError.Visible = true;
					//in case they somehow got here without PDF Advantage
					//TODO: Update the error handling here to reflect that PDF Advantage is no longer a separate product.
					if (ex.ErrorCode == -2147467259 && ex.Message.IndexOf("PDF Advantage") >= 0)
					{
						lblError.Text = "The requested document could not be assembled because HotDocs PDF Advantage " +
							"is not installed on this server.";
					}
					else
					{
						lblError.Text = "An error prevented the document from being assembled:<br>" + ex.Message;
					}
				}

				//Initialize Doc Grid
				docGrid.DataSource = cache.ToArray();
				docGrid.DataBind();
				panelDocDownload.Visible = (cache.Count > 0);
			}
		}
		catch (Exception ex)
		{
			// Note for HotDocs host application developers: An error entry should be added to a log file here
			// with information about the current exception, including a stack trace:
			System.Diagnostics.Debug.WriteLine("Page_Load: Exception occurred: " + ex.Message);
			if (Settings.DebugApplication) throw;
			else Response.Redirect("Default.aspx");
		}
	}

	protected void btnHome_Click(object sender, EventArgs e)
	{
		Response.Redirect("Default.aspx");
	}

	protected void btnNextInterview_Click(object sender, EventArgs e)
	{
		Response.Redirect("Interview.aspx");
	}

	protected void btnSave_Click(object sender, EventArgs e)
	{
		//TODO: Use CSS classes rather than explicit colors here.
		//save the answer file
		if (txtTitle.Text.Length > 0)
		{
			Util.SaveAnswers(_session.AnswerCollection, txtTitle.Text, txtDescription.Text);

			lblStatus.Text = "Answer set saved.";
			lblStatus.ForeColor = Color.Black;
		}
		else
		{
			lblStatus.Text = "Error: Please enter an answer set title.";
			lblStatus.ForeColor = Color.Red;
		}
		lblStatus.Visible = true;
	}
	protected void docGrid_ItemCommand(object source, DataGridCommandEventArgs e)
	{
		if (e.CommandName == "Download")
		{
			string docFile = e.Item.Cells[0].Text;
			FileInfo fInfo = new FileInfo(docFile);
			Response.Clear();
			string downloadFilename = Path.GetRandomFileName() + Path.GetExtension(docFile);
			Response.AddHeader("Content-Disposition", "attachment; filename=" + EncodeDownloadFileName(downloadFilename));
			Response.AddHeader("Content-Length", fInfo.Length.ToString());
			//TODO: Use a common implementation of GetMimeType.
			Response.ContentType = Util.GetMimeType(downloadFilename);
			Response.WriteFile(fInfo.FullName);
			Response.End();
		}
	}

	private string EncodeDownloadFileName(string fileName)
	{
		string newFileName = System.Web.HttpUtility.UrlPathEncode(fileName);
		newFileName = newFileName.Replace("%20", " ");
		return newFileName;
	}

	protected void docGrid_ItemDataBound(object sender, DataGridItemEventArgs e)
	{
		if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
		{
			System.Web.UI.WebControls.LinkButton btnTplImg = (System.Web.UI.WebControls.LinkButton)e.Item.Cells[0].FindControl("btnImgTemplate");
			string docFile = e.Item.Cells[0].Text;
			if (docFile == "&nbsp;" || Util.IsEmpty(docFile))
			{
				btnTplImg.Text = "<div class=\"hd-sp-img hd-sp-img-blank\" ></div>";
				return;
			}
			switch (Path.GetExtension(docFile).ToLower())
			{
				case ".pdf":
				case ".hpd":
				case ".hfd":
					// form template
					btnTplImg.Text = "<div class=\"hd-sp-img hd-sp-img-frm\" ></div>";
					break;
				default:
					// text template 
					btnTplImg.Text = "<div class=\"hd-sp-img hd-sp-img-txt\" ></div>";
					break;
			}
		}
	}
	//TODO: Remove.
	private string GetDocumentName(string templateTitle, System.Collections.Hashtable templateCounts)
	{
		string retVal;
		string key = templateTitle.ToLower();
		if (templateCounts.ContainsKey(key))
		{
			templateCounts[key] = ((int)templateCounts[key]) + 1;
			retVal = string.Format("{0} {1}", templateTitle, templateCounts[key].ToString());
		}
		else
		{
			templateCounts.Add(templateTitle.ToLower(), 1);
			retVal = templateTitle;
		}
		return retVal;
	}

	//TODO: Remove.
	private string MakeDocFilename(string baseName, string docName)
	{
		string result = Util.EnsureValidFilename(baseName);
		if (txtTitle.Text.Length > 0)
			result += " - " + Util.EnsureValidFilename(txtTitle.Text);
		result += Path.GetExtension(docName);
		return result;
	}
}