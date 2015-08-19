using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Tagging;

namespace PowerShellTools.Classification
{
    /// <summary>
    /// The highlight matched braces Tag.
    /// </summary>
    internal sealed class HighlightMatchedBracesTag : TextMarkerTag
    {
	/// <summary>
	/// The constructor.
	/// </summary>
	public HighlightMatchedBracesTag()
	    : base(PowerShellConstants.HighlightMatchedBracesFormatDefinition)
	{

	}
    }

}
