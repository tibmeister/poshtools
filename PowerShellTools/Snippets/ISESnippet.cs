using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.Windows.PowerShell.Gui.Internal;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace Microsoft.PowerShell.Host.ISE
{
	[DebuggerDisplay("SnippetName = {fileName}")]
	public class ISESnippet : IEquatable<ISESnippet>
	{
		private static ImageSource imageSource = new BitmapImage(new Uri("/Microsoft.PowerShell.GPowerShell;component/CodeSnippets.ico", UriKind.Relative));
		private string displayTitle;
		private Version schemaVersion;
		private string description;
		private string author;
		private string codeFragment;
		private bool isTabSpecific;
		private bool isDefault;
		private string fullDiskPath;
		private int caretOffsetFromStart;
		private bool indent;
		private string hashValue;
		private Completion snippetCompletion;
		public string DisplayTitle
		{
			get
			{
				return (string)WPFHelper.SendToUIThread((object param0) => this.displayTitle);
			}
		}
		public Version SchemaVersion
		{
			get
			{
				return (Version)WPFHelper.SendToUIThread((object param0) => this.schemaVersion);
			}
		}
		public string Description
		{
			get
			{
				return (string)WPFHelper.SendToUIThread((object param0) => this.description);
			}
		}
		public string Author
		{
			get
			{
				return (string)WPFHelper.SendToUIThread((object param0) => this.author);
			}
		}
		public string CodeFragment
		{
			get
			{
				return (string)WPFHelper.SendToUIThread((object param0) => this.codeFragment);
			}
		}
		public bool IsTabSpecific
		{
			get
			{
				return (bool)WPFHelper.SendToUIThread((object param0) => this.isTabSpecific);
			}
		}
		public bool IsDefault
		{
			get
			{
				return (bool)WPFHelper.SendToUIThread((object param0) => this.isDefault);
			}
		}
		public string FullPath
		{
			get
			{
				return (string)WPFHelper.SendToUIThread((object param0) => this.fullDiskPath);
			}
		}
		internal Completion Completion
		{
			get
			{
				return this.snippetCompletion;
			}
		}
		internal int CaretOffsetFromStart
		{
			get
			{
				return this.caretOffsetFromStart;
			}
		}
		internal bool Indent
		{
			get
			{
				return this.indent;
			}
		}
		internal string HashCode
		{
			get
			{
				return this.hashValue;
			}
		}
		internal ISESnippet(string displayTitle, Version schemaVersion, string description, string author, string codeFragment, int caretPosition)
		{
			this.CreateSnippet(displayTitle, schemaVersion, description, author, codeFragment, null, true, false, caretPosition, true);
		}
		internal ISESnippet(string displayTitle, Version schemaVersion, string description, string author, string codeFragment, string fullPath, bool isTabSpecific, int caretPosition, bool mustIndent)
		{
			this.CreateSnippet(displayTitle, schemaVersion, description, author, codeFragment, fullPath, false, isTabSpecific, caretPosition, mustIndent);
		}
		public bool Equals(ISESnippet other)
		{
			return other != null && (string.Compare(this.hashValue, other.hashValue, StringComparison.Ordinal) == 0 && this.caretOffsetFromStart == other.caretOffsetFromStart && string.Compare(this.displayTitle, other.displayTitle, StringComparison.Ordinal) == 0 && this.schemaVersion.Equals(other.schemaVersion) && string.Compare(this.author, other.author, StringComparison.Ordinal) == 0 && string.Compare(this.codeFragment, other.codeFragment, StringComparison.Ordinal) == 0) && this.indent == other.indent;
		}
		private void CreateSnippet(string pdisplayTitle, Version pschemaVersion, string pdescription, string pauthor, string pcodeFragment, string pfullPath, bool pisDefault, bool pisTabSpec, int pcaretOffset, bool pindent)
		{
			this.displayTitle = pdisplayTitle;
			this.schemaVersion = pschemaVersion;
			this.description = pdescription;
			this.author = pauthor;
			this.codeFragment = pcodeFragment;
			this.isTabSpecific = pisTabSpec;
			this.isDefault = pisDefault;
			this.fullDiskPath = pfullPath;
			this.caretOffsetFromStart = pcaretOffset;
			this.indent = pindent;
			string text = this.isDefault ? Strings.SnippetToolTipDefault : this.fullDiskPath;
			string text2 = string.Concat(new string[]
			{
				Strings.Format(Strings.SnippetToolTipDescription, new object[]
				{
					this.description
				}),
				"\n",
				Strings.Format(Strings.SnippetToolTipPath, new object[]
				{
					text
				}),
				"\n\n",
				this.codeFragment
			});
			this.snippetCompletion = new Completion(this.displayTitle, this.codeFragment, text2, ISESnippet.imageSource, null);
			this.snippetCompletion.Properties.AddProperty("SnippetInfo", this);
			this.hashValue = this.ComputeHash();
		}
		private string ComputeHash()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(this.displayTitle);
			stringBuilder.Append("\0");
			stringBuilder.Append(this.schemaVersion);
			stringBuilder.Append("\0");
			stringBuilder.Append(this.author);
			stringBuilder.Append("\0");
			stringBuilder.Append(this.codeFragment);
			stringBuilder.Append("\0");
			stringBuilder.Append(this.caretOffsetFromStart);
			stringBuilder.Append("\0");
			stringBuilder.Append(this.indent);
			byte[] bytes = new SHA256CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
			return new string(Encoding.UTF8.GetChars(bytes));
		}
	}
}
