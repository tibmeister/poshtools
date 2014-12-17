using Microsoft.VisualStudioTools.Project;

namespace PowerShellTools.Project
{
    internal class PowerShellFileNode : CommonFileNode
    {
		#region Constructors
		/// <summary>
        /// Initializes a new instance of the <see cref="PowerShellFileNode"/> class.
		/// </summary>
		/// <param name="root">The project node.</param>
		/// <param name="e">The project element node.</param>
        internal PowerShellFileNode(CommonProjectNode root, ProjectElement e)
			: base(root, e)
		    {
            
		    }
		#endregion



    }
}
