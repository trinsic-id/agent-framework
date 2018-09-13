using System;
using System.Collections.Generic;
using System.Text;

namespace Streetcred.Sdk.Model.Connections
{
    /// <summary>
    /// Connection alias object for tagging 
    /// a connection record with an alias to
    /// give more context 
    /// </summary>
    public class ConnectionAlias
    {
        /// <summary>
        /// Name of the alias
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Url to an image of the alias
        /// </summary>
        public string ImageUrl { get; set; }
    }
}
