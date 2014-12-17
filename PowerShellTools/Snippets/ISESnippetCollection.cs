using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.Windows.PowerShell.Gui.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Management.Automation;
using System.Xml;
using System.Xml.XPath;
namespace Microsoft.PowerShell.Host.ISE
{
	public class ISESnippetCollection : IEnumerable<ISESnippet>, IEnumerable
	{
		private const string XmlNamespaceForISESnippet = "http://schemas.microsoft.com/PowerShell/Snippets";
		private const string SnippetFileExtension = ".snippets.ps1xml";
		private static Version referenceSchemaVersion = new Version("1.0.0");
		private static List<ISESnippet> userSnippetsBank;
		private static int simulatedTab;
		private static object lockObj = new object();
		private bool isFunctional;
		private Dictionary<string, ISESnippet> lookupTable = new Dictionary<string, ISESnippet>();
		private List<ISESnippet> snippets = new List<ISESnippet>();
		private List<Completion> snippetCompletions = new List<Completion>();
		private List<Completion> nonDefaultSnippetCompletions = new List<Completion>();
		private AuthorizationManager authManager;
		public int Count
		{
			get
			{
				return this.snippets.Count;
			}
		}
		internal bool IsFunctional
		{
			get
			{
				return this.isFunctional;
			}
		}
		internal List<Completion> SnippetCompletions
		{
			get
			{
				return this.snippetCompletions;
			}
		}
		internal List<Completion> NonDefaultSnippetCompletions
		{
			get
			{
				return this.nonDefaultSnippetCompletions;
			}
		}
		public ISESnippet this[int index]
		{
			get
			{
				return this.snippets[index];
			}
		}
		internal ISESnippetCollection(AuthorizationManager authManager)
		{
			this.authManager = authManager;
			this.AddSnippetsSubjectToDupCheck(ISESnippetDefaultSnippets.Snippets, true);
			bool flag = PSGInternalHost.Current.PSGData != null;
			if (ISESnippetCollection.simulatedTab >= 0)
			{
				flag = (ISESnippetCollection.simulatedTab == 0);
			}
			if (!flag)
			{
				if (ISESnippetCollection.userSnippetsBank != null)
				{
					this.UpdateWithDiskSnippets();
					return;
				}
			}
			else
			{
				BackgroundWorker backgroundWorker = new BackgroundWorker();
				backgroundWorker.DoWork += delegate(object param0, DoWorkEventArgs param1)
				{
					ISESnippetCollection.userSnippetsBank = this.LoadDiskSnippets(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "WindowsPowerShell\\Snippets\\"), true);
				};
				backgroundWorker.RunWorkerCompleted += delegate(object param0, RunWorkerCompletedEventArgs param1)
				{
					if (PSGInternalHost.Current.PSGData == null)
					{
						return;
					}
					List<PowerShellTab> list = new List<PowerShellTab>(PSGInternalHost.Current.PowerShellTabs);
					foreach (PowerShellTab current in list)
					{
						current.Snippets.UpdateWithDiskSnippets();
					}
				};
				backgroundWorker.RunWorkerAsync();
			}
		}
		public void Load(string filePathName)
		{
			if (!File.Exists(filePathName))
			{
				throw new InvalidOperationException(Strings.Format(GuiStrings.SnippetLoadFileMissing, new object[]
				{
					filePathName
				}));
			}
			List<ISESnippet> newSnippets = this.LoadOneDiskSnippetXmlFile(filePathName);
			this.AddSnippetsSubjectToDupCheck(newSnippets, false);
		}
		public IEnumerator<ISESnippet> GetEnumerator()
		{
			return this.snippets.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.snippets.GetEnumerator();
		}
		internal static void InternalAccessToSimulateTab(bool isFirst)
		{
			ISESnippetCollection.simulatedTab = (isFirst ? 0 : 1);
		}
		internal static int InternalAccessToParseAddToList(string filename, XPathDocument docNav, List<ISESnippet> loadDestination)
		{
			return ISESnippetCollection.ParseAddToList(filename, docNav, loadDestination);
		}
		internal void UpdateWithDiskSnippets()
		{
			this.AddSnippetsSubjectToDupCheck(ISESnippetCollection.userSnippetsBank, false);
		}
		internal bool Contains(ISESnippet s)
		{
			return this.snippets.Contains(s);
		}
		private static int ParseAddToList(string filename, XPathDocument docNav, List<ISESnippet> loadDestination)
		{
			return ISESnippetCollection.ParseAddToList(filename, docNav, loadDestination, false);
		}
		private static int ParseAddToList(string filename, XPathDocument docNav, List<ISESnippet> loadDestination, bool shouldThrow)
		{
			int num = 0;
			XPathNavigator xPathNavigator = docNav.CreateNavigator();
			XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xPathNavigator.NameTable);
			xmlNamespaceManager.AddNamespace("defns", "http://schemas.microsoft.com/PowerShell/Snippets");
			XPathExpression xPathExpression = xPathNavigator.Compile("/defns:Snippets/defns:Snippet");
			xPathExpression.SetContext(xmlNamespaceManager);
			XPathNodeIterator xPathNodeIterator = xPathNavigator.Select(xPathExpression);
			IL_44E:
			while (xPathNodeIterator.MoveNext())
			{
				string attribute = xPathNodeIterator.Current.GetAttribute("Version", string.Empty);
				if (attribute == null || attribute.Length == 0)
				{
					if (shouldThrow)
					{
						throw new InvalidOperationException(Strings.Format(GuiStrings.SnippetsMissingOrInvalidNodeUnderParentNode, new object[]
						{
							"Version",
							"<Snippet>"
						}));
					}
				}
				else
				{
					Version version = new Version(attribute);
					if (!(version > ISESnippetCollection.referenceSchemaVersion))
					{
						if (!xPathNodeIterator.Current.MoveToChild("Header", "http://schemas.microsoft.com/PowerShell/Snippets") || !xPathNodeIterator.Current.MoveToFirstChild())
						{
							if (shouldThrow)
							{
								throw new InvalidOperationException(Strings.Format(GuiStrings.SnippetsMissingNodeUnderParentNode, new object[]
								{
									"<Header>",
									"<ISESnippet>"
								}));
							}
						}
						else
						{
							string text = null;
							string text2 = null;
							string text3 = null;
							do
							{
								if (string.Compare(xPathNodeIterator.Current.LocalName, "Title", StringComparison.Ordinal) == 0 && string.Compare(xPathNodeIterator.Current.NamespaceURI, "http://schemas.microsoft.com/PowerShell/Snippets", StringComparison.Ordinal) == 0)
								{
									text = xPathNodeIterator.Current.Value;
								}
								if (string.Compare(xPathNodeIterator.Current.LocalName, "Description", StringComparison.Ordinal) == 0 && string.Compare(xPathNodeIterator.Current.NamespaceURI, "http://schemas.microsoft.com/PowerShell/Snippets", StringComparison.Ordinal) == 0)
								{
									text2 = xPathNodeIterator.Current.Value;
								}
								if (string.Compare(xPathNodeIterator.Current.LocalName, "Author", StringComparison.Ordinal) == 0 && string.Compare(xPathNodeIterator.Current.NamespaceURI, "http://schemas.microsoft.com/PowerShell/Snippets", StringComparison.Ordinal) == 0)
								{
									text3 = xPathNodeIterator.Current.Value;
								}
							}
							while (xPathNodeIterator.Current.MoveToNext(XPathNodeType.Element));
							if (text == null || text.Trim().Length == 0)
							{
								if (shouldThrow)
								{
									throw new InvalidOperationException(Strings.Format(GuiStrings.SnippetsMissingOrInvalidNodeUnderParentNode, new object[]
									{
										"<Title>",
										"<Header>"
									}));
								}
							}
							else
							{
								if (text3 == null)
								{
									if (shouldThrow)
									{
										throw new InvalidOperationException(Strings.Format(GuiStrings.SnippetsMissingOrInvalidNodeUnderParentNode, new object[]
										{
											"<Author>",
											"<Header>"
										}));
									}
								}
								else
								{
									if (text2 == null)
									{
										if (shouldThrow)
										{
											throw new InvalidOperationException(Strings.Format(GuiStrings.SnippetsMissingOrInvalidNodeUnderParentNode, new object[]
											{
												"<Description>",
												"<Header>"
											}));
										}
									}
									else
									{
										if (xPathNodeIterator.Current.MoveToParent() && xPathNodeIterator.Current.MoveToNext("Code", "http://schemas.microsoft.com/PowerShell/Snippets") && xPathNodeIterator.Current.MoveToFirstChild())
										{
											string text4 = null;
											int num2 = -1;
											while (string.Compare(xPathNodeIterator.Current.LocalName, "Script", StringComparison.Ordinal) != 0 || string.Compare(xPathNodeIterator.Current.NamespaceURI, "http://schemas.microsoft.com/PowerShell/Snippets", StringComparison.Ordinal) != 0 || string.Compare(xPathNodeIterator.Current.GetAttribute("Language", string.Empty), "PowerShell", StringComparison.OrdinalIgnoreCase) != 0)
											{
												if (!xPathNodeIterator.Current.MoveToNext(XPathNodeType.Element))
												{
													IL_37A:
													if (text4 != null)
													{
														string attribute2 = xPathNodeIterator.Current.GetAttribute("CaretOffset", string.Empty);
														if (attribute2.Trim().Length > 0)
														{
															bool flag = int.TryParse(attribute2, out num2);
															if (!flag || num2 > text4.Length || num2 < 0)
															{
																num2 = -1;
															}
														}
														string attribute3 = xPathNodeIterator.Current.GetAttribute("Indent", string.Empty);
														bool flag2 = string.Compare("false", attribute3.Trim(), StringComparison.OrdinalIgnoreCase) == 0;
														ISESnippet item = new ISESnippet(text, version, text2, text3, text4, filename, true, num2, !flag2);
														loadDestination.Add(item);
														num++;
														goto IL_44E;
													}
													if (shouldThrow)
													{
														throw new InvalidOperationException(Strings.Format(GuiStrings.SnippetsMissingNodeUnderParentNode, new object[]
														{
															"<Script>",
															"<Code>"
														}));
													}
													goto IL_44E;
												}
											}
											text4 = xPathNodeIterator.Current.Value;
											goto IL_37A;
										}
										if (shouldThrow)
										{
											throw new InvalidOperationException(Strings.Format(GuiStrings.SnippetsMissingNodeUnderParentNode, new object[]
											{
												"<Code>",
												"<Header>"
											}));
										}
									}
								}
							}
						}
					}
				}
			}
			return num;
		}
		private int AddSnippetsSubjectToDupCheck(IEnumerable<ISESnippet> newSnippets, bool isDefaultSnippet)
		{
			int num = 0;
			lock (ISESnippetCollection.lockObj)
			{
				this.isFunctional = false;
				foreach (ISESnippet current in newSnippets)
				{
					if (!this.lookupTable.ContainsKey(current.HashCode))
					{
						this.lookupTable.Add(current.HashCode, current);
						this.snippets.Add(current);
						this.snippetCompletions.Add(current.Completion);
						if (!isDefaultSnippet)
						{
							this.nonDefaultSnippetCompletions.Add(current.Completion);
						}
						num++;
					}
				}
				this.isFunctional = true;
			}
			return num;
		}
		private List<ISESnippet> LoadDiskSnippets(string folderPath, bool recurseSubFolders)
		{
			List<ISESnippet> list = new List<ISESnippet>();
			string[] array = null;
			try
			{
				array = Directory.GetFiles(folderPath, "*.snippets.ps1xml", recurseSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
			}
			catch (ArgumentException)
			{
				List<ISESnippet> result = list;
				return result;
			}
			catch (IOException)
			{
				List<ISESnippet> result = list;
				return result;
			}
			catch (UnauthorizedAccessException)
			{
				List<ISESnippet> result = list;
				return result;
			}
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				string text = array2[i];
				string text2 = WPFHelper.ReadIfRunnable(text, this.authManager);
				if (text2 != null)
				{
					StringReader textReader = new StringReader(text2);
					try
					{
						XPathDocument docNav = new XPathDocument(textReader);
						ISESnippetCollection.ParseAddToList(text, docNav, list);
					}
					catch (XmlException)
					{
					}
					catch (ArgumentException)
					{
					}
				}
			}
			return list;
		}
		private List<ISESnippet> LoadOneDiskSnippetXmlFile(string fullFilePathName)
		{
			if (!fullFilePathName.EndsWith(".snippets.ps1xml", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException(Strings.Format(GuiStrings.SnippetWrongExtensionFormat, new object[]
				{
					".snippets.ps1xml"
				}));
			}
			List<ISESnippet> result;
			try
			{
				string s = WPFHelper.ReadIfRunnable(fullFilePathName, this.authManager, true);
				StringReader textReader = new StringReader(s);
				XPathDocument docNav = new XPathDocument(textReader);
				List<ISESnippet> list = new List<ISESnippet>();
				ISESnippetCollection.ParseAddToList(fullFilePathName, docNav, list, true);
				result = list;
			}
			catch (XmlException innerException)
			{
				throw new InvalidOperationException(Strings.Format(GuiStrings.SnippetBadXmlFormat, new object[]
				{
					fullFilePathName
				}), innerException);
			}
			catch (InvalidOperationException)
			{
				throw;
			}
			catch (Exception innerException2)
			{
				throw new InvalidOperationException(Strings.Format(GuiStrings.SnippetLoadFileMissing, new object[]
				{
					fullFilePathName
				}), innerException2);
			}
			return result;
		}
	}
}
