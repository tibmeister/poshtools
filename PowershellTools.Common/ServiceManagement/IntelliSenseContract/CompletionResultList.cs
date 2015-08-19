using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.Management.Automation;

namespace PowerShellTools.Common.ServiceManagement.IntelliSenseContract
{
    /// <summary>
    /// The statement completion lists needed for IntelliSense.
    /// </summary>
    [DataContract]
    public sealed class CompletionResultList
    {
        [DataMember]
        public CompletionItem[] CompletionMatches { get; set; }

        [DataMember]
        public int ReplacementIndex { get; set; }

        [DataMember]
        public int ReplacementLength { get; set; }

        public static CompletionResultList FromCommandCompletion(CommandCompletion commandCompletion)
        {
            if (commandCompletion == null || commandCompletion.CompletionMatches.Count == 0)
            {
                return new CompletionResultList()
                {
                    CompletionMatches = new CompletionItem[0]
                };
            }

            return new CompletionResultList()
            {
                CompletionMatches = (from match in commandCompletion.CompletionMatches
                                     select new CompletionItem(match.CompletionText, 
                                                               match.ListItemText,
                                                               (int)match.ResultType,
                                                               match.ToolTip)).ToArray(),
                ReplacementIndex = commandCompletion.ReplacementIndex,
                ReplacementLength = commandCompletion.ReplacementLength
            };
        }
    }
}
